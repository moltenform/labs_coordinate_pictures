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
        ModeBase _mode;
        string _root;
        List<ToolStripItem> _originalCategoriesMenu;
        List<ToolStripItem> _originalEditMenu;
        public FormGallery(ModeBase mode, string initialDirectory, string initialFilepath = null)
        {
            InitializeComponent();
            _mode = mode;
            _root = initialDirectory;

            _originalCategoriesMenu = new List<ToolStripItem>(categoriesToolStripMenuItem.DropDownItems.Cast<ToolStripItem>());
            _originalEditMenu = new List<ToolStripItem>(editToolStripMenuItem.DropDownItems.Cast<ToolStripItem>());
            movePrevMenuItem.ShortcutKeyDisplayString = "Left";
            movePrevMenuItem.Click += (sender, e) => MoveOne(false);
            moveNextMenuItem.ShortcutKeyDisplayString = "Right";
            moveNextMenuItem.Click += (sender, e) => MoveOne(true);
            moveManyPrevToolStripMenuItem.ShortcutKeyDisplayString = "PgUp";
            moveManyPrevToolStripMenuItem.Click += (sender, e) => MoveMany(false);
            moveManyNextToolStripMenuItem.ShortcutKeyDisplayString = "PgDn";
            moveManyNextToolStripMenuItem.Click += (sender, e) => MoveMany(true);
            moveFirstToolStripMenuItem.ShortcutKeyDisplayString = "Home";
            moveFirstToolStripMenuItem.Click += (sender, e) => MoveFirst(false);
            moveLastToolStripMenuItem.ShortcutKeyDisplayString = "End";
            moveLastToolStripMenuItem.Click += (sender, e) => MoveFirst(true);
            convertResizeImageToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+[";
            convertToSeveralJpgsInDifferentQualitiesToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+[";
            keepAndDeleteOthersToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+]";
            renameToolStripMenuItem.ShortcutKeyDisplayString = "H";
            finishedCategorizingToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Enter";
        }

        private void MoveOne(bool v)
        {
            throw new NotImplementedException();
        }
        private void MoveMany(bool v)
        {
            throw new NotImplementedException();
        }
        private void MoveFirst(bool v)
        {
            throw new NotImplementedException();
        }

        void RefreshCategories()
        {
            categoriesToolStripMenuItem.DropDownItems.Clear();
            foreach (var item in _originalCategoriesMenu)
                categoriesToolStripMenuItem.DropDownItems.Add(item);
        }
    }
}
