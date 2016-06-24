// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public partial class FormStart : Form
    {
        public FormStart()
        {
            InitializeComponent();
            HideOrShowMenus();

            // provide Click events for menu items.
            setTrashDirectoryToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
                "When pressing Delete to 'move to trash', files will be moved to this directory.",
                o, ConfigKey.FilepathTrash);
            setAltImageEditorDirectoryToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
                "(Optional) Choose an alternative image editor.",
                o, ConfigKey.FilepathAltEditorImage);
            setPythonLocationToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
                "Locate python.exe; currently only Python 2 is supported.",
                o, ConfigKey.FilepathPython);
            setWinMergeDirectoryToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
                "(Optional) Locate winmerge.exe or another diff/merge application.",
                o, ConfigKey.FilepathWinMerge);
            setJpegCropDirectoryToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
                "(Optional) Locate jpegcrop.exe or another jpeg crop/rotate application.",
                o, ConfigKey.FilepathJpegCrop);
            setMozjpegDirectoryToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
                "Locate cjpeg.exe from mozjpeg (can be freely downloaded from Mozilla).",
                o, ConfigKey.FilepathMozJpeg);
            setWebpDirectoryToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
                "Locate cwebp.exe from webp (can be freely downloaded from Google)",
                o, ConfigKey.FilepathWebp);
            setMediaPlayerDirectoryToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
                "Choose application for playing audio.",
                o, ConfigKey.FilepathMediaPlayer);
            setMediaEditorDirectoryToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
                "(Optional) Choose application for editing audio, such as Audacity.",
                o, ConfigKey.FilepathMediaEditor);
            setCreateSyncDirectoryToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
                "(Optional) Locate 'create synchronicity.exe'",
                o, ConfigKey.FilepathCreateSync);
            setCoordmusicLocationToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
                "(Optional) Locate coordinate_music directory containing main.py.",
                o, ConfigKey.FilepathCoordMusicDirectory);
            setDropqpyLocationToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
                "(Optional) Locate encoder directory containing dropq128.py.",
                o, ConfigKey.FilepathEncodeMusicDropQDirectory);
            setMp3DirectCutToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
                "(Optional) Locate mp3directcut.exe.",
                o, ConfigKey.FilepathMp3DirectCut);
            setExiftoolLocationToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
               "(Optional) Locate exiftool.exe.",
               o, ConfigKey.FilepathExifTool);
            setSortmusicStagingLocationToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
               "(Optional) Set sortmusic staging directory.",
               o, ConfigKey.FilepathSortMusicToLibraryStagingDirectory);
            setSorttwitterSourceLocationToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
               "(Optional) Set sorttwitter input directory.",
               o, ConfigKey.FilepathSortTwitterImagesSourceDirectory);
            setSorttwitterDestinationLocationToolStripMenuItem.Click += (o, e) =>
                OnSetConfigsDir(
                "(Optional) Set sorttwitter output directory.",
                o, ConfigKey.FilepathSortTwitterImagesDestinationDirectory);

            categorizeAndRenamePicturesToolStripMenuItem.Click += (o, e) =>
                OpenForm(new ModeCategorizeAndRename(), InputBoxHistory.OpenImageDirectory);
            checkFilesizesToolStripMenuItem.Click += (o, e) =>
                OpenForm(new ModeCheckFilesizes(), InputBoxHistory.OpenImageDirectory);
            resizePhotosKeepingExifsToolStripMenuItem.Click += (o, e) =>
                OpenForm(new ModeResizeKeepExif(), InputBoxHistory.OpenImageKeepExifDirectory);
            markwavQualityToolStripMenuItem.Click += (o, e) =>
                OpenForm(new ModeMarkWavQuality(), InputBoxHistory.OpenWavAudioDirectory);
            markmp3QualityToolStripMenuItem.Click += (o, e) =>
                OpenForm(new ModeMarkMp3Quality(), InputBoxHistory.OpenAudioDirectory);

            if (Utils.IsDebug())
            {
                TestUtil.RunTests();
            }

            if (Environment.GetCommandLineArgs().Length > 1 &&
                Configs.Current.GetBool(ConfigKey.EnablePersonalFeatures))
            {
                OpenAudioFileInGallery(Environment.GetCommandLineArgs()[1]);
            }
        }

        static void OpenAudioFileInGallery(string path)
        {
            throw new NotImplementedException(path);
        }

        static string AskUserForDirectory(InputBoxHistory mruKey)
        {
            return InputBoxForm.GetStrInput(
                "Enter directory:", null, mruKey, mustBeDirectory: true);
        }

        void ShowForm(Form form)
        {
            this.Visible = false;
            using (form)
            {
                form.ShowDialog(this);
            }

            this.Visible = true;
        }

        void OpenForm(ModeBase mode, InputBoxHistory mruKey)
        {
            VerifyAllProgramChecksums();
            var directory = AskUserForDirectory(mruKey);
            if (directory == null)
            {
                return;
            }

            ShowForm(new FormGallery(mode, directory));
        }

        static void OnSetConfigsDir(string info, object sender, ConfigKey key)
        {
            var message = (sender as ToolStripItem).Text;
            message += Environment.NewLine + info;
            var chosenDirectory = InputBoxForm.GetStrInput(message,
                Configs.Current.Get(key), mustBeDirectory: true);

            if (!string.IsNullOrEmpty(chosenDirectory))
            {
                Configs.Current.Set(key, chosenDirectory);
                VerifyAllProgramChecksums();
            }
        }

        static void OnSetConfigsFile(object sender, string info, ConfigKey key)
        {
            var message = (sender as ToolStripItem).Text;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = ".exe";
            dialog.Filter = "Exe files (*.exe)|*.exe";
            dialog.Title = message + " " + info;
            dialog.CheckPathExists = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Configs.Current.Set(key, dialog.FileName);
                VerifyAllProgramChecksums();
            }
        }

        private void FormStart_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Shift && e.Control && !e.Alt && e.KeyCode == Keys.T)
            {
                TestUtil.RunTests();
                Utils.MessageBox("Tests complete.");
            }
            else if (!e.Shift && e.Control && !e.Alt && e.KeyCode == Keys.L)
            {
                bool nextState = !Configs.Current.GetBool(ConfigKey.EnableVerboseLogging);
                Configs.Current.SetBool(ConfigKey.EnableVerboseLogging, nextState);
                Utils.MessageBox("verbose logging set to " + nextState);
            }
            else if (!e.Shift && e.Control && e.Alt && e.KeyCode == Keys.E)
            {
                bool nextState = !Configs.Current.GetBool(ConfigKey.EnablePersonalFeatures);
                Configs.Current.SetBool(ConfigKey.EnablePersonalFeatures, nextState);
                Utils.MessageBox("personal features set to " + nextState);
                HideOrShowMenus();
            }
        }

        void HideOrShowMenus()
        {
            var menusPersonalOnly = new ToolStripItem[]
            {
                this.toolStripMenuItem1,
                this.toolStripMenuItem2,
                this.toolStripMenuItem3,
                this.resizePhotosKeepingExifsToolStripMenuItem,
                this.markwavQualityToolStripMenuItem,
                this.markmp3QualityToolStripMenuItem,
                this.sortTwitterToolStripMenuItem,
                this.sortMusicToolStripMenuItem,
                this.setMediaEditorDirectoryToolStripMenuItem,
                this.setMediaPlayerDirectoryToolStripMenuItem,
                this.setCreateSyncDirectoryToolStripMenuItem,
                this.setCoordmusicLocationToolStripMenuItem,
                this.setDropqpyLocationToolStripMenuItem,
                this.setMp3DirectCutToolStripMenuItem,
                this.setExiftoolLocationToolStripMenuItem,
                this.setSortmusicStagingLocationToolStripMenuItem,
                this.setSorttwitterSourceLocationToolStripMenuItem,
                this.setSorttwitterDestinationLocationToolStripMenuItem
            };

            foreach (var item in menusPersonalOnly)
            {
                item.Visible = Configs.Current.GetBool(ConfigKey.EnablePersonalFeatures);
            }
        }

        private void FormStart_DragEnter(object sender, DragEventArgs e)
        {
            if (Configs.Current.GetBool(ConfigKey.EnablePersonalFeatures) &&
                e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void FormStart_DragDrop(object sender, DragEventArgs e)
        {
            if (Configs.Current.GetBool(ConfigKey.EnablePersonalFeatures))
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
                {
                    string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                    string filePath = filePaths[0];
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        OnStartSpotify(filePath);
                    }
                }
            }
        }

        public static void OnStartSpotify(string path)
        {
            if (path.ToLowerInvariant().EndsWith(".url", StringComparison.Ordinal))
            {
                Utils.Run(path, null, hideWindow: true, waitForExit: false, shellExecute: true);
            }
            else if (FilenameUtils.LooksLikeAudio(path))
            {
                var script = Path.Combine(Configs.Current.Get(
                    ConfigKey.FilepathCoordMusicDirectory), "main.py");

                if (!File.Exists(script))
                {
                    Utils.MessageErr("could not find " + script + ".locate it by " +
                        "choosing from the menu Options->Set coordmusic location...");
                }
                else
                {
                    Utils.RunPythonScriptOnSeparateThread(script,
                        new string[] { "startspotify", path }, createWindow: true);
                }
            }
            else
            {
                Utils.MessageErr("Unsupported file type.");
            }
        }

        public static void VerifyAllProgramChecksums()
        {
            VerifyChecksum(ConfigKey.FilepathPython, ConfigKey.FilepathChecksumPython);
            VerifyChecksum(ConfigKey.FilepathWinMerge, ConfigKey.FilepathChecksumWinMerge);
            VerifyChecksum(ConfigKey.FilepathJpegCrop, ConfigKey.FilepathChecksumJpegCrop);
            VerifyChecksum(ConfigKey.FilepathMozJpeg, ConfigKey.FilepathChecksumMozJpeg);
            VerifyChecksum(ConfigKey.FilepathWebp, ConfigKey.FilepathChecksumCWebp);
            VerifyChecksum(ConfigKey.FilepathMediaPlayer, ConfigKey.FilepathChecksumMediaPlayer);
            VerifyChecksum(ConfigKey.FilepathMediaEditor, ConfigKey.FilepathChecksumMediaEditor);
            VerifyChecksum(ConfigKey.FilepathCreateSync, ConfigKey.FilepathChecksumCreateSync);
            VerifyChecksum(ConfigKey.FilepathMp3DirectCut, ConfigKey.FilepathChecksumMp3DirectCut);
            VerifyChecksum(ConfigKey.FilepathExifTool, ConfigKey.FilepathChecksumExifTool);
            VerifyChecksum(ConfigKey.FilepathAltEditorImage,
                ConfigKey.FilepathChecksumAltEditorImage);
            VerifyChecksum(ConfigKey.FilepathEncodeMusicDropQDirectory,
                ConfigKey.FilepathChecksumEncodeMusicDropQ, "/qaac.exe");
        }

        public static void VerifyChecksum(ConfigKey key, ConfigKey sumkey, string appendToPath = "")
        {
            if (!string.IsNullOrEmpty(Configs.Current.Get(key)))
            {
                var path = Configs.Current.Get(key);
                path += appendToPath;

                var hash = Utils.GetSha512(path);
                var hashExpected = Configs.Current.Get(sumkey);
                if (hashExpected != hash)
                {
                    if (Utils.AskToConfirm("Checksum does not match for file " +
                        path + Utils.NL + "was:" + hashExpected + Utils.NL + "now: " + hash +
                        Utils.NL + "Did you recently upgrade or change this program? " +
                        "If so, click Yes. Otherwise, click No to exit."))
                    {
                        Configs.Current.Set(sumkey, hash);
                    }
                    else
                    {
                        Environment.Exit(1);
                    }
                }
            }
        }

        private void sortTwitterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(new FormPersonalTwitter());
        }

        private void sortMusicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(new FormPersonalMusic());
        }

        private void findMovedFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(new FormSortFiles(SortFilesAction.SearchDifferences));
        }

        private void findDuplicateFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(new FormSortFiles(SortFilesAction.SearchDuplicates));
        }

        private void findDuplicateFilesWithinOneFolderToolStripMenuItem_Click(
            object sender, EventArgs e)
        {
            ShowForm(new FormSortFiles(SortFilesAction.SearchDuplicatesInOneDir));
        }

        private void syncDirectoriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(new FormSortFiles(SortFilesAction.SyncFiles));
        }

        private void onlineDocumentationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.LaunchUrl(
                "https://github.com/downpoured/labs_coordinate_pictures/blob/master/README.md");
        }
    }
}
