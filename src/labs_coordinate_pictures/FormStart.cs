// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
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

            // provide Click events for menu items (set dirs).
            setDirectoryForDeletedToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
                "When pressing Delete to 'move to trash', files will be moved to this directory.",
                o, ConfigKey.FilepathDeletedFilesDir);
            setSortmusicStagingLocationToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
               "(Optional) Set sortmusic staging directory.",
               o, ConfigKey.FilepathSortMusicToLibraryStagingDir);
            setSorttwitterSourceLocationToolStripMenuItem.Click += (o, e) => OnSetConfigsDir(
               "(Optional) Set sorttwitter input directory.",
               o, ConfigKey.FilepathSortTwitterImagesSourceDir);
            setSorttwitterDestinationLocationToolStripMenuItem.Click += (o, e) =>
                OnSetConfigsDir(
                "(Optional) Set sorttwitter output directory.",
                o, ConfigKey.FilepathSortTwitterImagesDestinationDir);

            // provide Click events for menu items (set files).
            setAudioCropStripMenuItem.Click += (o, e) => OnSetConfigsFile(
               "(Optional) Select an audio cropping tool, such as mp3directcut.exe.",
               o, ConfigKey.FilepathAudioCrop);
            setAudioEditorToolStripMenuItem.Click += (o, e) => OnSetConfigsFile(
                "(Optional) Select an application for editing audio, such as Audacity.",
                o, ConfigKey.FilepathAudioEditor);
            setAudioPlayerToolStripMenuItem.Click += (o, e) => OnSetConfigsFile(
                "(Optional) Select an application for playing audio.",
                o, ConfigKey.FilepathAudioPlayer);
            setCreateSyncToolStripMenuItem.Click += (o, e) => OnSetConfigsFile(
                "(Optional) Locate 'create synchronicity.exe'",
                o, ConfigKey.FilepathCreateSync);
            setCoordmusicLocationToolStripMenuItem.Click += (o, e) => OnSetConfigsFile(
                "(Optional) Locate coordinate_music directory's 'main.py'",
                o, ConfigKey.FilepathCoordMusicMainPy);
            setImageEditorLocationToolStripMenuItem.Click += (o, e) => OnSetConfigsFile(
                "(Optional) Select an application for editing images.",
                o, ConfigKey.FilepathImageEditor);
            setAltImageEditorToolStripMenuItem.Click += (o, e) => OnSetConfigsFile(
               "(Optional) Select another application for editing images.",
               o, ConfigKey.FilepathImageEditorAlt);
            setImageEditorJpegToolStripMenuItem.Click += (o, e) => OnSetConfigsFile(
                "(Optional) Select an application for editing jpgs, such as jpegcrop.exe.",
                o, ConfigKey.FilepathImageEditorJpeg);
            setImageEditorCropLocationToolStripMenuItem.Click += (o, e) => OnSetConfigsFile(
                "(Optional) Select an application for cropping jpgs, such as jpegcrop.exe.",
                o, ConfigKey.FilepathImageEditorCrop);
            setM4aEncoderLocationToolStripMenuItem.Click += (o, e) => OnSetConfigsFile(
               "(Optional) Locate 'qaac.exe'; directory should also contain 'dropq128.py'.",
               o, ConfigKey.FilepathM4aEncoder);
            setPythonLocationToolStripMenuItem.Click += (o, e) => OnSetConfigsFile(
                "Locate 'python.exe' from Python 3.",
                o, ConfigKey.FilepathPython);
            setWinMergeToolStripMenuItem.Click += (o, e) => OnSetConfigsFile(
                "(Optional) Select a diff/merge application, such as 'winmerge.exe'.",
                o, ConfigKey.FilepathWinMerge);

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
        }

        private void FormStart_Load(object sender, EventArgs e)
        {
            FindPythonDirectory();
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                OpenFileInGallery(Environment.GetCommandLineArgs()[1]);
            }
        }

        static ModeBase GuessModeBasedOnFileExtensions(IEnumerable<string> paths)
        {
            if (!Configs.Current.GetBool(ConfigKey.EnablePersonalFeatures))
            {
                return new ModeCheckFilesizes();
            }
            else if (paths.Any(path => FilenameUtils.LooksLikeImage(path)))
            {
                return new ModeCheckFilesizes();
            }
            else if (paths.All(path =>
                path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".flac", StringComparison.OrdinalIgnoreCase)))
            {
                return new ModeMarkWavQuality();
            }
            else
            {
                return new ModeMarkMp3Quality();
            }
        }

        void OpenFileInGallery(string path)
        {
            if (File.Exists(path))
            {
                var paths = new string[] { path };
                var mode = GuessModeBasedOnFileExtensions(paths);
                var form = new FormGallery(mode, Path.GetDirectoryName(path), path);
                ShowForm(form);
                Close();
            }
            else if (Directory.Exists(path))
            {
                var paths = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
                var form = new FormGallery(GuessModeBasedOnFileExtensions(paths), path);
                ShowForm(form);
                Close();
            }
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
            var directory = AskUserForDirectory(mruKey);
            if (directory == null)
            {
                return;
            }

            ShowForm(new FormGallery(mode, directory));
        }

        static void OnSetConfigsDir(string info, object sender, ConfigKey key)
        {
            var message = (sender as ToolStripItem).Text + Utils.NL + info;
            var chosenDirectory = InputBoxForm.GetStrInput(message,
                Configs.Current.Get(key), mustBeDirectory: true);

            if (!string.IsNullOrEmpty(chosenDirectory))
            {
                Configs.Current.Set(key, chosenDirectory);
            }
        }

        static void OnSetConfigsFile(string info, object sender, ConfigKey key)
        {
            var message = (sender as ToolStripItem).Text;
            var current = Configs.Current.Get(key);

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = message + " " + info;
            dialog.CheckPathExists = true;
            dialog.InitialDirectory = File.Exists(current) ?
                Path.GetDirectoryName(current) : null;

            if (key == ConfigKey.FilepathCoordMusicMainPy)
            {
                dialog.DefaultExt = ".py";
                dialog.Filter = "Py files (*.py)|*.py";
            }
            else
            {
                dialog.DefaultExt = ".exe";
                dialog.Filter = "Exe files (*.exe)|*.exe";
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Configs.Current.Set(key, dialog.FileName);
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
                toolStripPicturesSep1,
                toolStripPicturesSep2,
                markwavQualityToolStripMenuItem,
                markmp3QualityToolStripMenuItem,
                sortTwitterToolStripMenuItem,
                sortMusicToolStripMenuItem,
                setSortmusicStagingLocationToolStripMenuItem,
                setSorttwitterSourceLocationToolStripMenuItem,
                setSorttwitterDestinationLocationToolStripMenuItem,
                sortTextFilesToolStripMenuItem,
                setAudioCropStripMenuItem,
                setAudioEditorToolStripMenuItem,
                setAudioPlayerToolStripMenuItem,
                setCreateSyncToolStripMenuItem,
                setCoordmusicLocationToolStripMenuItem,
                setAltImageEditorToolStripMenuItem,
                setImageEditorCropLocationToolStripMenuItem,
                setM4aEncoderLocationToolStripMenuItem,
                setWinMergeToolStripMenuItem,
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
                var script = Configs.Current.Get(ConfigKey.FilepathCoordMusicMainPy);

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

        private void sortTextFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(new FormPersonalText());
        }

        private void onlineDocumentationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.LaunchUrl(
                "https://github.com/moltenjs/labs_coordinate_pictures/blob/master/README.md");
        }

        private static void FindPythonDirectory()
        {
            if (string.IsNullOrEmpty(Configs.Current.Get(ConfigKey.FilepathPython)))
            {
                var attempts = new string[] { @"C:\python34\python.exe",
                    @"C:\python35\python.exe",
                    @"C:\python36\python.exe",
                    @"C:\python37\python.exe",
                    @"C:\python38\python.exe" };
                foreach (var attempt in attempts)
                {
                    if (File.Exists(attempt))
                    {
                        Configs.Current.Set(ConfigKey.FilepathPython, attempt);
                        Configs.Current.Set(ConfigKey.FilepathChecksumPython,
                            Utils.GetSha512(attempt));
                        break;
                    }
                }
            }
        }
    }
}
