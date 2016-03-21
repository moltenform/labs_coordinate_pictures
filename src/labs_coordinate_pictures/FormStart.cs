using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
            if (Utils.Debug)
            {
                CoordinatePicturesTests.RunTests();
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
