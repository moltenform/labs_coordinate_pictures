using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public partial class FormSortFiles : Form
    {
        PersistMostRecentlyUsedList _mruHistoryLeft;
        PersistMostRecentlyUsedList _mruHistoryRight;
        SortFilesAction _action;
        object _lock = new object();

        public FormSortFiles(SortFilesAction action)
        {
            InitializeComponent();

            _action = action;
            _mruHistoryLeft = new PersistMostRecentlyUsedList(InputBoxHistory.SyncDirectoryLeft);
            _mruHistoryRight = new PersistMostRecentlyUsedList(InputBoxHistory.SyncDirectoryRight);
            cmbLeftDir.DragDrop += new DragEventHandler(CmbOnDragDrop);
            cmbLeftDir.DragEnter += new DragEventHandler(CmbOnDragEnter);
            cmbRightDir.DragDrop += new DragEventHandler(CmbOnDragDrop);
            cmbRightDir.DragEnter += new DragEventHandler(CmbOnDragEnter);

            RefreshComboListItems();
            AcceptButton = btnStart;
            cmbLeftDir.Text = cmbLeftDir.Items.Count > 0 ? cmbLeftDir.Items[0].ToString() : "";
            cmbRightDir.Text = cmbRightDir.Items.Count > 0 ? cmbRightDir.Items[0].ToString() : "";

            if (action != SortFilesAction.SyncFiles)
            {
                btnStart.Text = "Start...";
                btnShowRobo.Visible = false;
                txtShowRobo.Visible = false;
                checkMirror.Visible = false;
                checkPreview.Visible = false;
                lblSkipDirs.Visible = false;
                lblSkipFiles.Visible = false;
                txtSkipDirs.Visible = false;
                txtSkipFiles.Visible = false;
                lblLeftDirDesc.Text = "Directory #1:";
                lblRightDirDesc.Text = "Directory #2:";
                checkAllowDifferSeconds.Text = checkAllowDifferSeconds.Text.Replace("$",
                    SortFilesSearchDifferences.AllowDifferSeconds.ToString());
            }
            else
            {
                checkMirror.Checked = true;
                checkAllowDifferSeconds.Text = checkAllowDifferSeconds.Text.Replace("$",
                    SyncFilesWithRobocopy.AllowDifferSeconds.ToString());
            }

            if (action == SortFilesAction.SearchDifferences)
            {
                lblAction.Text = @"Search for differences in two similar folders.";
            }
            else if (action == SortFilesAction.SearchDuplicates)
            {
                lblAction.Text = "Search for duplicate files.";
                checkAllowDifferDST.Visible = false;
                checkAllowDifferSeconds.Visible = false;
            }
            else if (action == SortFilesAction.SearchDuplicatesInOneDir)
            {
                lblAction.Text = "Search for duplicate files in a folder.";
                lblLeftDirDesc.Text = "Directory:";
                lblRightDirDesc.Text = "";
                btnSetRightDir.Visible = false;
                cmbRightDir.Visible = false;
                btnSwap.Visible = false;
                checkAllowDifferDST.Visible = false;
                checkAllowDifferSeconds.Visible = false;
            }
            else if (action == SortFilesAction.SyncFiles)
            {
                lblAction.Text = "Sync files. Copy changes from the source directory to the destination directory.";
            }
        }

        void RefreshComboListItems()
        {
            cmbLeftDir.Items.Clear();
            cmbRightDir.Items.Clear();

            foreach (var s in _mruHistoryLeft.Get())
            {
                cmbLeftDir.Items.Add(s);
            }

            foreach (var s in _mruHistoryRight.Get())
            {
                cmbRightDir.Items.Add(s);
            }
        }

        private void btnSwap_Click(object sender, EventArgs e)
        {
            var tmp = cmbLeftDir.Text;
            cmbLeftDir.Text = cmbRightDir.Text;
            cmbRightDir.Text = tmp;
        }

        public static string[] TextLineByLineToList(string s)
        {
            // splits by paragraph, and ignores whitespace. supports both \n and \r\n
            var lines = s.Replace("\r\n", "\n").Split(new char[] { '\n' });
            return (from line in lines where line.Trim().Length > 0 select line.Trim()).ToArray();
        }

        public SortFilesSettings FillFromUI()
        {
            return FillFromUI(_action, txtSkipDirs.Text, txtSkipFiles.Text,
                cmbLeftDir.Text, cmbRightDir.Text,
                checkAllowDifferSeconds.Checked, checkAllowDifferDST.Checked,
                checkMirror.Checked, checkPreview.Checked);
        }

        public static SortFilesSettings FillFromUI(SortFilesAction action,
            string skipDirs, string skipFiles,
            string dirLeft, string dirRight,
            bool allowTimesDiffer, bool allowTimesDifferDst,
            bool mirror, bool previewOnly)
        {
            var settings = new SortFilesSettings();
            settings.SkipDirectories.AddRange(TextLineByLineToList(skipDirs));
            settings.SkipFiles.AddRange(TextLineByLineToList(skipFiles));
            settings.LeftDirectory = dirLeft;
            settings.RightDirectory = dirRight;
            settings.AllowFiletimesDifferForFAT = allowTimesDiffer;
            settings.AllowFiletimesDifferForDST = allowTimesDifferDst;
            settings.Mirror = mirror;
            settings.PreviewOnly = previewOnly;
            settings.LogFile = Path.Combine(TestUtil.GetTestWriteDirectory(),
                "log" + Utils.GetRandomDigits() + ".txt");

            // the right directory field isn't shown in UI, we need to set it manually
            if (action == SortFilesAction.SearchDuplicatesInOneDir)
            {
                settings.RightDirectory = settings.LeftDirectory;
            }

            if (!Directory.Exists(settings.LeftDirectory))
            {
                Utils.MessageErr("Directory does not exist " + settings.LeftDirectory, true);
                return null;
            }
            else if (!Directory.Exists(settings.RightDirectory) && action != SortFilesAction.SearchDuplicatesInOneDir)
            {
                Utils.MessageErr("Directory does not exist " + settings.RightDirectory, true);
                return null;
            }

            if (settings.LeftDirectory.EndsWith(Utils.PathSep, StringComparison.Ordinal) ||
                settings.RightDirectory.EndsWith(Utils.PathSep, StringComparison.Ordinal))
            {
                Utils.MessageErr("directory should not end with slash.", true);
                return null;
            }

            // https://msdn.microsoft.com/en-us/library/dd465121.aspx recommends comparing filepaths with OrdinalIgnoreCase
            if (action != SortFilesAction.SearchDuplicatesInOneDir &&
                (settings.LeftDirectory.StartsWith(settings.RightDirectory, StringComparison.OrdinalIgnoreCase) ||
                settings.RightDirectory.StartsWith(settings.LeftDirectory, StringComparison.OrdinalIgnoreCase)))
            {
                Utils.MessageErr("directories must be distinct.", true);
                return null;
            }

            return settings;
        }

        void Start(bool showCommandLineOnly)
        {
            if (_action == SortFilesAction.SyncFiles && !checkPreview.Checked && !showCommandLineOnly &&
                !Utils.AskToConfirm("Are you sure you want to synchronize these files?"))
            {
                return;
            }

            _mruHistoryLeft.AddToHistory(cmbLeftDir.Text);
            _mruHistoryRight.AddToHistory(cmbRightDir.Text);
            RefreshComboListItems();

            txtShowRobo.Text = "";
            var settings = FillFromUI();
            if (settings == null)
            {
                return;
            }

            if (_action == SortFilesAction.SyncFiles)
            {
                if (showCommandLineOnly)
                {
                    var args = SyncFilesWithRobocopy.GetFullArgs(settings);
                    txtShowRobo.Text = args;
                }
                else
                {
                    Utils.RunLongActionInThread(_lock, this, () =>
                    {
                        SyncFilesWithRobocopy.Go(settings);
                    });
                }
            }
            else
            {
                this.Visible = false;
                using (var childForm = new FormSortFilesList(
                    _action, settings, lblAction.Text, false))
                {
                    childForm.ShowDialog();
                }

                this.Close();
            }
        }

        private void btnShowRobo_Click(object sender, EventArgs e)
        {
            Start(true);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            Start(false);
        }

        private void btnSetLeftDir_Click(object sender, EventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                cmbLeftDir.Text = dlg.SelectedPath;
            }
        }

        private void btnSetRightDir_Click(object sender, EventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                cmbRightDir.Text = dlg.SelectedPath;
            }
        }

        private void CmbOnDragEnter(object sender, DragEventArgs e)
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

        private void CmbOnDragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (filePaths.Length > 0 && !string.IsNullOrEmpty(filePaths[0]))
                {
                    (sender as ComboBox).Text = filePaths[0];
                }
            }
        }
    }
}
