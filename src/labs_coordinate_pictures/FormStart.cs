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
            SimpleLog.Current.WriteLog("Initializing.");
            ClassConfigs.Current.LoadPersisted();
            ClassConfigs.Current.Set(ConfigsPersistedKeys.Version, "0.1");
            if (OsHelpers.Debug)
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
                bool nextState = !ClassConfigs.Current.GetBool(ConfigsPersistedKeys.EnableVerboseLogging);
                ClassConfigs.Current.SetBool(ConfigsPersistedKeys.EnableVerboseLogging, nextState);
                MessageBox.Show("Set verbose logging to " + nextState);
            }
            else if (!e.Shift && e.Control && e.Alt && e.KeyCode == Keys.E)
            {
                bool nextState = !ClassConfigs.Current.GetBool(ConfigsPersistedKeys.EnablePersonalFeatures);
                ClassConfigs.Current.SetBool(ConfigsPersistedKeys.EnablePersonalFeatures, nextState);
                MessageBox.Show("Set personal features to " + nextState);
            }
        }
    }
}
