﻿using System;
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
    public partial class FormPersonalMusic : Form
    {
        public FormPersonalMusic()
        {
            InitializeComponent();
        }

        public static void OnDragDropFiles(string[] paths)
        {
            SimpleLog.Current.WriteLog("Dragged " + paths.Length + " file(s)");
        }
    }
}
