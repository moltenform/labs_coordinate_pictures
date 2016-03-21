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
            ClassConfigs.Current.LoadPersisted();
            ClassConfigs.Current.Set(ConfigsPersistedKeys.Version, "0.1");
#if DEBUG
            CoordinatePicturesTests.RunTests();
#endif
        }

        private void FormStart_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Shift && e.Control && !e.Alt && e.KeyCode == Keys.T)
            {
                CoordinatePicturesTests.RunTests();
                MessageBox.Show("Tests complete.");
            }
        }
    }
}
