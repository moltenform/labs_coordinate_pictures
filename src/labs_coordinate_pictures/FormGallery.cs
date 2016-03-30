using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public partial class FormGallery : Form
    {
        ModeBase _mode;
        bool _enabled = true;
        bool _zoomed = false;
        bool _currentImageResized = false;
        List<ToolStripItem> _originalCategoriesMenu;
        List<ToolStripItem> _originalEditMenu;
        Dictionary<string, string> _categoryShortcuts;
        List<string> _pathsToCache = new List<string>();
        internal FileListNavigation nav;
        internal ImageCache imcache;
        const int imagecachesize = 15;
        const int imagecachebatch = 8;
        public FormGallery(ModeBase mode, string initialDirectory, string initialFilepath = "")
        {
            InitializeComponent();

            for (int i = 0; i < imagecachebatch; i++)
                _pathsToCache.Add(null);

            SimpleLog.Current.WriteLog("Starting session in " + initialDirectory + "|" + initialFilepath);
            _mode = mode;
            _originalCategoriesMenu = new List<ToolStripItem>(categoriesToolStripMenuItem.DropDownItems.Cast<ToolStripItem>());
            _originalEditMenu = new List<ToolStripItem>(editToolStripMenuItem.DropDownItems.Cast<ToolStripItem>());
            pictureBox1.SizeMode = PictureBoxSizeMode.Normal;
            
            // event handlers
            movePrevMenuItem.Click += (sender, e) => MoveOne(false);
            moveNextMenuItem.Click += (sender, e) => MoveOne(true);
            moveManyPrevToolStripMenuItem.Click += (sender, e) => MoveMany(false);
            moveManyNextToolStripMenuItem.Click += (sender, e) => MoveMany(true);
            moveFirstToolStripMenuItem.Click += (sender, e) => MoveFirst(false);
            moveLastToolStripMenuItem.Click += (sender, e) => MoveFirst(true);
            moveToTrashToolStripMenuItem.Click += (sender, e) => KeyDelete();
            renameToolStripMenuItem.Click += (sender, e) => RenameFile(false);

            nav = new FileListNavigation(initialDirectory, _mode.GetFileTypes(), true, true, initialFilepath);
            ModeUtils.UseDefaultCategoriesIfFirstRun(mode);
            RefreshCategories();
            OnOpenItem();
        }

        void OnOpenItem()
        {
            // if the user resized the window, create a new cache for the new size
            pictureBox1.Image = ImageCache.BitmapBlank;
            if (imcache == null || imcache.MaxWidth != pictureBox1.Width || imcache.MaxHeight != pictureBox1.Height)
            {
                RefreshImageCache();
            }

            if (nav.Current == null)
            {
                label.Text = "looks done.";
                pictureBox1.Image = ImageCache.BitmapBlank;
            }
            else
            {
                // tell the mode we've opened something
                _mode.OnOpenItem(nav.Current, this);
                int nOrigW = 0, nOrigH = 0;
                pictureBox1.Image = imcache.Get(nav.Current, out nOrigW, out nOrigH);
                _currentImageResized = nOrigW > imcache.MaxWidth || nOrigH > imcache.MaxHeight;
                var showResized = _currentImageResized ? "s" : "";
                label.Text = string.Format("{0} {1}\r\n{2} {3}({4}x{5})", nav.Current,
                    Utils.FormatFilesize(nav.Current), Path.GetFileName(nav.Current),
                    showResized, nOrigW, nOrigH);
            }

            _zoomed = false;
        }

        void RefreshImageCache()
        {
            if (imcache != null)
            {
                pictureBox1.Image = null;
                imcache.Dispose();
            }

            Func<Bitmap, bool> canDisposeBitmap = 
                (bmp) => (bmp as object) != (pictureBox1.Image as object);
            Func<Action, bool> callbackOnUiThread =
                (act) => { this.Invoke((MethodInvoker)(() => act.Invoke())); return true; };
            imcache = new ImageCache(pictureBox1.Width, pictureBox1.Height, 
                imagecachesize, callbackOnUiThread, canDisposeBitmap);
        }

        void RefreshFilelist()
        {
            nav.Refresh();
            MoveFirst(false);
        }

        void MoveOne(bool forwardDirection)
        {
            for (int i = 0; i < _pathsToCache.Count; i++)
                _pathsToCache[i] = null;

            nav.GoNextOrPrev(forwardDirection, _pathsToCache, _pathsToCache.Count);
            OnOpenItem();
            imcache.AddAsync(_pathsToCache, pictureBox1);
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

        void RefreshCustomCommands()
        {
            if (_mode.GetDisplayCustomCommands().Length > 0)
            {
                editToolStripMenuItem.DropDownItems.Clear();
                foreach (var item in _originalEditMenu)
                    editToolStripMenuItem.DropDownItems.Add(item);

                editToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
                labelView.Text = "\r\n\r\n";
                foreach (var tuple in _mode.GetDisplayCustomCommands())
                {
                    var menuItem = new ToolStripMenuItem(tuple.Item2);
                    menuItem.ShortcutKeyDisplayString = tuple.Item1;
                    menuItem.Click += (sender, e) => MessageBox.Show(
                        "Press the shortcut " + tuple.Item1 + " to run this command.");
                    editToolStripMenuItem.DropDownItems.Add(menuItem);
                    labelView.Text += tuple.Item1 + "=" + tuple.Item2 + "\r\n\r\n";
                }
            }
        }


        void RefreshCategories()
        {
            RefreshCustomCommands();
            categoriesToolStripMenuItem.DropDownItems.Clear();
            foreach (var item in _originalCategoriesMenu)
                categoriesToolStripMenuItem.DropDownItems.Add(item);

            categoriesToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            var categoriesString = Configs.Current.Get(_mode.GetCategories());
            var tuples = ModeUtils.CategoriesStringToTuple(categoriesString);
            _categoryShortcuts = new Dictionary<string, string>();
            foreach (var tuple in tuples)
            {
                var menuItem = new ToolStripMenuItem(tuple.Item2);
                menuItem.ShortcutKeyDisplayString = tuple.Item1;
                menuItem.Click += (sender, e) => AssignCategory(tuple.Item3);
                categoriesToolStripMenuItem.DropDownItems.Add(menuItem);
                labelView.Text += tuple.Item1 + "=" + tuple.Item2 + "\r\n\r\n";
                this._categoryShortcuts[tuple.Item1] = tuple.Item3;
            }

            if (!Configs.Current.GetBool(ConfigKey.GalleryViewCategories))
            {
                labelView.Text = "";
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
            var suggestions = new string[] {
                Configs.Current.Get(_mode.GetCategories()),
                _mode.GetDefaultCategories() };
            var text = "Please enter a list of category strings separated by |. Each category string must be in the form A/categoryReadable/categoryId, where A is a single capital letter, categoryReadable will be the human-readable label, and categoryID will be the unique ID (when an image is given this ID, the ID will be added to the filename as a suffix).";
            var nextCategories = InputBoxForm.GetStrInput(text, null, InputBoxHistory.EditCategoriesString, suggestions);
            if (!string.IsNullOrEmpty(nextCategories))
            {
                try
                {
                    ModeUtils.CategoriesStringToTuple(nextCategories);
                }
                catch (CoordinatePicturesException exc)
                {
                    MessageBox.Show(exc.Message);
                    return;
                }

                Configs.Current.Set(_mode.GetCategories(), nextCategories);
                RefreshCategories();
            }
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

            // Use custom shortcut strings instead of ShortcutKeys
            // 1) allows shortcuts without Ctrl or Alt
            // 2) otherwise it uses KeyDown, fires many times if you hold the key
            if (!e.Shift && !e.Control && !e.Alt)
            {
                if (e.KeyCode == Keys.F5) // not in menus, shouldn't be needed
                    RefreshFilelist();
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
                else if (e.KeyCode == Keys.Delete)
                    KeyDelete();
                else if (e.KeyCode == Keys.H)
                    RenameFile(false);
            }
            else if (e.Shift && !e.Control && !e.Alt)
            {
                if (_categoryShortcuts.ContainsKey(e.KeyCode.ToString()))
                    AssignCategory(_categoryShortcuts[e.KeyCode.ToString()]);
            }
            else if (e.Shift && e.Control && !e.Alt)
            {
                if (e.KeyCode == Keys.E)
                    editInAltEditorToolStripMenuItem_Click(null, null);
                else if (e.KeyCode == Keys.X)
                    cropRotateFileToolStripMenuItem_Click(null, null);
                else if (e.KeyCode == Keys.H)
                    replaceInFilenameToolStripMenuItem_Click(null, null);
                else if (e.KeyCode == Keys.D3)
                    removeNumberedPrefixToolStripMenuItem_Click(null, null);
                else if (e.KeyCode == Keys.V)
                    viewCategoriesToolStripMenuItem_Click(null, null);
                else if (e.KeyCode == Keys.K)
                    editCategoriesToolStripMenuItem_Click(null, null);
            }
            else if (!e.Shift && e.Control && !e.Alt)
            {
                if (e.KeyCode == Keys.W)
                    showInExplorerToolStripMenuItem_Click(null, null);
                else if (e.KeyCode == Keys.C)
                    copyPathToolStripMenuItem_Click(null, null);
                else if (e.KeyCode == Keys.E)
                    editFileToolStripMenuItem_Click(null, null);
                else if (e.KeyCode == Keys.D3)
                    addNumberedPrefixToolStripMenuItem_Click(null, null);
            }
            else if (!e.Shift && !e.Control && e.Alt)
            {
                if (e.KeyCode == Keys.H) // not in menus, not comonly used
                    RenameFile(true);
            }

            _mode.OnCustomCommand(this, e.Shift, e.Control, e.Alt, e.KeyCode);
        }

        void AssignCategory(string categoryId)
        {
            _mode.OnBeforeAssignCategory();
            if (nav.Current == null)
                return;

            var newname = FilenameUtils.AddMarkToFilename(nav.Current, categoryId);
            if (WrapMoveFile(nav.Current, newname))
            {
                MoveOne(true);
            }
        }

        void RenameFile(bool overwriteNumberedPrefix)
        {
            if (!_mode.SupportsRename() || nav.Current == null)
                return;

            InputBoxHistory key = FilenameUtils.LooksLikeImage(nav.Current) ?
                InputBoxHistory.RenameImage : (FilenameUtils.IsExt(nav.Current, "wav") ?
                InputBoxHistory.RenameWavAudio : InputBoxHistory.RenameOther);

            var current = overwriteNumberedPrefix ? 
                Path.GetFileName(nav.Current):
                FilenameUtils.GetFileNameWithoutNumberedPrefix(nav.Current);
            var currentNoext = Path.GetFileNameWithoutExtension(current);
            var newname = InputBoxForm.GetStrInput("Enter a new name:", currentNoext, key);
            if (!string.IsNullOrEmpty(newname))
            {
                var fullnewname = Path.GetDirectoryName(nav.Current) + "\\" +
                    (current != Path.GetFileName(nav.Current) ? Path.GetFileName(nav.Current).Substring(0, 8) : "") +
                    newname + Path.GetExtension(nav.Current);

                if (WrapMoveFile(nav.Current, fullnewname))
                {
                    nav.NotifyFileChanges();
                    nav.TrySetPath(fullnewname);
                    OnOpenItem();
                }
            }
        }

        void KeyDelete()
        {
            if (nav.Current != null)
            {
                _mode.OnBeforeAssignCategory();
                var dest = Utils.GetSoftDeleteDestination(nav.Current);
                if (dest != null && WrapMoveFile(nav.Current, dest))
                {
                    MoveOne(true);
                }
            }
        }

        public void UIEnable()
        {
            this.label.ForeColor = Color.Black;
            fileToolStripMenuItem.Enabled = editToolStripMenuItem.Enabled =
                renameToolStripMenuItem.Enabled = categoriesToolStripMenuItem.Enabled = false;
            _enabled = true;
        }

        public void UIDisable()
        {
            this.label.ForeColor = Color.Gray;
            fileToolStripMenuItem.Enabled = editToolStripMenuItem.Enabled =
                renameToolStripMenuItem.Enabled = categoriesToolStripMenuItem.Enabled = true;
            _enabled = false;
        }

        private void showInExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (nav.Current == null)
                Utils.OpenDirInExplorer(nav.BaseDirectory);
            else
                Utils.SelectFileInExplorer(nav.Current);
        }

        private void copyPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(nav.Current ?? "");
        }

        static void LaunchEditor(string exe, string path)
        {
            if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
                MessageBox.Show("Could not find the application '" + exe + "'. The location can be set in the Options menu.");
            else
                Utils.Run(exe, new string[] { path }, shell: false, waitForExit: false, hideWindow: false);
        }

        private void editFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (FilenameUtils.IsExt(nav.Current, "webp"))
                Process.Start(nav.Current); // open in default viewer
            else if (FilenameUtils.LooksLikeEditableAudio(nav.Current))
                LaunchEditor(Configs.Current.Get(ConfigKey.FilepathMediaEditor), nav.Current);
            else
                LaunchEditor(@"C:\Windows\System32\mspaint.exe", nav.Current);
        }

        private void editInAltEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (FilenameUtils.IsExt(nav.Current, "webp"))
                LaunchEditor(@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe", nav.Current);
            else if (FilenameUtils.LooksLikeEditableAudio(nav.Current))
                LaunchEditor(Configs.Current.Get(ConfigKey.FilepathMediaEditor), nav.Current);
            else
                LaunchEditor(Configs.Current.Get(ConfigKey.FilepathAltEditorImage), nav.Current);
        }

        private void cropRotateFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (FilenameUtils.IsExt(nav.Current, "jpg"))
                LaunchEditor(Configs.Current.Get(ConfigKey.FilepathJpegCrop), nav.Current);
            else if (FilenameUtils.LooksLikeEditableAudio(nav.Current))
                LaunchEditor(Configs.Current.Get(ConfigKey.FilepathMp3DirectCut), nav.Current);
            else
                LaunchEditor(@"C:\Windows\System32\mspaint.exe", nav.Current);
        }

        private void addNumberedPrefixToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int nAddedPrefix = 0, nSkippedPrefix = 0, nFailedToRename = 0;
            int i = 0;
            if (Utils.AskToConfirm("Add numbered prefix?"))
            {
                foreach (var path in nav.GetList())
                {
                    i++;
                    if (Path.GetFileName(path) == FilenameUtils.GetFileNameWithoutNumberedPrefix(path))
                    {
                        if (WrapMoveFile(path, FilenameUtils.AddNumberedPrefix(path, i)))
                            nAddedPrefix++;
                        else
                            nFailedToRename++;
                    }
                    else
                    {
                        nSkippedPrefix++;
                    }
                }
                MoveFirst(false);
            }
            MessageBox.Show(string.Format("{0} files skipped because they already have a prefix, {1} files failed to be renamed, {2} files successfully renamed.", nSkippedPrefix, nFailedToRename, nAddedPrefix));
        }

        private void removeNumberedPrefixToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int nRemovedPrefix = 0, nSkippedAlready = 0, nFailedToRename = 0;
            if (Utils.AskToConfirm("Remove numbered prefix?"))
            {
                foreach (var path in nav.GetList())
                {
                    if (Path.GetFileName(path) == FilenameUtils.GetFileNameWithoutNumberedPrefix(path))
                    {
                        nSkippedAlready++;
                    }
                    else
                    {
                        if (WrapMoveFile(path, Path.GetDirectoryName(path) + "\\" + FilenameUtils.GetFileNameWithoutNumberedPrefix(path)))
                            nRemovedPrefix++;
                        else
                            nFailedToRename++;
                    }
                }
                MoveFirst(false);
            }
            MessageBox.Show(string.Format("{0} files skipped because they have no prefix, {1} files failed to be renamed, {2} files successfully renamed.", nSkippedAlready, nFailedToRename, nRemovedPrefix));
        }

        private void replaceInFilenameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (nav.Current == null)
                return;

            var filename = Path.GetFileName(nav.Current);
            var search = InputBoxForm.GetStrInput("Search for this in filename (not directory name):", filename, InputBoxHistory.RenameReplaceInName);
            if (!string.IsNullOrEmpty(search))
            {
                var replace = InputBoxForm.GetStrInput("Replace with this:", filename, InputBoxHistory.RenameReplaceInName);
                if (replace != null && filename.Contains(search))
                {
                    var newfilename = Path.GetDirectoryName(nav.Current) + "\\" + filename.Replace(search, replace);
                    if (WrapMoveFile(nav.Current, newfilename))
                    {
                        nav.NotifyFileChanges();
                        nav.TrySetPath(newfilename);
                        OnOpenItem();
                    }
                }
            }
        }

        private void FormGallery_FormClosed(object sender, FormClosedEventArgs e)
        {
            RefreshImageCache();
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (_zoomed)
                {
                    OnOpenItem();
                }
                else if (_currentImageResized)
                {
                    imcache.Excerpt.MakeBmp(
                        nav.Current, e.X, e.Y, pictureBox1.Image.Width, pictureBox1.Image.Height);
                    pictureBox1.Image = imcache.Excerpt.Bmp;
                    _zoomed = true;
                }
            }
        }
    }
}
