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
    public partial class FormGallery : Form
    {
        ModeBase _mode;
        string _root;
        bool _enabled = true;
        List<ToolStripItem> _originalCategoriesMenu;
        List<ToolStripItem> _originalEditMenu;
        internal FileListNavigation nav;
        public FormGallery(ModeBase mode, string initialDirectory, string initialFilepath = "")
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

            ModeUtils.UseDefaultCategoriesIfFirstRun(mode);
            RefreshCategories();

            nav = new FileListNavigation(initialDirectory, _mode.GetFileTypes(), true, true, initialFilepath);
            OnOpenItem();
        }

        void OnOpenItem()
        {
            
        }

        void RefreshFilelist()
        {
            nav.Refresh();
            MoveFirst(false);
        }

        void MoveOne(bool forwardDirection)
        {
            nav.GoNextOrPrev(forwardDirection);
            OnOpenItem();
        }

        void MoveMany(bool forwardDirection)
        {
            for (int i=0; i<15; i++)
                nav.GoNextOrPrev(forwardDirection);
            OnOpenItem();
        }

        void MoveFirst(bool forwardDirection)
        {
            if (forwardDirection)
                nav.GoLast();
            else
                nav.GoFirst();
            OnOpenItem();
        }

        void RefreshCategories()
        {
            categoriesToolStripMenuItem.DropDownItems.Clear();
            foreach (var item in _originalCategoriesMenu)
                categoriesToolStripMenuItem.DropDownItems.Add(item);

            var categoriesString = Configs.Current.Get(_mode.GetCategories());
            var tuples = ModeUtils.CategoriesStringToTuple(categoriesString);
            foreach(var tuple in tuples)
            {

                if (Configs.Current.GetBool(ConfigKey.GalleryViewCategories))
                {

                }
            }
        }

        private void viewCategoriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var current = Configs.Current.GetBool(ConfigKey.GalleryViewCategories);
            Configs.Current.SetBool(ConfigKey.GalleryViewCategories, !current);
            RefreshCategories();
        }

        private void editCategoriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var suggestions = new string[] { Configs.Current.Get(_mode.GetCategories()),
                _mode.GetDefaultCategories() };
            var nextCategories = InputBoxForm.GetStrInput("xsdfgdfgdf", null, InputBoxHistory.EditCategoriesString, suggestions);
            //try it and see if it throws...
        }

        internal List<Tuple<string, string>> m_undoStack = new List<Tuple<string, string>>();
        internal int m_undoIndex = 0;
        public bool WrapMoveFile(string src, string target, bool fAddToUndoStack = true)
        {
            const int millisecondsToRetryMoving = 3000;
            if (File.Exists(target))
            {
                MessageBox.Show("already exists: " + target);
                return false;
            }

            if (!File.Exists(src))
            {
                MessageBox.Show("does not exist: " + src);
                return false;
            }

            SimpleLog.Current.WriteLog("Moving [" + src + "] to [" + target + "]");
            try
            {
                bool succeeded = Utils.RepeatWhileFileLocked(src, millisecondsToRetryMoving);
                if (!succeeded)
                {
                    SimpleLog.Current.WriteLog("Move failed, access denied.");
                    MessageBox.Show("File is locked: " + src);
                    return false;
                }

                File.Move(src, target);
            }
            catch (IOException e)
            {
                MessageBox.Show("IOException:" + e);
                return false;
            }

            if (fAddToUndoStack)
            {
                m_undoStack.Add(new Tuple<string, string>(src, target));
                m_undoIndex = m_undoStack.Count - 1;
            }
            return true;
        }

        public void UndoLastMove()
        {
            if (m_undoIndex < 0)
            {
                MessageBox.Show("nothing to undo");
            }
            else if (m_undoIndex >= m_undoStack.Count)
            {
                MessageBox.Show("invalid undo index");
                m_undoIndex = m_undoStack.Count - 1;
            }
            else
            {
                var newdest = m_undoStack[m_undoIndex].Item1;
                var newsrc = m_undoStack[m_undoIndex].Item2;
                if (Utils.AskToConfirm("move " + newsrc + " back to " + newdest + "?"))
                {
                    if (WrapMoveFile(newsrc, newdest, fAddToUndoStack: false))
                    {
                        m_undoIndex--;
                    }
                }
            }
        }

        private void FormGallery_KeyUp(object sender, KeyEventArgs e)
        {
            if (!_enabled)
                return;

            if (!e.Shift && !e.Control && !e.Alt)
            {
                if (e.KeyCode == Keys.F5) // not in menus, since shouldn't be needed
                    RefreshFilelist();
                else if (e.KeyCode == Keys.Delete)
                    KeyDelete();
                else if (e.KeyCode == Keys.Left)
                    MoveOne(false);
                else if (e.KeyCode == Keys.Right)
                    MoveOne(true);
                else if (e.KeyCode == Keys.PageUp)
                    MoveMany(false);
                else if (e.KeyCode == Keys.PageDown)
                    MoveMany(true);
                else if (e.KeyCode == Keys.Home)
                    MoveFirst(false);
                else if (e.KeyCode == Keys.End)
                    MoveFirst(true);
                else if (e.KeyCode == Keys.End)
                    RenameFile();
            }
            else if (e.Shift && !e.Control && !e.Alt)
            {
            }
            else if (!e.Shift && e.Control && !e.Alt)
            {
            }
        }

        void RenameFile()
        {
            
        }

        void KeyDelete()
        {
            if (nav.Current != null)
            {
                _mode.OnBeforeAssignCategory();
                var dest = Utils.GetSoftDeleteDestination(nav.Current);
                if (WrapMoveFile(nav.Current, dest))
                {
                    MoveOne(true);
                }
            }
        }

        internal void UIEnable()
        {
            this.label.ForeColor = Color.Black;
            _enabled = true;
        }

        internal void UIDisable()
        {
            this.label.ForeColor = Color.Gray;
            _enabled = false;
        }
    }
}
