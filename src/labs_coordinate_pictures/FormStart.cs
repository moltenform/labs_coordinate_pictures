// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public partial class FormStart : Form
    {
        public FormStart()
        {
            InitializeComponent();
            HideOrShowMenus();

            this.setTrashDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsDir(sender, "When pressing Delete to 'move to trash', files will be moved to this directory.", ConfigKey.FilepathTrash);
            this.setAltImageEditorDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "(Optional) Choose an alternative image editor.", ConfigKey.FilepathAltEditorImage);
            this.setPythonLocationToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "Locate python.exe; currently only Python 2 is supported.", ConfigKey.FilepathPython);
            this.setWinMergeDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "(Optional) Locate winmerge.exe or another diff/merge application.", ConfigKey.FilepathWinMerge);
            this.setJpegCropDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "(Optional) Locate jpegcrop.exe or another jpeg crop/rotate application.", ConfigKey.FilepathJpegCrop);
            this.setMozjpegDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "Locate cjpeg.exe from mozjpeg (can be freely downloaded from Mozilla).", ConfigKey.FilepathMozJpeg);
            this.setWebpDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "Locate cwebp.exe from webp (can be freely downloaded from Google)", ConfigKey.FilepathWebp);
            this.setMediaPlayerDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "Choose application for playing audio.", ConfigKey.FilepathMediaPlayer);
            this.setMediaEditorDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "(Optional) Choose application for editing audio, such as Audacity.", ConfigKey.FilepathMediaEditor);
            this.setCreateSyncDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "(Optional) Locate 'create synchronicity.exe'", ConfigKey.FilepathCreateSync);
            this.setCoordmusicLocationToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsDir(sender, "(Optional) Locate coordinate_music directory containing main.py.", ConfigKey.FilepathCoordMusicDirectory);
            this.setDropqpyLocationToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsDir(sender, "(Optional) Locate encoder directory containing dropq128.py.", ConfigKey.FilepathEncodeMusicDropQDirectory);
            this.setMp3DirectCutToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "(Optional) Locate mp3directcut.exe.", ConfigKey.FilepathMp3DirectCut);
            this.setExiftoolLocationToolStripMenuItem.Click += (sender, e) =>
               OnSetConfigsFile(sender, "(Optional) Locate exiftool.exe.", ConfigKey.FilepathExifTool);
            this.categorizeAndRenamePicturesToolStripMenuItem.Click += (sender, e) =>
                OpenForm(new ModeCategorizeAndRename(), InputBoxHistory.OpenImageDirectory);
            this.checkFilesizesToolStripMenuItem.Click += (sender, e) =>
                OpenForm(new ModeCheckFilesizes(), InputBoxHistory.OpenImageDirectory);
            this.resizePhotosKeepingExifsToolStripMenuItem.Click += (sender, e) =>
                OpenForm(new ModeResizeKeepExif(), InputBoxHistory.OpenImageKeepExifDirectory);
            this.markwavQualityToolStripMenuItem.Click += (sender, e) =>
                OpenForm(new ModeMarkWavQuality(), InputBoxHistory.OpenWavAudioDirectory);
            this.markmp3QualityToolStripMenuItem.Click += (sender, e) =>
                OpenForm(new ModeMarkMp3Quality(), InputBoxHistory.OpenAudioDirectory);

            if (Utils.IsDebug())
            {
                CoordinatePicturesTests.RunTests();
            }

            if (Environment.GetCommandLineArgs().Length > 1 && Configs.Current.GetBool(ConfigKey.EnablePersonalFeatures))
            {
                OpenAudioFileInGallery(Environment.GetCommandLineArgs()[1]);
            }
        }

        void OpenAudioFileInGallery(string path)
        {
            throw new NotImplementedException();
        }

        string AskUserForDirectory(ModeBase mode, InputBoxHistory mruKey)
        {
            return InputBoxForm.GetStrInput("Enter directory:", null, mruKey, mustBeDirectory: true);
        }

        void OpenForm(ModeBase mode, InputBoxHistory mruKey)
        {
            VerifyProgramChecksums();
            var dir = AskUserForDirectory(mode, mruKey);
            if (dir == null)
                return;

            new FormGallery(mode, dir).Show();
        }

        private void OnSetConfigsDir(object sender, string info, ConfigKey key)
        {
            var prompt = (sender as ToolStripItem).Text;
            var res = InputBoxForm.GetStrInput(prompt + Environment.NewLine + info, Configs.Current.Get(key), mustBeDirectory: true);
            if (!string.IsNullOrEmpty(res))
            {
                Configs.Current.Set(key, res);
                VerifyProgramChecksums();
            }
        }

        private void OnSetConfigsFile(object sender, string info, ConfigKey key)
        {
            var prompt = (sender as ToolStripItem).Text;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = ".exe";
            dialog.Filter = "Exe files (*.exe)|*.exe";
            dialog.Title = prompt + " " + info;
            dialog.CheckPathExists = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Configs.Current.Set(key, dialog.FileName);
                VerifyProgramChecksums();
            }
        }

        private void FormStart_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Shift && e.Control && !e.Alt && e.KeyCode == Keys.T)
            {
                CoordinatePicturesTests.RunTests();
                MessageBox.Show("Tests complete.");
            }
            else if (!e.Shift && e.Control && !e.Alt && e.KeyCode == Keys.L)
            {
                bool nextState = !Configs.Current.GetBool(ConfigKey.EnableVerboseLogging);
                Configs.Current.SetBool(ConfigKey.EnableVerboseLogging, nextState);
                MessageBox.Show("Set verbose logging to " + nextState);
            }
            else if (!e.Shift && e.Control && e.Alt && e.KeyCode == Keys.E)
            {
                bool nextState = !Configs.Current.GetBool(ConfigKey.EnablePersonalFeatures);
                Configs.Current.SetBool(ConfigKey.EnablePersonalFeatures, nextState);
                MessageBox.Show("Set personal features to " + nextState);
                HideOrShowMenus();
            }
        }

        void HideOrShowMenus()
        {
            ToolStripItem[] menusPersonalOnly = new ToolStripItem[]
            {
                this.toolStripMenuItem1,
                this.toolStripMenuItem2,
                this.resizePhotosKeepingExifsToolStripMenuItem,
                this.checkFilesizesToolStripMenuItem,
                this.markwavQualityToolStripMenuItem,
                this.markmp3QualityToolStripMenuItem,
                this.setMediaEditorDirectoryToolStripMenuItem,
                this.setMediaPlayerDirectoryToolStripMenuItem,
                this.setCreateSyncDirectoryToolStripMenuItem,
                this.setCoordmusicLocationToolStripMenuItem,
                this.setDropqpyLocationToolStripMenuItem
            };
            foreach (var item in menusPersonalOnly)
            {
                item.Visible = Configs.Current.GetBool(ConfigKey.EnablePersonalFeatures);
            }
        }

        private void syncDirectoriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void FormStart_DragEnter(object sender, DragEventArgs e)
        {
            if (Configs.Current.GetBool(ConfigKey.EnablePersonalFeatures) && e.Data.GetDataPresent(DataFormats.FileDrop))
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
            string pathlower = path.ToLowerInvariant();
            if (pathlower.EndsWith(".url"))
            {
                Utils.Run(path, null, hideWindow: true, waitForExit: false, shell: true);
            }
            else if (pathlower.EndsWith(".mp3") || pathlower.EndsWith(".mp4") ||
                pathlower.EndsWith(".m4a") || pathlower.EndsWith(".flac"))
            {
                var script = Path.Combine(Configs.Current.Get(ConfigKey.FilepathCoordMusicDirectory), "main.py");
                if (!File.Exists(script))
                {
                    MessageBox.Show("could not find " + script + ". locate it by choosing from the menu Options->Set coordmusic location...");
                }
                else
                {
                    Utils.RunPythonScriptOnSeparateThread(script, new string[] { "startspotify", path }, true /*create window*/);
                }
            }
            else
            {
                MessageBox.Show("Unsupported file type.");
            }
        }

        static Tuple<ConfigKey, string, ConfigKey>[] verifyChecksumsKeys = new Tuple<ConfigKey, string, ConfigKey>[]
        {
            Tuple.Create(ConfigKey.FilepathAltEditorImage, "", ConfigKey.FilepathChecksumAltEditorImage),
            Tuple.Create(ConfigKey.FilepathPython, "", ConfigKey.FilepathChecksumPython),
            Tuple.Create(ConfigKey.FilepathWinMerge, "", ConfigKey.FilepathChecksumWinMerge),
            Tuple.Create(ConfigKey.FilepathJpegCrop, "", ConfigKey.FilepathChecksumJpegCrop),
            Tuple.Create(ConfigKey.FilepathMozJpeg, "", ConfigKey.FilepathChecksumMozJpeg),
            Tuple.Create(ConfigKey.FilepathWebp, "", ConfigKey.FilepathChecksumCWebp),
            Tuple.Create(ConfigKey.FilepathWebp, ".dwebp.exe", ConfigKey.FilepathChecksumDWebp),
            Tuple.Create(ConfigKey.FilepathMediaPlayer, "", ConfigKey.FilepathChecksumMediaPlayer),
            Tuple.Create(ConfigKey.FilepathMediaEditor, "", ConfigKey.FilepathChecksumMediaEditor),
            Tuple.Create(ConfigKey.FilepathCreateSync, "", ConfigKey.FilepathChecksumCreateSync),
            Tuple.Create(ConfigKey.FilepathEncodeMusicDropQDirectory, "/qaac.exe", ConfigKey.FilepathChecksumEncodeMusicDropQ),
            Tuple.Create(ConfigKey.FilepathMp3DirectCut, "", ConfigKey.FilepathChecksumMp3DirectCut),
            Tuple.Create(ConfigKey.FilepathExifTool, "", ConfigKey.FilepathChecksumExifTool),
        };

        public static void VerifyProgramChecksums()
        {
            foreach (var tuple in verifyChecksumsKeys)
            {
                if (!string.IsNullOrEmpty(Configs.Current.Get(tuple.Item1)))
                {
                    var path = Configs.Current.Get(tuple.Item1);
                    if (tuple.Item2.StartsWith("."))
                        path = Path.GetDirectoryName(path) + "\\" + tuple.Item2.Substring(1);
                    else if (tuple.Item2.StartsWith("/"))
                        path = path + "\\" + tuple.Item2.Substring(1);

                    var hashNow = Utils.GetSha512(path);
                    var hashExpected = Configs.Current.Get(tuple.Item3);
                    if (hashExpected != hashNow)
                    {
                        if (Utils.AskToConfirm("Checksum does not match for file " + path + "\r\nwas:" + hashExpected + "\r\nnow: " + hashNow + "\r\nDid you recently upgrade or change this program? If so, click Yes. Otherwise, click No to exit."))
                            Configs.Current.Set(tuple.Item3, hashNow);
                        else
                            Environment.Exit(1);
                    }
                }
            }
        }
    }
}
