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
                OnSetConfigsDir(sender, "When pressing Delete to 'move to trash', files will be moved to this directory.", ConfigsPersistedKeys.FilepathMediaEditor);
            this.setAltImageEditorDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "(Optional) Choose an alternative image editor.", ConfigsPersistedKeys.FilepathAltEditorImage);
            this.setPythonLocationToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "Locate python.exe; currently only Python 2 is supported.", ConfigsPersistedKeys.FilepathPython);
            this.setWinMergeDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "(Optional) Locate winmerge.exe or another diff/merge application.", ConfigsPersistedKeys.FilepathWinMerge);
            this.setJpegCropDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "(Optional) Locate jpegcrop.exe or another jpeg crop/rotate application.", ConfigsPersistedKeys.FilepathJpegCrop);
            this.setMozjpegDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "Locate cjpeg.exe from mozjpeg (can be freely downloaded from Mozilla).", ConfigsPersistedKeys.FilepathMozJpeg);
            this.setWebpDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "Locate cwebp.exe from webp (can be freely downloaded from Google)", ConfigsPersistedKeys.FilepathWebp);
            this.setMediaPlayerDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "Choose application for playing audio.", ConfigsPersistedKeys.FilepathMediaPlayer);
            this.setMediaEditorDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "(Optional) Choose application for editing audio, such as Audacity.", ConfigsPersistedKeys.FilepathMediaEditor);
            this.setCreateSyncDirectoryToolStripMenuItem.Click += (sender, e) =>
                OnSetConfigsFile(sender, "(Optional) Locate 'create synchronicity.exe'", ConfigsPersistedKeys.FilepathCreateSync);
            
            if (Utils.Debug)
            {
                CoordinatePicturesTests.RunTests();
            }
        }

        private void OnSetConfigsDir(object sender, string info, ConfigsPersistedKeys key)
        {
            var prompt = (sender as ToolStripItem).Text;
            var res = InputBoxForm.GetStrInput(prompt + Environment.NewLine + info, Configs.Current.Get(key));
            if (String.IsNullOrEmpty(res))
            {
            }
            else if (!Directory.Exists(res))
            {
                MessageBox.Show("directory not found");
            }
            else
            {
                Configs.Current.Set(key, res);
            }
        }

        private void OnSetConfigsFile(object sender, string info, ConfigsPersistedKeys key)
        {
            var prompt = (sender as ToolStripItem).Text;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = ".exe";
            dialog.Filter = "Exe files (*.exe)|*.exe";
            dialog.Title = prompt + Environment.NewLine + info;
            dialog.CheckPathExists = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Configs.Current.Set(key, dialog.FileName);
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
                bool nextState = !Configs.Current.GetBool(ConfigsPersistedKeys.EnableVerboseLogging);
                Configs.Current.SetBool(ConfigsPersistedKeys.EnableVerboseLogging, nextState);
                MessageBox.Show("Set verbose logging to " + nextState);
            }
            else if (!e.Shift && e.Control && e.Alt && e.KeyCode == Keys.E)
            {
                bool nextState = !Configs.Current.GetBool(ConfigsPersistedKeys.EnablePersonalFeatures);
                Configs.Current.SetBool(ConfigsPersistedKeys.EnablePersonalFeatures, nextState);
                MessageBox.Show("Set personal features to " + nextState);
                HideOrShowMenus();
            }
        }

        void HideOrShowMenus()
        {
            ToolStripItem[] menusPersonalOnly = new ToolStripItem[] {
                this.toolStripMenuItem1,
                this.toolStripMenuItem2,
                this.secondPassThroughPicturesToCheckFilesizesToolStripMenuItem,
                this.markwavQualityToolStripMenuItem,
                this.markmp3QualityToolStripMenuItem,
                this.setMediaEditorDirectoryToolStripMenuItem,
                this.setMediaPlayerDirectoryToolStripMenuItem,
                this.setCreateSyncDirectoryToolStripMenuItem
            };
            foreach (var item in menusPersonalOnly)
            {
                item.Visible = Configs.Current.GetBool(ConfigsPersistedKeys.EnablePersonalFeatures);
            }
        }

        private void syncDirectoriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }
    }
}
