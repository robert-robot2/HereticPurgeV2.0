using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Button = System.Windows.Forms.Button;
using Label = System.Windows.Forms.Label;

namespace HereticPurge
{
    public class PurgeProgressForm : Form
    {
        private List<string> projectPaths;
        private bool purgeBin;
        private bool purgeObj;
        private ProgressBar progressBar;
        private RichTextBox logTextBox;
        private Label lblStatus;
        private Button btnClose;

        private int totalFoldersDeleted = 0;
        private int totalErrors = 0;
        private long totalBytesFreed = 0;
        private bool isComplete = false;

        public PurgeProgressForm(List<string> paths, bool includeBin = true, bool includeObj = true)
        {
            projectPaths = new List<string>(paths);
            purgeBin = includeBin;
            purgeObj = includeObj;
            InitializeComponent();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            StartPurge();
        }

        private void InitializeComponent()
        {
            this.Text = "Purging Projects...";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Status label
            lblStatus = new Label
            {
                Text = "Initializing purge...",
                Location = new Point(20, 20),
                Size = new Size(640, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            // Progress bar
            progressBar = new ProgressBar
            {
                Location = new Point(20, 50),
                Size = new Size(640, 25),
                Style = ProgressBarStyle.Continuous
            };

            // Log text box
            logTextBox = new RichTextBox
            {
                Location = new Point(20, 85),
                Size = new Size(640, 320),
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Close button
            btnClose = new Button
            {
                Text = "Processing...",
                Location = new Point(280, 420),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            this.Controls.Add(lblStatus);
            this.Controls.Add(progressBar);
            this.Controls.Add(logTextBox);
            this.Controls.Add(btnClose);
        }

        private async void StartPurge()
        {
            await Task.Run(() => PerformPurge());
        }

        private void PerformPurge()
        {
            int totalProjects = projectPaths.Count;
            int currentProject = 0;

            foreach (var projectPath in projectPaths)
            {
                currentProject++;

                this.Invoke((MethodInvoker)delegate
                {
                    lblStatus.Text = $"Purging project {currentProject} of {totalProjects}: {Path.GetFileName(projectPath)}";
                    progressBar.Value = (int)((currentProject - 1) / (double)totalProjects * 100);
                });

                LogMessage($"\n═══ Project {currentProject}/{totalProjects}: {projectPath} ═══", Color.Cyan);

                // Purge based on selected options
                if (purgeBin)
                {
                    PurgeFolder(projectPath, "bin");
                }

                if (purgeObj)
                {
                    PurgeFolder(projectPath, "obj");
                }
            }

            // Complete
            this.Invoke((MethodInvoker)delegate
            {
                progressBar.Value = 100;
                lblStatus.Text = "Purge Complete!";
                lblStatus.ForeColor = Color.FromArgb(39, 174, 96);

                LogMessage("\n════════════════════════════════════════════", Color.Green);
                LogMessage("✓ PURGE COMPLETE!", Color.Green);
                LogMessage($"  • Folders deleted: {totalFoldersDeleted}", Color.White);
                LogMessage($"  • Errors encountered: {totalErrors}", totalErrors > 0 ? Color.Orange : Color.White);

                if (totalBytesFreed > 0)
                {
                    string sizeFreed = FormatBytes(totalBytesFreed);
                    LogMessage($"  • Approximate space freed: {sizeFreed}", Color.LightGreen);
                }

                LogMessage("════════════════════════════════════════════", Color.Green);

                btnClose.Text = "Close";
                btnClose.Enabled = true;
                btnClose.BackColor = Color.FromArgb(39, 174, 96);
                isComplete = true;
            });
        }

        private void PurgeFolder(string rootPath, string folderName)
        {
            try
            {
                var folders = Directory.GetDirectories(rootPath, folderName, SearchOption.AllDirectories);

                if (folders.Length == 0)
                {
                    LogMessage($"  No '{folderName}' folders found", Color.Gray);
                    return;
                }

                LogMessage($"  Found {folders.Length} '{folderName}' folder(s)", Color.Yellow);

                foreach (var folder in folders)
                {
                    try
                    {
                        // Calculate size before deletion
                        long folderSize = GetDirectorySize(folder);

                        Directory.Delete(folder, true);

                        totalFoldersDeleted++;
                        totalBytesFreed += folderSize;

                        string relativePath = folder.Replace(rootPath, "").TrimStart(Path.DirectorySeparatorChar);
                        LogMessage($"    ✓ Deleted: {relativePath} ({FormatBytes(folderSize)})", Color.LightGreen);
                    }
                    catch (Exception ex)
                    {
                        totalErrors++;
                        string relativePath = folder.Replace(rootPath, "").TrimStart(Path.DirectorySeparatorChar);
                        LogMessage($"    ✗ Failed: {relativePath} - {ex.Message}", Color.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                totalErrors++;
                LogMessage($"  ✗ Error scanning '{folderName}': {ex.Message}", Color.Red);
            }
        }

        private long GetDirectorySize(string path)
        {
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(path);
                long size = 0;

                // Add file sizes
                FileInfo[] files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
                foreach (FileInfo file in files)
                {
                    size += file.Length;
                }

                return size;
            }
            catch
            {
                return 0;
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private void LogMessage(string message, Color color)
        {
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke((MethodInvoker)delegate
                {
                    LogMessage(message, color);
                });
            }
            else
            {
                logTextBox.SelectionStart = logTextBox.TextLength;
                logTextBox.SelectionLength = 0;
                logTextBox.SelectionColor = color;
                logTextBox.AppendText(message + "\n");
                logTextBox.SelectionColor = logTextBox.ForeColor;
                logTextBox.ScrollToCaret();
            }
        }

        public string GetCompletionMessage()
        {
            if (!isComplete)
                return "Purge cancelled or incomplete";

            return $"Purge complete! Deleted {totalFoldersDeleted} folders ({FormatBytes(totalBytesFreed)} freed), {totalErrors} errors";
        }
    }
}