using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Label = System.Windows.Forms.Label;
using Button = System.Windows.Forms.Button;

namespace HereticPurge
{
    public partial class MainForm : Form
    {
        private List<string> projectPaths = new List<string>();

        // UI Controls
        private ListView listViewPaths;
        private Button btnBrowse;
        private Button btnAdd;
        private Button btnRemove;
        private Button btnClear;
        private Button btnPurge;
        private Button btnRefresh;
        private Button btnRemoveInvalid;
        private CheckBox chkPurgeBin;
        private CheckBox chkPurgeObj;
        private TextBox txtPath;
        private Label lblPaths;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private MenuStrip menuStrip;
        private ToolTip toolTip;
        private ContextMenuStrip contextMenu;

        public MainForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
            LoadSavedPaths();
        }

        private void LoadSavedPaths()
        {
            var savedPaths = ConfigManager.LoadPaths();

            if (savedPaths.Count > 0)
            {
                foreach (var path in savedPaths)
                {
                    projectPaths.Add(path);
                    AddPathToListView(path);
                }

                statusLabel.Text = $"Loaded {savedPaths.Count} saved path(s)";
                UpdatePathCount();
            }
        }

        private void TxtPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                BtnAdd_Click(this, EventArgs.Empty);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void InitializeComponent()
        {
            // Form settings
            this.Text = "HereticPurge v2.0";
            this.Size = new Size(800, 600);
            this.MinimumSize = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+S to save
            if (e.Control && e.KeyCode == Keys.S)
            {
                ConfigManager.SavePaths(projectPaths);
                statusLabel.Text = "Paths saved manually";
                e.Handled = true;
            }
            // Delete key to remove selected
            else if (e.KeyCode == Keys.Delete && listViewPaths.SelectedItems.Count > 0)
            {
                BtnRemove_Click(this, EventArgs.Empty);
                e.Handled = true;
            }
            // F5 to refresh
            else if (e.KeyCode == Keys.F5)
            {
                if (projectPaths.Count > 0)
                {
                    RefreshPathValidation();
                }
                e.Handled = true;
            }
        }

