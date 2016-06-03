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
        PersistMostRecentlyUsedList _mruHistorySrc;
        PersistMostRecentlyUsedList _mruHistoryDest;
        SortFilesAction _action;

        public FormSortFiles(SortFilesAction action)
        {
            InitializeComponent();

            _action = action;
            _mruHistorySrc = new PersistMostRecentlyUsedList(InputBoxHistory.SyncDirectorySrc);
            _mruHistoryDest = new PersistMostRecentlyUsedList(InputBoxHistory.SyncDirectoryDest);

            RefreshComboListItems();
            cmbLeftDir.Text = cmbLeftDir.Items.Count > 0 ? cmbLeftDir.Items[0].ToString() : "";
            cmbRightDir.Text = cmbRightDir.Items.Count > 0 ? cmbRightDir.Items[0].ToString() : "";

            if (action != SortFilesAction.SyncFiles)
            {
                btnStart.Text = "Start...";
                btnShowRobo.Visible = false;
                txtShowRobo.Visible = false;
                checkMirror.Visible = false;
                lblSkipDirs.Visible = false;
                lblSkipFiles.Visible = false;
                txtSkipDirs.Visible = false;
                txtSkipFiles.Visible = false;
                lblLeftDirDesc.Text = "Directory #1:";
                lblRightDirDesc.Text = "Directory #2:";
                checkAllowDiffer.Text = checkAllowDiffer.Text.Replace("2", "4");
            }

            if (action == SortFilesAction.FindMovedFiles)
            {
                lblAction.Text = "Find moved files. Look for differences between directories from filenames and last modified times,\r\n and show which of these differences are simply moved files.";
            }
            else if (action == SortFilesAction.FindDupeFiles)
            {
                lblAction.Text = "Find duplicate files. Look for files with identical contents.";
            }
            else if (action == SortFilesAction.FindDupeFilesInOneDir)
            {
                lblAction.Text = "Find duplicate files. Look for files with identical contents.";
                lblLeftDirDesc.Text = "Directory:";
                lblRightDirDesc.Text = "";
                btnSetRightDir.Visible = false;
                cmbRightDir.Visible = false;
                btnSwap.Visible = false;
                lblShiftTimeHours.Visible = false;
                txtShiftTimeHours.Visible = false;
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

            foreach (var s in _mruHistorySrc.Get())
            {
                cmbLeftDir.Items.Add(s);
            }

            foreach (var s in _mruHistoryDest.Get())
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
                checkAllowDiffer.Checked, txtShiftTimeHours.Text,
                checkMirror.Checked);
        }

        public static SortFilesSettings FillFromUI(SortFilesAction action,
            string skipDirs, string skipFiles,
            string dirSrc, string dirDest,
            bool allowTimesDiffer, string shiftTimeHours,
            bool mirror)
        {
            var settings = new SortFilesSettings();
            settings.SkipDirectories = TextLineByLineToList(skipDirs);
            settings.SkipFiles = TextLineByLineToList(skipFiles);
            settings.SourceDirectory = dirSrc;
            settings.DestDirectory = dirDest;
            settings.AllowFiletimesDifferForFAT = allowTimesDiffer;
            settings.Mirror = mirror;
            settings.LogFile = Path.Combine(TestUtil.GetTestWriteDirectory(),
                "log" + Utils.GetRandomDigits() + ".txt");

            int shiftTimes = 0;
            if (int.TryParse(shiftTimeHours, out shiftTimes))
            {
                settings.ShiftFiletimeHours = shiftTimes;
            }
            else
            {
                Utils.MessageErr("Not a valid time shift in hours.", true);
                return null;
            }

            if (action == SortFilesAction.SyncFiles && shiftTimes != 0 &&
                shiftTimes != 1)
            {
                Utils.MessageErr("Robocopy only supports DST compensation of 0 hours or 1 hour, but got " +
                    shiftTimes + ".", true);
                return null;
            }
            else if (!Directory.Exists(settings.SourceDirectory))
            {
                Utils.MessageErr("Directory does not exist " + settings.SourceDirectory, true);
                return null;
            }
            else if (!Directory.Exists(settings.DestDirectory) && action != SortFilesAction.FindDupeFilesInOneDir)
            {
                Utils.MessageErr("Directory does not exist " + settings.DestDirectory, true);
                return null;
            }

            if (settings.SourceDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()) ||
                settings.DestDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                Utils.MessageErr("directory should not end with slash.", true);
                return null;
            }

            if (action != SortFilesAction.FindDupeFilesInOneDir &&
                (settings.SourceDirectory.StartsWith(settings.DestDirectory) ||
                settings.DestDirectory.StartsWith(settings.SourceDirectory)))
            {
                Utils.MessageErr("directories must be distinct.", true);
                return null;
            }

            return settings;
        }

        void Start(bool showCommandLineOnly)
        {
            _mruHistorySrc.AddToHistory(cmbLeftDir.Text);
            _mruHistoryDest.AddToHistory(cmbRightDir.Text);
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
                    var args = RobocopySyncFiles.GetFullArgs(settings);
                    txtShowRobo.Text = args;
                }
                else
                {
                    Utils.RunLongActionInThread(this, new Action(() =>
                    {
                        RobocopySyncFiles.Run(settings);
                    }));
                }
            }
            else
            {
                this.Visible = false;
                using (var childForm = new FormSortFilesList(_action, settings))
                {
                    childForm.ShowDialog();
                }

                this.Visible = true;
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
    }

    public enum SortFilesAction
    {
        FindMovedFiles,
        FindDupeFiles,
        FindDupeFilesInOneDir,
        SyncFiles
    }
}
