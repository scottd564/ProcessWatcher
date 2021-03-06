﻿using ProcessWatcher.Process;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ProcessWatcher
{
    public partial class MainForm : MetroFramework.Forms.MetroForm
    {
        private System.Threading.Timer timer;
        private ProcessContainer editingContainer;
        private DateTime lastInteraction = DateTime.Now;

        public MainForm()
        {
            InitializeComponent();

            style_manager.Owner = this;

            datagrid_view.AutoGenerateColumns = false;
            datagrid_view.BackgroundColor = style_manager.Theme == MetroFramework.MetroThemeStyle.Dark ? BackColor : Color.WhiteSmoke;
            datagrid_view.BorderStyle = System.Windows.Forms.BorderStyle.None;
            datagrid_view.RowHeadersVisible = false;
            datagrid_view.RowHeadersWidth = 4;

            datagrid_view.ColumnHeadersDefaultCellStyle.BackColor = style_manager.Theme == MetroFramework.MetroThemeStyle.Dark ? Color.Black : Color.White;
            datagrid_view.ColumnHeadersDefaultCellStyle.ForeColor = style_manager.Theme == MetroFramework.MetroThemeStyle.Dark ? Color.WhiteSmoke : Color.Gray;
            datagrid_view.ColumnHeadersDefaultCellStyle.SelectionForeColor = style_manager.Theme == MetroFramework.MetroThemeStyle.Dark ? Color.White : Color.Black;
            datagrid_view.ColumnHeadersDefaultCellStyle.SelectionBackColor = style_manager.Theme == MetroFramework.MetroThemeStyle.Dark ? Color.Black : Color.White;

            datagrid_view.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.EnableResizing;
            datagrid_view.CellContentClick += Datagrid_view_CellContentClick;
            datagrid_view.CellPainting += (object _, DataGridViewCellPaintingEventArgs e) =>
            {
                e.CellStyle.BackColor = datagrid_view.BackgroundColor;
            };

            dropdown_priority.BackColor = style_manager.Theme == MetroFramework.MetroThemeStyle.Dark ? Color.Black : Color.White;
            FormClosed += (object sender, FormClosedEventArgs e) =>
            {
                App.OnProcessExit();
            };
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            timer = new System.Threading.Timer(new TimerCallback(CheckServerStatus), null, 1000, Timeout.Infinite);

            datagrid_view.DataSource = ServerFactory.servers;
            datagrid_view.PerformLayout();
        }

        private void Datagrid_view_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (lastInteraction.CompareTo(DateTime.Now) > 0)
                return;

            lastInteraction = DateTime.Now.AddSeconds(5);

            DataGridViewRow gridRow = datagrid_view.Rows[e.RowIndex];
            ProcessContainer procContainer = gridRow.DataBoundItem as ProcessContainer;
            if (e.ColumnIndex == 4)
            {
                if (procContainer.IsRunning)
                {
                    procContainer.CloseProcess();
                    gridRow.Cells["StartStatus"].Value = "Start";
                }
                else
                {
                    procContainer.CreateProcess();
                    gridRow.Cells["StartStatus"].Value = "Stop";
                }

                datagrid_view.RefreshEdit();
            }
            else if (e.ColumnIndex == 5)
            {
                EditContainer(procContainer);
            }
        }

        private void CheckServerStatus(object state)
        {
            foreach (ProcessContainer pc in ServerFactory.servers)
                pc.PollStatus(true);

            datagrid_view.Invoke(new Action(() =>
            {
                datagrid_view.Refresh();
            }));

            timer.Change(1000, Timeout.Infinite);
        }

        private void input_process_TextChanged(object sender, EventArgs e)
        {
            input_startfrom.Text = Path.GetDirectoryName(input_process.Text ?? "");

            if (string.IsNullOrWhiteSpace(input_name.Text))
                input_name.Text = Path.GetFileNameWithoutExtension(input_process.Text);
        }

        public bool IsValidFilePath(string path)
        {
            if (Path.GetExtension(path) != ".exe")
                return false;

            return File.Exists(path);
        }

        private void button_update_Click(object sender, EventArgs e)
        {
            string name = input_name.Text;
            string process = input_process.Text;
            string startIn = input_startfrom.Text;
            string startParams = input_target.Text;

            if (!IsValidFilePath(input_process.Text))
                return;

            if (editingContainer != null)
            {
                editingContainer.ProcName = name;
                editingContainer.Path = process;
                editingContainer.PathParams = startParams;
                editingContainer = null;
            }
            else
            {
                ServerFactory.Create(name, process, startParams, new Dictionary<string, dynamic>() {
                    {"UseWeb", toggle_web.Checked},
                    {"StartIn", startIn},
                    {"AutoStart", toggle_autostart.Checked}
                });
            }
        }

        private void button_delete_Click(object sender, EventArgs e)
        {
            ClearCreation(true);
        }

        private void btn_filebrowser_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog diag = new OpenFileDialog())
            {
                diag.InitialDirectory = "c:\\";
                diag.Filter = "Executable (*.exe)|*.exe";

                if (diag.ShowDialog() == DialogResult.OK)
                {
                    input_process.Text = diag.FileName;
                }
            }
        }

        private void folder_browser_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog diag = new FolderBrowserDialog())
            {
                if (diag.ShowDialog() == DialogResult.OK)
                {
                    input_startfrom.Text = diag.SelectedPath;
                }
            }
        }

        private void ClearCreation(bool removeProcess = false)
        {
            string name = input_name.Text;

            input_name.Text = "";
            input_process.Text = "";
            input_startfrom.Text = "";
            input_target.Text = "";

            toggle_web.Checked = false;
            toggle_autostart.Checked = false;
            
            editingContainer = null;

            if(removeProcess)
             ServerFactory.RemoveByName(name);
        }

        private void EditContainer(ProcessContainer container)
        {
            ClearCreation();

            editingContainer = container;
            input_name.Text = container.ProcName;
            input_process.Text = container.Path;
            input_target.Text = container.PathParams;
        }
    }
}