        private void InitializeCustomComponents()
        {
            // Initialize tooltips
            toolTip = new ToolTip();
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 500;
            toolTip.ReshowDelay = 100;

            // Initialize context menu
            contextMenu = new ContextMenuStrip();

            ToolStripMenuItem openFolderItem = new ToolStripMenuItem("Open in Explorer");
            openFolderItem.Click += (s, e) => OpenSelectedInExplorer();

            ToolStripMenuItem copyPathItem = new ToolStripMenuItem("Copy Path");
            copyPathItem.Click += (s, e) => CopySelectedPath();

            ToolStripMenuItem removeItem = new ToolStripMenuItem("Remove");
            removeItem.Click += (s, e) => BtnRemove_Click(this, EventArgs.Empty);

            contextMenu.Items.Add(openFolderItem);
            contextMenu.Items.Add(copyPathItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(removeItem);

            // Menu bar
            menuStrip = new MenuStrip();

            // File menu
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");

            ToolStripMenuItem searchProjectsItem = new ToolStripMenuItem("Search for Projects...");
            searchProjectsItem.Click += SearchForProjects_Click;

            ToolStripMenuItem saveNowItem = new ToolStripMenuItem("Save Paths Now");
            saveNowItem.Click += (s, e) =>
            {
                ConfigManager.SavePaths(projectPaths);
                MessageBox.Show("Paths saved successfully!", "Saved",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            ToolStripMenuItem openConfigItem = new ToolStripMenuItem("Open Config Folder");
            openConfigItem.Click += (s, e) =>
            {
                string configDir = Path.GetDirectoryName(ConfigManager.GetConfigLocation());
                if (Directory.Exists(configDir))
                {
                    System.Diagnostics.Process.Start("explorer.exe", configDir);
                }
            };

            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => this.Close();

            fileMenu.DropDownItems.Add(searchProjectsItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(saveNowItem);
            fileMenu.DropDownItems.Add(openConfigItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitItem);

            // Tools menu
            ToolStripMenuItem toolsMenu = new ToolStripMenuItem("Tools");

            ToolStripMenuItem previewItem = new ToolStripMenuItem("Preview Purge Statistics");
            previewItem.Click += PreviewPurgeStats_Click;

            toolsMenu.DropDownItems.Add(previewItem);

            // Help menu
            ToolStripMenuItem helpMenu = new ToolStripMenuItem("Help");

            ToolStripMenuItem shortcutsItem = new ToolStripMenuItem("Keyboard Shortcuts");
            shortcutsItem.Click += (s, e) =>
            {
                MessageBox.Show(
                    "Keyboard Shortcuts:\n\n" +
                    "Enter - Add path from text box\n" +
                    "Delete - Remove selected path(s)\n" +
                    "F5 - Refresh path validation\n" +
                    "Ctrl+S - Save paths manually\n",
                    "Keyboard Shortcuts",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };

            ToolStripMenuItem aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += (s, e) =>
            {
                MessageBox.Show(
                    "HereticPurge v2.0\n\n" +
                    "A utility to clean bin and obj folders from .NET projects.\n\n" +
                    $"Config location:\n{ConfigManager.GetConfigLocation()}\n\n" +
                    "© 2026",
                    "About HereticPurge",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };

            helpMenu.DropDownItems.Add(shortcutsItem);
            helpMenu.DropDownItems.Add(new ToolStripSeparator());
            helpMenu.DropDownItems.Add(aboutItem);

            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(toolsMenu);
            menuStrip.Items.Add(helpMenu);

            // Main container panel
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };

            // Title Label
            Label titleLabel = new Label
            {
                Text = "HereticPurge - Clean bin & obj Folders",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                AutoSize = true,
                Location = new Point(15, 35)
            };

            // Path input section
            Label lblInput = new Label
            {
                Text = "Add Project Path:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(15, 80)
            };

            txtPath = new TextBox
            {
                Location = new Point(15, 105),
                Size = new Size(500, 25),
                Font = new Font("Segoe UI", 10)
            };
            txtPath.KeyDown += TxtPath_KeyDown;
            toolTip.SetToolTip(txtPath, "Enter or paste a project folder path (press Enter to add)");

            btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(525, 103),
                Size = new Size(100, 27),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.Click += BtnBrowse_Click;
            toolTip.SetToolTip(btnBrowse, "Browse for a project folder");

            btnAdd = new Button
            {
                Text = "Add Path",
                Location = new Point(635, 103),
                Size = new Size(100, 27),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += BtnAdd_Click;
            toolTip.SetToolTip(btnAdd, "Add this path to the list");

            // Project paths list section
            lblPaths = new Label
            {
                Text = "Project Paths (0):",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(15, 145)
            };

            listViewPaths = new ListView
            {
                Location = new Point(15, 170),
                Size = new Size(720, 230),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 9),
                MultiSelect = true,
                AllowDrop = true,
                ContextMenuStrip = contextMenu
            };
            listViewPaths.Columns.Add("Path", 650);
            listViewPaths.Columns.Add("Status", 65);
            listViewPaths.DragEnter += ListView_DragEnter;
            listViewPaths.DragDrop += ListView_DragDrop;
            listViewPaths.MouseDoubleClick += ListView_MouseDoubleClick;
            toolTip.SetToolTip(listViewPaths, "Drag and drop folders here to add them | Double-click to open in Explorer");

            // Button panel for list controls
            Panel buttonPanel = new Panel
            {
                Location = new Point(15, 410),
                Size = new Size(720, 40)
            };

            // Filter checkboxes for purge options
            Label lblPurgeOptions = new Label
            {
                Text = "Purge:",
                Location = new Point(490, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            chkPurgeBin = new CheckBox
            {
                Text = "bin",
                Location = new Point(540, 7),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            toolTip.SetToolTip(chkPurgeBin, "Include 'bin' folders in purge");

            chkPurgeObj = new CheckBox
            {
                Text = "obj",
                Location = new Point(590, 7),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            toolTip.SetToolTip(chkPurgeObj, "Include 'obj' folders in purge");

            btnRemove = new Button
            {
                Text = "Remove Selected",
                Location = new Point(0, 0),
                Size = new Size(130, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRemove.FlatAppearance.BorderSize = 0;
            btnRemove.Click += BtnRemove_Click;
            toolTip.SetToolTip(btnRemove, "Remove selected path(s) from the list");

            btnClear = new Button
            {
                Text = "Clear All",
                Location = new Point(140, 0),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += BtnClear_Click;
            toolTip.SetToolTip(btnClear, "Remove all paths from the list");

            btnRefresh = new Button
            {
                Text = "🔄 Refresh",
                Location = new Point(250, 0),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += BtnRefresh_Click;
            toolTip.SetToolTip(btnRefresh, "Re-validate all paths to check if they still exist");

            btnRemoveInvalid = new Button
            {
                Text = "Remove Invalid",
                Location = new Point(360, 0),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(230, 126, 34),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRemoveInvalid.FlatAppearance.BorderSize = 0;
            btnRemoveInvalid.Click += BtnRemoveInvalid_Click;
            toolTip.SetToolTip(btnRemoveInvalid, "Remove all paths that no longer exist");

            btnPurge = new Button
            {
                Text = "🗑 PURGE",
                Location = new Point(640, 0),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(192, 57, 43),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPurge.FlatAppearance.BorderSize = 0;
            btnPurge.Click += BtnPurge_Click;
            toolTip.SetToolTip(btnPurge, "Delete selected folder types in the listed projects");

            // Status strip
            statusStrip = new StatusStrip
            {
                BackColor = Color.FromArgb(230, 230, 230)
            };
            statusLabel = new ToolStripStatusLabel
            {
                Text = "Ready",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            statusStrip.Items.Add(statusLabel);

            // Add all controls to button panel
            buttonPanel.Controls.Add(btnRemove);
            buttonPanel.Controls.Add(btnClear);
            buttonPanel.Controls.Add(btnRefresh);
            buttonPanel.Controls.Add(btnRemoveInvalid);
            buttonPanel.Controls.Add(lblPurgeOptions);
            buttonPanel.Controls.Add(chkPurgeBin);
            buttonPanel.Controls.Add(chkPurgeObj);
            buttonPanel.Controls.Add(btnPurge);

            // Add all controls to main panel
            mainPanel.Controls.Add(titleLabel);
            mainPanel.Controls.Add(lblInput);
            mainPanel.Controls.Add(txtPath);
            mainPanel.Controls.Add(btnBrowse);
            mainPanel.Controls.Add(btnAdd);
            mainPanel.Controls.Add(lblPaths);
            mainPanel.Controls.Add(listViewPaths);
            mainPanel.Controls.Add(buttonPanel);

            // Add to form
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
            this.Controls.Add(mainPanel);
            this.Controls.Add(statusStrip);

            UpdatePathCount();
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select a project folder to add";
                folderDialog.ShowNewFolderButton = false;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = folderDialog.SelectedPath;
                }
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            string path = txtPath.Text.Trim();

            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Please enter or browse for a folder path.", "No Path Entered",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(path))
            {
                MessageBox.Show("The specified path does not exist.", "Invalid Path",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (projectPaths.Contains(path))
            {
                MessageBox.Show("This path is already in the list.", "Duplicate Path",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            projectPaths.Add(path);
            AddPathToListView(path);
            txtPath.Clear();
            statusLabel.Text = $"Added: {path}";
            UpdatePathCount();

            // Auto-save after adding
            ConfigManager.SavePaths(projectPaths);
        }

        private void AddPathToListView(string path)
        {
            ListViewItem item = new ListViewItem(path);

            // Check if path exists and set status
            if (Directory.Exists(path))
            {
                item.SubItems.Add("✓ Valid");
                item.ForeColor = Color.FromArgb(39, 174, 96);
            }
            else
            {
                item.SubItems.Add("✗ Invalid");
                item.ForeColor = Color.FromArgb(231, 76, 60);
            }

            listViewPaths.Items.Add(item);
        }

        private void RefreshPathValidation()
        {
            foreach (ListViewItem item in listViewPaths.Items)
            {
                string path = item.Text;

                if (Directory.Exists(path))
                {
                    item.SubItems[1].Text = "✓ Valid";
                    item.ForeColor = Color.FromArgb(39, 174, 96);
                }
                else
                {
                    item.SubItems[1].Text = "✗ Invalid";
                    item.ForeColor = Color.FromArgb(231, 76, 60);
                }
            }

            statusLabel.Text = "Path validation refreshed";
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (listViewPaths.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select one or more paths to remove.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int removedCount = 0;
            for (int i = listViewPaths.SelectedItems.Count - 1; i >= 0; i--)
            {
                ListViewItem item = listViewPaths.SelectedItems[i];
                string path = item.Text;
                projectPaths.Remove(path);
                listViewPaths.Items.Remove(item);
                removedCount++;
            }

            statusLabel.Text = $"Removed {removedCount} path(s)";
            UpdatePathCount();

            // Auto-save after removing
            ConfigManager.SavePaths(projectPaths);
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            if (projectPaths.Count == 0)
            {
                MessageBox.Show("No paths to clear.", "Empty List",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Are you sure you want to remove all {projectPaths.Count} path(s)?",
                "Clear All Paths",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                int count = projectPaths.Count;
                projectPaths.Clear();
                listViewPaths.Items.Clear();
                statusLabel.Text = $"Cleared {count} path(s)";
                UpdatePathCount();

                // Auto-save after clearing
                ConfigManager.SavePaths(projectPaths);
            }
        }

        private void BtnPurge_Click(object sender, EventArgs e)
        {
            if (projectPaths.Count == 0)
            {
                MessageBox.Show("No project paths to purge. Please add at least one path first.",
                    "No Paths", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!chkPurgeBin.Checked && !chkPurgeObj.Checked)
            {
                MessageBox.Show("Please select at least one folder type to purge (bin or obj).",
                    "No Options Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string folderTypes = "";
            if (chkPurgeBin.Checked && chkPurgeObj.Checked)
                folderTypes = "'bin' and 'obj'";
            else if (chkPurgeBin.Checked)
                folderTypes = "'bin'";
            else
                folderTypes = "'obj'";

            DialogResult result = MessageBox.Show(
                $"This will DELETE all {folderTypes} folders in {projectPaths.Count} project path(s).\n\n" +
                "This action cannot be undone!\n\n" +
                "Are you sure you want to continue?",
                "Confirm Purge",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                // Show progress form
                PurgeProgressForm progressForm = new PurgeProgressForm(
                    projectPaths,
                    chkPurgeBin.Checked,
                    chkPurgeObj.Checked);
                progressForm.ShowDialog();

                statusLabel.Text = progressForm.GetCompletionMessage();
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            if (projectPaths.Count == 0)
            {
                MessageBox.Show("No paths to refresh.", "Empty List",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RefreshPathValidation();
        }

        private void BtnRemoveInvalid_Click(object sender, EventArgs e)
        {
            if (projectPaths.Count == 0)
            {
                MessageBox.Show("No paths in the list.", "Empty List",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Find invalid paths
            List<string> invalidPaths = new List<string>();
            foreach (string path in projectPaths)
            {
                if (!Directory.Exists(path))
                {
                    invalidPaths.Add(path);
                }
            }

            if (invalidPaths.Count == 0)
            {
                MessageBox.Show("All paths are valid!", "No Invalid Paths",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Remove {invalidPaths.Count} invalid path(s) from the list?",
                "Remove Invalid Paths",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Remove from list
                foreach (string path in invalidPaths)
                {
                    projectPaths.Remove(path);
                }

                // Remove from ListView
                for (int i = listViewPaths.Items.Count - 1; i >= 0; i--)
                {
                    if (!Directory.Exists(listViewPaths.Items[i].Text))
                    {
                        listViewPaths.Items.RemoveAt(i);
                    }
                }

                statusLabel.Text = $"Removed {invalidPaths.Count} invalid path(s)";
                UpdatePathCount();
                ConfigManager.SavePaths(projectPaths);
            }
        }

        private void UpdatePathCount()
        {
            lblPaths.Text = $"Project Paths ({projectPaths.Count}):";
        }

        private void ListView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void ListView_DragDrop(object sender, DragEventArgs e)
        {
            string[] droppedPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            int addedCount = 0;
            int skippedCount = 0;

            foreach (string path in droppedPaths)
            {
                // Only accept directories
                if (Directory.Exists(path))
                {
                    if (!projectPaths.Contains(path))
                    {
                        projectPaths.Add(path);
                        AddPathToListView(path);
                        addedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
            }

            if (addedCount > 0)
            {
                UpdatePathCount();
                ConfigManager.SavePaths(projectPaths);
                statusLabel.Text = $"Added {addedCount} path(s)" +
                    (skippedCount > 0 ? $", skipped {skippedCount} duplicate(s)" : "");
            }
            else if (skippedCount > 0)
            {
                statusLabel.Text = $"All {skippedCount} path(s) already in list";
            }
        }

        private void ListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listViewPaths.SelectedItems.Count > 0)
            {
                OpenSelectedInExplorer();
            }
        }

        private void OpenSelectedInExplorer()
        {
            if (listViewPaths.SelectedItems.Count == 0)
                return;

            foreach (ListViewItem item in listViewPaths.SelectedItems)
            {
                string path = item.Text;
                if (Directory.Exists(path))
                {
                    System.Diagnostics.Process.Start("explorer.exe", path);
                }
                else
                {
                    MessageBox.Show($"Path no longer exists:\n{path}", "Path Not Found",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void CopySelectedPath()
        {
            if (listViewPaths.SelectedItems.Count == 0)
                return;

            if (listViewPaths.SelectedItems.Count == 1)
            {
                Clipboard.SetText(listViewPaths.SelectedItems[0].Text);
                statusLabel.Text = "Path copied to clipboard";
            }
            else
            {
                var paths = new System.Text.StringBuilder();
                foreach (ListViewItem item in listViewPaths.SelectedItems)
                {
                    paths.AppendLine(item.Text);
                }
                Clipboard.SetText(paths.ToString());
                statusLabel.Text = $"Copied {listViewPaths.SelectedItems.Count} paths to clipboard";
            }
        }

        private void SearchForProjects_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select a root folder to search for .NET projects";
                folderDialog.ShowNewFolderButton = false;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string searchPath = folderDialog.SelectedPath;
                    statusLabel.Text = "Searching for projects...";
                    Application.DoEvents();

                    try
                    {
                        var projectFiles = Directory.GetFiles(searchPath, "*.csproj", SearchOption.AllDirectories);

                        if (projectFiles.Length == 0)
                        {
                            MessageBox.Show("No .csproj files found in the selected directory.",
                                "No Projects Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            statusLabel.Text = "No projects found";
                            return;
                        }

                        int addedCount = 0;
                        int skippedCount = 0;

                        foreach (var projectFile in projectFiles)
                        {
                            string projectDir = Path.GetDirectoryName(projectFile);

                            if (!projectPaths.Contains(projectDir))
                            {
                                projectPaths.Add(projectDir);
                                AddPathToListView(projectDir);
                                addedCount++;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }

                        UpdatePathCount();
                        ConfigManager.SavePaths(projectPaths);

                        MessageBox.Show(
                            $"Found {projectFiles.Length} project(s)\n" +
                            $"Added: {addedCount}\n" +
                            $"Already in list: {skippedCount}",
                            "Search Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        statusLabel.Text = $"Added {addedCount} project(s) from search";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error searching for projects:\n{ex.Message}",
                            "Search Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        statusLabel.Text = "Search failed";
                    }
                }
            }
        }

        private void PreviewPurgeStats_Click(object sender, EventArgs e)
        {
            if (projectPaths.Count == 0)
            {
                MessageBox.Show("No project paths to analyze. Please add at least one path first.",
                    "No Paths", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            statusLabel.Text = "Analyzing folders...";
            Application.DoEvents();

            int totalBinFolders = 0;
            int totalObjFolders = 0;
            long totalSize = 0;

            try
            {
                foreach (var projectPath in projectPaths)
                {
                    if (chkPurgeBin.Checked)
                    {
                        var binFolders = Directory.GetDirectories(projectPath, "bin", SearchOption.AllDirectories);
                        totalBinFolders += binFolders.Length;

                        foreach (var folder in binFolders)
                        {
                            totalSize += GetDirectorySize(folder);
                        }
                    }

                    if (chkPurgeObj.Checked)
                    {
                        var objFolders = Directory.GetDirectories(projectPath, "obj", SearchOption.AllDirectories);
                        totalObjFolders += objFolders.Length;

                        foreach (var folder in objFolders)
                        {
                            totalSize += GetDirectorySize(folder);
                        }
                    }
                }

                string message = "Purge Preview:\n\n";
                if (chkPurgeBin.Checked)
                    message += $"'bin' folders found: {totalBinFolders}\n";
                if (chkPurgeObj.Checked)
                    message += $"'obj' folders found: {totalObjFolders}\n";
                message += $"\nTotal folders: {totalBinFolders + totalObjFolders}\n";
                message += $"Approximate size: {FormatBytes(totalSize)}";

                MessageBox.Show(message, "Purge Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
                statusLabel.Text = $"Preview: {totalBinFolders + totalObjFolders} folders, {FormatBytes(totalSize)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error analyzing folders:\n{ex.Message}",
                    "Analysis Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Analysis failed";
            }
        }

        private long GetDirectorySize(string path)
        {
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(path);
                long size = 0;

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
    }
}