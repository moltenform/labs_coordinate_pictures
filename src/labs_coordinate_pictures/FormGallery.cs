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
    public partial class FormGallery : Form
    {
        public FormGallery(ModeBase mode, string initialDirectory, string initialFilepath = null)
        {
            InitializeComponent();
            this.movePrevMenuItem.ShortcutKeyDisplayString = "Left";
            this.moveNextMenuItem.ShortcutKeyDisplayString = "Right";
        }
    }
}
