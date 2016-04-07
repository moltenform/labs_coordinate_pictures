// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public partial class FormGallery : Form
    {
        // see SortingImages.md for a description of modes and categories.
        // the 'mode' specifies filetypes, and can add custom commands
        ModeBase _mode;

        // is this a large image that needed to be resized
        bool _currentImageResized = false;

        // if user control-clicks on a large image, we'll show a zoomed portion of image
        bool _zoomed = false;

        // during long-running operations on bg threads, we block most UI input
        bool _enabled = true;

        // user can add custom categories; we store the original menu in order to restore later
        List<ToolStripItem> _originalCategoriesMenu;
        List<ToolStripItem> _originalEditMenu;

        // shortcut key bindings from letter, to category.
        Dictionary<string, string> _categoryShortcuts;

        // support undoing file moves
        UndoStack<Tuple<string, string>> _undoFileMoves = new UndoStack<Tuple<string, string>>();

        // placeholder image
        Bitmap _bitmapBlank = new Bitmap(1, 1);

        // smart directory-list object that updates itself if filenames are changed
        FileListNavigation _filelist;

        // cache of images; we'll prefetch images into the cache on a bg thread
        ImageCache _imagecache;

        // how many images to store in cache
        const int ImageCacheSize = 15;

        // how many images to prefetch after the user moves to the next image
        const int ImageCacheBatch = 8;

        public FormGallery(ModeBase mode, string initialDirectory, string initialFilepath = "")
        {
            InitializeComponent();

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
            renameItemToolStripMenuItem.Click += (sender, e) => RenameFile();
            undoMoveToolStripMenuItem.Click += (sender, e) => UndoOrRedo(true);
            redoMoveToolStripMenuItem.Click += (sender, e) => UndoOrRedo(false);

            _filelist = new FileListNavigation(initialDirectory, _mode.GetFileTypes(), true, true, initialFilepath);
            ModeUtils.UseDefaultCategoriesIfFirstRun(mode);
            RefreshCategories();
            OnOpenItem();
        }

        public FileListNavigation GetFilelist()
        {
            return _filelist;
        }

        public ImageCache GetImageCache()
        {
            return _imagecache;
        }

        void OnOpenItem()
        {
            // if the user resized the window, create a new cache for the new size
            pictureBox1.Image = _bitmapBlank;
            if (_imagecache == null || _imagecache.MaxWidth != pictureBox1.Width || _imagecache.MaxHeight != pictureBox1.Height)
            {
                RefreshImageCache();
            }

            if (_filelist.Current == null)
            {
                label.Text = "looks done.";
                pictureBox1.Image = _bitmapBlank;
            }
            else
            {
                // tell the mode we've opened something
                _mode.OnOpenItem(_filelist.Current, this);

                // show the current image
                int originalWidth = 0, originalHeight = 0;
                pictureBox1.Image = _imagecache.Get(_filelist.Current, out originalWidth, out originalHeight);
                _currentImageResized = originalWidth > _imagecache.MaxWidth || originalHeight > _imagecache.MaxHeight;
                var showResized = _currentImageResized ? "s" : "";
                label.Text = string.Format("{0} {1}\r\n{2} {3}({4}x{5})", _filelist.Current,
                    Utils.FormatFilesize(_filelist.Current), Path.GetFileName(_filelist.Current),
                    showResized, originalWidth, originalHeight);
            }

            renameToolStripMenuItem.Visible = _mode.SupportsRename();
            _zoomed = false;
        }

        void RefreshImageCache()
        {
            if (_imagecache != null)
            {
                pictureBox1.Image = null;
                _imagecache.Dispose();
            }

            // provide callbacks for ImageCache to see if it can dispose an image.
            Func<Bitmap, bool> canDisposeBitmap =
                (bmp) => (bmp as object) != (pictureBox1.Image as object);
            Func<Action, bool> callbackOnUiThread =
                (act) =>
                {
                    this.Invoke((MethodInvoker)(() => act.Invoke()));
                    return true;
                };
            _imagecache = new ImageCache(pictureBox1.Width, pictureBox1.Height,
                ImageCacheSize, callbackOnUiThread, canDisposeBitmap);
        }

        void RefreshFilelist()
        {
            _filelist.Refresh();
            MoveFirst(false);
        }

        void MoveOne(bool forwardDirection)
        {
            // make a list of length ImageCacheBatch
            var pathsToCache = new List<string>();
            for (int i = 0; i < ImageCacheBatch; i++)
            {
                pathsToCache.Add(null);
            }

            _filelist.GoNextOrPrev(forwardDirection, pathsToCache, pathsToCache.Count);
            OnOpenItem();
            _imagecache.AddAsync(pathsToCache);
        }

        void MoveMany(bool forwardDirection)
        {
            for (int i = 0; i < 15; i++)
            {
                _filelist.GoNextOrPrev(forwardDirection);
            }

            OnOpenItem();
        }

        void MoveFirst(bool forwardDirection)
        {
            if (forwardDirection)
            {
                _filelist.GoLast();
            }
            else
            {
                _filelist.GoFirst();
            }

            OnOpenItem();
        }

        void RefreshCustomCommands()
        {
            labelView.Text = "\r\n\r\n";
            if (_mode.GetDisplayCustomCommands().Length > 0)
            {
                // restore original Edit menu
                editToolStripMenuItem.DropDownItems.Clear();
                foreach (var item in _originalEditMenu)
                {
                    editToolStripMenuItem.DropDownItems.Add(item);
                }

                // add items to the Edit menu and to labelView.Text.
                editToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
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

            // restore original Categories menu
            categoriesToolStripMenuItem.DropDownItems.Clear();
            foreach (var item in _originalCategoriesMenu)
            {
                categoriesToolStripMenuItem.DropDownItems.Add(item);
            }

            // add items to the Categories menu and to labelView.Text.
            categoriesToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            _categoryShortcuts = new Dictionary<string, string>();
            var tuples = ModeUtils.ModeToTuples(_mode);
            foreach (var tuple in tuples)
            {
                var menuItem = new ToolStripMenuItem(tuple.Item2);
                menuItem.ShortcutKeyDisplayString = tuple.Item1;
                menuItem.Click += (sender, e) => AssignCategory(tuple.Item3);
                categoriesToolStripMenuItem.DropDownItems.Add(menuItem);
                labelView.Text += tuple.Item1 + "    " + tuple.Item2 + "\r\n\r\n";
                this._categoryShortcuts[tuple.Item1] = tuple.Item3;
            }

            // only show categories in the UI if enabled.
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
            // in the input box, suggest category strings.
            var suggestions = new string[]
            {
                Configs.Current.Get(_mode.GetCategories()),
                _mode.GetDefaultCategories()
            };

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
                _undoFileMoves.Add(Tuple.Create(src, target));
            }

            return true;
        }

        public void UndoOrRedo(bool isUndo)
        {
            var moveConsidered = isUndo ?
                _undoFileMoves.PeekUndo() :
                _undoFileMoves.PeekRedo();

            if (moveConsidered == null)
            {
                if (!Configs.Current.SupressDialogs)
                    MessageBox.Show("nothing to undo");
            }
            else
            {
                var newdest = isUndo ? moveConsidered.Item1 : moveConsidered.Item2;
                var newsrc = isUndo ? moveConsidered.Item2 : moveConsidered.Item1;
                if (Configs.Current.SupressDialogs ||
                    Utils.AskToConfirm("move " + newsrc + " back to " + newdest + "?"))
                {
                    if (WrapMoveFile(newsrc, newdest, fAddToUndoStack: false))
                    {
                        if (isUndo)
                        {
                            _undoFileMoves.Undo();
                        }
                        else
                        {
                            _undoFileMoves.Redo();
                        }
                    }
                }
            }
        }

        public void FormGallery_KeyUp(object sender, KeyEventArgs e)
        {
            if (!_enabled)
            {
                return;
            }

            // Use custom shortcut strings instead of ShortcutKeys
            // 1) allows shortcuts without Ctrl or Alt
            // 2) ShortcutKeys uses KeyDown which fires many times if you hold the key
            if (!e.Shift && !e.Control && !e.Alt)
            {
                if (e.KeyCode == Keys.F5)
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
                    RenameFile();
            }
            else if (e.Shift && !e.Control && !e.Alt)
            {
                // see if this is a category shortcut
                var keystring = e.KeyCode.ToString();
                if (keystring.Length == 1 || (keystring.Length == 2 && keystring[0] == 'D'))
                {
                    keystring = keystring.Substring(keystring.Length - 1);
                    if (_categoryShortcuts.ContainsKey(keystring))
                        AssignCategory(_categoryShortcuts[keystring]);
                }
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
                else if (e.KeyCode == Keys.OemOpenBrackets)
                    convertToSeveralJpgsInDifferentQualitiesToolStripMenuItem_Click(null, null);
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
                else if (e.KeyCode == Keys.Z)
                    UndoOrRedo(true);
                else if (e.KeyCode == Keys.Y)
                    UndoOrRedo(false);
                else if (e.KeyCode == Keys.OemOpenBrackets)
                    convertResizeImageToolStripMenuItem_Click(null, null);
                else if (e.KeyCode == Keys.D1)
                    convertAllPngToWebpToolStripMenuItem_Click(null, null);
                else if (e.KeyCode == Keys.OemCloseBrackets)
                    keepAndDeleteOthersToolStripMenuItem_Click(null, null);
                else if (e.KeyCode == Keys.Enter)
                    finishedCategorizingToolStripMenuItem_Click(null, null);
            }

            _mode.OnCustomCommand(this, e.Shift, e.Alt, e.Control, e.KeyCode);
        }

        // add a file to a category (appends to the filename and it won't show up in formgallery anymore)
        void AssignCategory(string categoryId)
        {
            _mode.OnBeforeAssignCategory();
            if (_filelist.Current == null)
            {
                return;
            }

            var newname = FilenameUtils.AddMarkToFilename(_filelist.Current, categoryId);
            if (WrapMoveFile(_filelist.Current, newname))
            {
                MoveOne(true);
            }
        }

        public void RenameFile()
        {
            if (_filelist.Current == null || !_mode.SupportsRename())
            {
                return;
            }

            InputBoxHistory key = FilenameUtils.LooksLikeImage(_filelist.Current) ?
                InputBoxHistory.RenameImage : (FilenameUtils.IsExt(_filelist.Current, ".wav") ?
                InputBoxHistory.RenameWavAudio : InputBoxHistory.RenameOther);

            // for convenience, don't include the numbered prefix or file extension.
            var current = FilenameUtils.GetFileNameWithoutNumberedPrefix(_filelist.Current);
            var currentNoext = Path.GetFileNameWithoutExtension(current);

            var newname = InputBoxForm.GetStrInput("Enter a new name:", currentNoext, key);
            if (!string.IsNullOrEmpty(newname))
            {
                var fullnewname = Path.GetDirectoryName(_filelist.Current) + "\\" +
                    (current != Path.GetFileName(_filelist.Current) ? Path.GetFileName(_filelist.Current).Substring(0, 8) : "") +
                    newname + Path.GetExtension(_filelist.Current);

                if (WrapMoveFile(_filelist.Current, fullnewname))
                {
                    _filelist.NotifyFileChanges();
                    _filelist.TrySetPath(fullnewname);
                    OnOpenItem();
                }
            }
        }

        void KeyDelete()
        {
            if (_filelist.Current != null)
            {
                _mode.OnBeforeAssignCategory();
                if (UndoableSoftDelete(_filelist.Current))
                {
                    MoveOne(true);
                }
            }
        }

        bool UndoableSoftDelete(string path)
        {
            var dest = Utils.GetSoftDeleteDestination(path);
            return WrapMoveFile(path, dest);
        }

        // during long-running operations on bg threads, we block most UI input
        public void UIEnable()
        {
            this.label.ForeColor = Color.Black;
            fileToolStripMenuItem.Enabled = editToolStripMenuItem.Enabled =
                renameToolStripMenuItem.Enabled = categoriesToolStripMenuItem.Enabled = true;
            _enabled = true;
        }

        public void UIDisable()
        {
            this.label.ForeColor = Color.Gray;
            fileToolStripMenuItem.Enabled = editToolStripMenuItem.Enabled =
                renameToolStripMenuItem.Enabled = categoriesToolStripMenuItem.Enabled = false;
            _enabled = false;
        }

        private void showInExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_filelist.Current == null)
            {
                Utils.OpenDirInExplorer(_filelist.BaseDirectory);
            }
            else
            {
                Utils.SelectFileInExplorer(_filelist.Current);
            }
        }

        private void copyPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(_filelist.Current ?? "");
        }

        static void LaunchEditor(string exe, string path)
        {
            if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
            {
                MessageBox.Show("Could not find the application '" + exe + "'. The location can be set in the Options menu.");
            }
            else
            {
                Utils.Run(exe, new string[] { path }, shell: false, waitForExit: false, hideWindow: false);
            }
        }

        private void editFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (FilenameUtils.IsExt(_filelist.Current, ".webp"))
            {
                Process.Start(_filelist.Current);
            }
            else if (FilenameUtils.LooksLikeEditableAudio(_filelist.Current))
            {
                LaunchEditor(Configs.Current.Get(ConfigKey.FilepathMediaEditor), _filelist.Current);
            }
            else
            {
                LaunchEditor(@"C:\Windows\System32\mspaint.exe", _filelist.Current);
            }
        }

        private void editInAltEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (FilenameUtils.IsExt(_filelist.Current, ".webp"))
            {
                LaunchEditor(@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe", _filelist.Current);
            }
            else if (FilenameUtils.LooksLikeEditableAudio(_filelist.Current))
            {
                LaunchEditor(Configs.Current.Get(ConfigKey.FilepathMediaEditor), _filelist.Current);
            }
            else
            {
                LaunchEditor(Configs.Current.Get(ConfigKey.FilepathAltEditorImage), _filelist.Current);
            }
        }

        private void cropRotateFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (FilenameUtils.IsExt(_filelist.Current, ".jpg"))
            {
                LaunchEditor(Configs.Current.Get(ConfigKey.FilepathJpegCrop), _filelist.Current);
            }
            else if (FilenameUtils.LooksLikeEditableAudio(_filelist.Current))
            {
                LaunchEditor(Configs.Current.Get(ConfigKey.FilepathMp3DirectCut), _filelist.Current);
            }
            else
            {
                LaunchEditor(@"C:\Windows\System32\mspaint.exe", _filelist.Current);
            }
        }

        // add a prefix to files, useful when renaming and you want to maintain the order
        private void addNumberedPrefixToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int nAddedPrefix = 0, nSkippedPrefix = 0, nFailedToRename = 0;
            if (_mode.SupportsRename() && Utils.AskToConfirm("Add numbered prefix?"))
            {
                int i = 0;
                foreach (var path in _filelist.GetList())
                {
                    i++;
                    if (Path.GetFileName(path) == FilenameUtils.GetFileNameWithoutNumberedPrefix(path))
                    {
                        if (WrapMoveFile(path, FilenameUtils.AddNumberedPrefix(path, i)))
                        {
                            nAddedPrefix++;
                        }
                        else
                        {
                            nFailedToRename++;
                        }
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
            if (_mode.SupportsRename() && Utils.AskToConfirm("Remove numbered prefix?"))
            {
                foreach (var path in _filelist.GetList())
                {
                    if (Path.GetFileName(path) == FilenameUtils.GetFileNameWithoutNumberedPrefix(path))
                    {
                        nSkippedAlready++;
                    }
                    else
                    {
                        if (WrapMoveFile(path, Path.GetDirectoryName(path) + "\\" + FilenameUtils.GetFileNameWithoutNumberedPrefix(path)))
                        {
                            nRemovedPrefix++;
                        }
                        else
                        {
                            nFailedToRename++;
                        }
                    }
                }

                MoveFirst(false);
            }

            MessageBox.Show(string.Format("{0} files skipped because they have no prefix, {1} files failed to be renamed, {2} files successfully renamed.", nSkippedAlready, nFailedToRename, nRemovedPrefix));
        }

        // replace one string with another within a filename.
        private void replaceInFilenameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_filelist.Current == null || !_mode.SupportsRename())
            {
                return;
            }

            var filename = Path.GetFileName(_filelist.Current);
            var search = InputBoxForm.GetStrInput("Search for this in filename (not directory name):", filename, InputBoxHistory.RenameReplaceInName);
            if (!string.IsNullOrEmpty(search))
            {
                var replace = InputBoxForm.GetStrInput("Replace with this:", filename, InputBoxHistory.RenameReplaceInName);
                if (replace != null && filename.Contains(search))
                {
                    var newfilename = Path.GetDirectoryName(_filelist.Current) + "\\" + filename.Replace(search, replace);
                    if (WrapMoveFile(_filelist.Current, newfilename))
                    {
                        _filelist.NotifyFileChanges();
                        _filelist.TrySetPath(newfilename);
                        OnOpenItem();
                    }
                }
            }
        }

        // ctrl-clicking a large image will show a zoomed-in view.
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
                    _imagecache.Excerpt.MakeBmp(
                        _filelist.Current, e.X, e.Y, pictureBox1.Image.Width, pictureBox1.Image.Height);
                    pictureBox1.Image = _imagecache.Excerpt.Bmp;
                    _zoomed = true;
                }
            }
        }

        // calls a Python script to convert or resize an image.
        private void convertResizeImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_filelist.Current == null || !FilenameUtils.LooksLikeImage(_filelist.Current))
            {
                return;
            }

            var more = new string[] { "50%", "100%", "70%" };
            var resize = InputBoxForm.GetStrInput("Resize by what value (example 50%):", null, more: more, useClipboard: false);
            if (string.IsNullOrEmpty(resize))
            {
                return;
            }

            if ((!resize.EndsWith("h") && !resize.EndsWith("%")) ||
                !Utils.IsDigits(resize.Substring(0, resize.Length - 1)))
            {
                MessageBox.Show("invalid resize spec.");
                return;
            }

            more = new string[] { "png|100", "jpg|90", "webp|100" };
            var fmt = InputBoxForm.GetStrInput("Convert to format|quality:", null, InputBoxHistory.EditConvertResizeImage, more, useClipboard: false);
            if (string.IsNullOrEmpty(fmt))
            {
                return;
            }

            var parts = fmt.Split(new char[] { '|' });
            int nQual = 0;
            if (!(parts.Length == 2 && int.TryParse(parts[1], out nQual) && nQual > 0 && nQual <= 100))
            {
                MessageBox.Show("Invalid format string or bad quality");
                return;
            }
            else if (Array.IndexOf(new string[] { "jpg", "png", "gif", "bmp", "webp" }, parts[0]) == -1)
            {
                MessageBox.Show("Unsupported image format");
                return;
            }

            var outFile = Path.GetDirectoryName(_filelist.Current) + "\\" + Path.GetFileNameWithoutExtension(_filelist.Current) + "_out." + parts[0];
            Utils.RunImageConversion(_filelist.Current, outFile, resize, nQual);
        }

        private void convertAllPngToWebpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // webp converter can be slow for large images, so ask the user first.
            var list = _filelist.GetList().Where((item) => item.EndsWith(".png"));
            var newlist = new List<string>();
            foreach (var path in list)
            {
                if (new FileInfo(path).Length < 1024 * 500 ||
                    Utils.AskToConfirm("include the large file " +
                        Path.GetFileName(path) + "\r\n" + Utils.FormatFilesize(path) + "?"))
                {
                    newlist.Add(path);
                }
            }

            RunLongActionInThread(new Action(() =>
            {
                int countConverted = 0, countLeft = 0;
                foreach (var path in newlist)
                {
                    var newname = Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path) + ".webp";
                    if (!File.Exists(newname))
                    {
                        // convert png to webp, then delete the larger of the resulting pair of images.
                        Utils.RunImageConversion(path, newname, "100%", 100);
                        if (new FileInfo(newname).Length < new FileInfo(path).Length)
                        {
                            countConverted++;
                            Utils.SoftDelete(path);
                        }
                        else
                        {
                            countLeft++;
                            File.Delete(newname);
                        }
                    }
                }

                MessageBox.Show("Complete. " +
                    countConverted + "file(s) to webp, " + countLeft + " file(s) were smaller as png.");
            }));
        }

        // makes several images at different jpg qualities.
        private void convertToSeveralJpgsInDifferentQualitiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_filelist.Current == null || !FilenameUtils.LooksLikeImage(_filelist.Current))
            {
                return;
            }

            RunLongActionInThread(new Action(() =>
            {
                var qualities = new int[] { 96, 94, 92, 90, 85, 80, 75, 70, 60 };
                foreach (var qual in qualities)
                {
                    var outFile = _filelist.Current + qual.ToString() + ".jpg";
                    Utils.RunImageConversion(_filelist.Current, outFile, "100%", qual);
                }
            }));
        }

        // after making several images at different jpg qualities, keeps the current image and removes the rest.
        private void keepAndDeleteOthersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_filelist.Current == null || !FilenameUtils.LooksLikeImage(_filelist.Current))
            {
                return;
            }

            bool fHasMiddleName;
            string sNewname;
            var toDelete = FilenameFindSimilarFilenames.FindSimilarNames(_filelist.Current, _mode.GetFileTypes(), _filelist.GetList(),
                out fHasMiddleName, out sNewname);

            if (Utils.AskToConfirm("Delete the extra files \r\n" + string.Join("\r\n", toDelete) + "\r\n?"))
            {
                foreach (var sFile in toDelete)
                {
                    UndoableSoftDelete(sFile);
                }

                // rename this file to be better
                if (fHasMiddleName && WrapMoveFile(_filelist.Current, sNewname))
                {
                    _filelist.NotifyFileChanges();
                    _filelist.TrySetPath(sNewname);
                    OnOpenItem();
                }
            }
        }

        // each mode can specify a 'completion action' that is called for each file that was given a category.
        private void finishedCategorizingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!_mode.SupportsCompletionAction())
            {
                MessageBox.Show("Mode does not have an associated action.");
                return;
            }

            if (Utils.AskToConfirm("Apply finishing?"))
            {
                RunLongActionInThread(new Action(() =>
                {
                    CallCompletionAction();
                    OnOpenItem();
                }));
            }
        }

        public void CallCompletionAction()
        {
            var tuples = ModeUtils.ModeToTuples(_mode);
            foreach (var path in _filelist.GetList(includeMarked: true))
            {
                if (path.Contains(FilenameUtils.MarkerString))
                {
                    string pathWithoutCategory, category;
                    FilenameUtils.GetMarkFromFilename(path, out pathWithoutCategory, out category);
                    var tupleFound = tuples.Where((item) => item.Item3 == category).ToArray();
                    if (tupleFound.Length == 0)
                    {
                        MessageBox.Show("Invalid mark for file " + path);
                    }
                    else
                    {
                        _mode.OnCompletionAction(_filelist.BaseDirectory, path, pathWithoutCategory, tupleFound[0]);
                    }
                }
            }
        }

        public void RunLongActionInThread(Action fn)
        {
            UIDisable();
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    fn.Invoke();
                }
                finally
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        UIEnable();
                        OnOpenItem();
                    }));
                }
            });
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }

                if (_filelist != null)
                {
                    _filelist.Dispose();
                }

                if (_imagecache != null)
                {
                    _imagecache.Dispose();
                }

                if (_bitmapBlank != null)
                {
                    _bitmapBlank.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
