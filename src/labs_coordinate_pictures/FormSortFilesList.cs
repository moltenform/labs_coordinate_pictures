using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public partial class FormSortFilesList : Form
    {
        SortFilesAction _action;
        SortFilesSettings _settings;
        FileComparisonResult[] _results;
        UndoStack<List<IUndoableFileOp>> _undoFileMoves;

        bool _synchronous; // for testing, run all bg tasks synchronously
        List<FileComparisonResult> _testSelectedItems;
        object _lock = new object();
        int _sortCol = int.MaxValue;
        string _caption;

        public FormSortFilesList(SortFilesAction action, SortFilesSettings settings,
            string caption, bool allActionsSynchronous)
        {
            InitializeComponent();
            _action = action;
            _settings = settings;
            _caption = caption;
            _synchronous = allActionsSynchronous;
            _undoFileMoves = new UndoStack<List<IUndoableFileOp>>();
            _testSelectedItems = new List<FileComparisonResult>();

            listView.SmallImageList = imageList;
            lblAction.Text = "Searching...";
            btnCopyFileLeft.Click += (o, e) => OnClickCopyFile(true, true);
            btnCopyFileRight.Click += (o, e) => OnClickCopyFile(false, true);
            btnDeleteLeft.Click += (o, e) => OnClickDeleteFile(true, true);
            btnDeleteRight.Click += (o, e) => OnClickDeleteFile(false, true);
            btnShowLeft.Click += (o, e) => OnClickShowFile(true);
            btnShowRight.Click += (o, e) => OnClickShowFile(false);
            undoFileMoveToolStripMenuItem.Click += (o, e) => OnUndoClick(true);

            if (action != SortFilesAction.SearchDifferences)
            {
                searchMovedFilesToolStripMenuItem.Visible = false;
                propagateMovesToolStripMenuItem.Visible = false;
            }
        }

        private void FormSortFilesList_Load(object sender, EventArgs e)
        {
            if (listView.Items != null && listView.Items.Count > 0)
            {
                return;
            }

            refreshToolStripMenuItem_Click(null, null);
        }

        internal void RunSortFilesAction()
        {
            _results = new FileComparisonResult[] { };
            switch (_action)
            {
                case SortFilesAction.SearchDifferences:
                    _results = SortFilesSearchDifferences.Go(_settings).ToArray();
                    break;
                case SortFilesAction.SearchDuplicates:
                    _results = SortFilesSearchDuplicates.Go(_settings).ToArray();
                    break;
                case SortFilesAction.SearchDuplicatesInOneDir:
                    _results = SortFilesSearchDuplicatesInOneDir.Go(_settings).ToArray();
                    break;
                default:
                    Utils.MessageErr("Unrecognized action.");
                    break;
            }

            // update UI on main thread
            WrapInvoke(() =>
            {
                listView_ColumnClick(null, new ColumnClickEventArgs(0));
                listView.Columns[1].Width = -2; // autosize to the longest item in the column
                lblAction.Text = _caption + Utils.NL;
                lblAction.Text += "" + _results.Length + " file(s) listed:";
            });
        }

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SelectedItems().Count() == 0)
            {
                return;
            }

            // limit filepaths shown; apparently txtbox.Text = "long string" can be slow
            const int nMaxFilepathsToShow = 20;
            StringBuilder sbLeftFiles = new StringBuilder();
            StringBuilder sbLeftStats = new StringBuilder();
            StringBuilder sbRightFiles = new StringBuilder();
            StringBuilder sbRightStats = new StringBuilder();
            foreach (var item in SelectedItems().Take(nMaxFilepathsToShow))
            {
                // only collect info we have cached, avoid hitting the disk, for responsiveness
                if (item.FileInfoLeft != null)
                {
                    sbLeftFiles.AppendLine(item.GetLeft(_settings.LeftDirectory));
                    sbLeftStats.AppendLine(Utils.FormatFilesize(item.FileInfoLeft.FileSize));
                }

                if (item.FileInfoRight != null)
                {
                    sbRightFiles.AppendLine(item.GetRight(_settings.RightDirectory));
                    sbRightStats.AppendLine(Utils.FormatFilesize(item.FileInfoRight.FileSize));
                }
            }

            if (SelectedItems().Count() > nMaxFilepathsToShow)
            {
                var omitted = "...Files ommitted, use Edit->Copy Filepaths to see the rest...";
                sbLeftStats.AppendLine(omitted);
                sbRightStats.AppendLine(omitted);
            }

            foreach (var ctrl in new Control[] { lblOnLeft, tbLeft,
                btnCopyFileLeft, btnDeleteLeft, btnShowLeft })
            {
                ctrl.Visible = (sbLeftFiles.Length > 0) ? true : false;
            }

            foreach (var ctrl in new Control[] { lblOnRight, tbRight,
                btnCopyFileRight, btnDeleteRight, btnShowRight })
            {
                ctrl.Visible = (sbRightFiles.Length > 0) ? true : false;
            }

            tbLeft.Text = sbLeftFiles.ToString() + Utils.NL + sbLeftStats.ToString();
            tbRight.Text = sbRightFiles.ToString() + Utils.NL + sbRightStats.ToString();
            if (_action != SortFilesAction.SearchDifferences)
            {
                btnCopyFileLeft.Visible = false;
                btnCopyFileRight.Visible = false;
            }
        }

        static string GetFileDetails(string filename)
        {
            try
            {
                var info = new FileInfo(filename);
                return filename + Utils.NL +
                    Utils.FormatFilesize(info.Length) + Utils.NL +
                    "Created: " + info.CreationTimeUtc + Utils.NL +
                    "Written: " + info.LastWriteTimeUtc + Utils.NL +
                    "Sha512: " + Utils.GetSha512(filename);
            }
            catch (Exception)
            {
                return filename + " not accessible.";
            }
        }

        internal void listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            IEnumerable<FileComparisonResult> displayResults = null;
            if (e.Column == 0 && e.Column == _sortCol)
            {
                // the second time it's clicked, sort by contents of first column
                displayResults = from item in _results
                                 orderby item.SubItems[0].Text
                                 select item;
            }
            else if (e.Column == 0 && e.Column != _sortCol)
            {
                // the first time it's clicked, sort by original order
                displayResults = from item in _results select item;
            }
            else if (e.Column == 1)
            {
                // sort by type
                displayResults = from item in _results orderby item.Type select item;
            }
            else if (e.Column == 2)
            {
                // sort by path
                displayResults = from item in _results
                                 orderby item.SubItems[2].Text
                                 select item;
            }
            else
            {
                // unknown sort column
                return;
            }

            if (e.Column == _sortCol)
            {
                // if the same column is clicked again after this, go back to default order.
                _sortCol = int.MaxValue;
                displayResults = displayResults.Reverse();
            }
            else
            {
                _sortCol = e.Column;
            }

            listView.Items.Clear();
            listView.Items.AddRange(displayResults.ToArray());
        }

        internal bool CheckSelectedItemsSameType()
        {
            var count = (from item in SelectedItems()
                         select item.Type).Distinct().Count();
            if (count == 0)
            {
                Utils.MessageBox("Nothing selected.", true);
                return false;
            }
            else if (count == 1)
            {
                return true;
            }
            else
            {
                Utils.MessageBox("Please ensure that all selected items are the same type, " +
                    "for example all Left Only or all Changed.", true);
                return false;
            }
        }

        internal void OnClickCopyFile(bool left, bool needConfirm)
        {
            if (CheckSelectedItemsSameType() || _synchronous)
            {
                if (needConfirm &&
                    !Utils.AskToConfirm("Move " + SelectedItems().Count() + " files?"))
                {
                    return;
                }

                var moves = new List<IUndoableFileOp>();
                foreach (var item in SelectedItems())
                {
                    var source = left ? item.GetLeft(_settings.LeftDirectory) :
                        item.GetRight(_settings.RightDirectory);
                    var dest = left ? item.GetRight(_settings.RightDirectory) :
                        item.GetLeft(_settings.LeftDirectory);
                    var destRoot = left ? _settings.RightDirectory :
                        _settings.LeftDirectory;

                    if (source != null)
                    {
                        item.SetMarkedAsModifiedInUI(true);
                        if (dest != null && File.Exists(dest))
                        {
                            moves.Add(new FileOpFileMove(
                                dest, Utils.GetSoftDeleteDestination(dest)));

                            moves.Add(new FileOpFileCopy(
                                source, dest));
                        }
                        else
                        {
                            // we've been told to Copy-Left-To-Right a Left-Only file,
                            // the destination isn't set yet, so make a new path.
                            var destWouldBe = destRoot + (left ? item.FileInfoLeft.Filename :
                                item.FileInfoRight.Filename);

                            moves.Add(new FileOpFileCopy(source, destWouldBe));
                        }
                    }
                }

                moveFiles(moves, left ? tbLeft : tbRight);
            }
        }

        // both left and right should not overlap with files already deleted
        // for the SearchDuplicatesInOneDir case, we can then show a warning
        // if the user is trying to delete both the left and right file
        bool checkOverlappingDeletes(bool left, bool needConfirm = true)
        {
            var filesWillBeDeleted = new HashSet<string>();
            foreach (var item in SelectedItems())
            {
                var path = left ? item.GetLeft(_settings.LeftDirectory) :
                    item.GetRight(_settings.RightDirectory);
                var original = left ? item.GetRight(_settings.LeftDirectory) :
                    item.GetLeft(_settings.RightDirectory);

                if (path != null)
                {
                    if (filesWillBeDeleted.Contains(original) ||
                        filesWillBeDeleted.Contains(path))
                    {
                        if (needConfirm)
                        {
                            MessageBox.Show("Warning, don't want to delete both " +
                                path + " and " + original +
                                "because it might delete all copies of file." +
                                "Try Delete Right instead of Delete Left?");
                        }

                        return false;
                    }

                    filesWillBeDeleted.Add(path);
                }
            }

            return true;
        }

        internal void OnClickDeleteFile(bool left, bool needConfirm)
        {
            if (CheckSelectedItemsSameType() || _synchronous)
            {
                if (needConfirm &&
                    !Utils.AskToConfirm("Delete " + SelectedItems().Count() + " files?"))
                {
                    return;
                }

                if (_action == SortFilesAction.SearchDuplicatesInOneDir &&
                    !checkOverlappingDeletes(left, needConfirm))
                {
                    return;
                }

                var moves = new List<IUndoableFileOp>();
                foreach (var item in SelectedItems())
                {
                    var path = left ? item.GetLeft(_settings.LeftDirectory) :
                        item.GetRight(_settings.RightDirectory);

                    if (path != null)
                    {
                        item.SetMarkedAsModifiedInUI(true);
                        moves.Add(new FileOpFileMove(
                            path, Utils.GetSoftDeleteDestination(path)));
                    }
                }

                moveFiles(moves, left ? tbLeft : tbRight);
            }
        }

        void OnClickShowFile(bool left)
        {
            var item = SelectedItems().FirstOrDefault();
            if (item != null)
            {
                var path = left ? item.GetLeft(_settings.LeftDirectory) :
                            item.GetRight(_settings.RightDirectory);
                if (path != null)
                {
                    Utils.SelectFileInExplorer(path);
                }
            }
        }

        internal void GetTestHooks(out ListView outListView,
            out List<FileComparisonResult> outSelectedItems,
            out UndoStack<List<IUndoableFileOp>> outUndoStack)
        {
            outListView = listView;
            outSelectedItems = _testSelectedItems;
            outUndoStack = _undoFileMoves;
        }

        void moveFiles(List<IUndoableFileOp> fileMoves, TextBox tbLog)
        {
            StartBgAction(() =>
            {
                // clear UI
                WrapInvoke(() =>
                {
                    tbLeft.Text = "";
                    tbRight.Text = "";
                });

                var addToUndoStack = new List<IUndoableFileOp>();
                try
                {
                    while (fileMoves.Count > 0)
                    {
                        // move file, exceptions are ok
                        fileMoves[0].Do();

                        // print to UI
                        WrapInvoke(() =>
                        {
                            tbLog.AppendText(Utils.NL + Utils.NL + fileMoves[0]);
                        });

                        addToUndoStack.Add(fileMoves[0]);
                        fileMoves.RemoveAt(0);
                    }
                }
                finally
                {
                    // keep ability to undo even if operation was stopped by an exception
                    if (addToUndoStack.Count > 0)
                    {
                        _undoFileMoves.Add(addToUndoStack);
                    }
                }

                // print to UI
                WrapInvoke(() =>
                {
                    tbLog.AppendText(Utils.NL + Utils.NL + "Complete.");
                });
            });
        }

        internal void OnUndoClick(bool needConfirm)
        {
            StartBgAction(() =>
            {
                // clear UI
                WrapInvoke(() =>
                {
                    tbLeft.Text = "";
                    tbRight.Text = "";
                });

                var fileMoves = _undoFileMoves.PeekUndo();
                if (fileMoves == null)
                {
                    Utils.MessageBox("Nothing to undo.");
                }
                else
                {
                    // ask user to confirm
                    var preview = string.Join(Utils.NL,
                        from move in fileMoves select move.ToString());

                    if (!needConfirm ||
                        Utils.AskToConfirm("Undo these moves?" + Utils.NL + preview))
                    {
                        // if an exception occurs partially through, our progress will be saved
                        while (fileMoves.Count > 0)
                        {
                            // undo move, exceptions are ok
                            var move = fileMoves[fileMoves.Count - 1];
                            move.Undo();

                            // print to UI
                            WrapInvoke(() =>
                            {
                                tbLeft.AppendText(Utils.NL + Utils.NL + "Undo " + move);
                            });

                            // remove from list
                            fileMoves.RemoveAt(fileMoves.Count - 1);
                        }

                        // print to UI
                        WrapInvoke(() =>
                        {
                            tbLeft.AppendText(Utils.NL + Utils.NL + "Complete.");
                        });

                        // remove from undo stack
                        _undoFileMoves.Undo();
                    }
                }
            });
        }

        void StartBgAction(Action fn)
        {
            // for testability, when _synchronous is true run everything on one thread.
            if (_synchronous)
            {
                fn();
            }
            else
            {
                Utils.RunLongActionInThread(_lock, this, fn);
            }
        }

        void WrapInvoke(Action fn)
        {
            // for testability, when _synchronous is true run everything on one thread.
            if (_synchronous)
            {
                fn();
            }
            else
            {
                this.Invoke(fn);
            }
        }

        private void listView_DoubleClick(object sender, EventArgs e)
        {
            foreach (var item in SelectedItems())
            {
                item.SetMarkedAsModifiedInUI(!item.GetMarkedAsModifiedInUI());
            }

            listView.Refresh();
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartBgAction(RunSortFilesAction);
        }

        private void showFileDetailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // allowed to hit disk, so it pulls recent info + computes hash
            var first = SelectedItems().FirstOrDefault();
            if (first != null)
            {
                tbLeft.Text = first.FileInfoLeft == null ? "" :
                    GetFileDetails(first.GetLeft(_settings.LeftDirectory));

                tbRight.Text = first.FileInfoRight == null ? "" :
                    GetFileDetails(first.GetRight(_settings.RightDirectory));
            }
        }

        private void compareFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var item = SelectedItems().FirstOrDefault();
            if (item != null)
            {
                // if 1 item selected, compare left file and right file
                string compLeft = item.GetLeft(_settings.LeftDirectory) ?? "";
                string compRight = item.GetRight(_settings.RightDirectory) ?? "";

                // if > 1 item selected, compare left parent dir and right parent dir
                if (SelectedItems().Count() > 1)
                {
                    compLeft = string.IsNullOrEmpty(compLeft) ? "" :
                        Path.GetDirectoryName(compLeft);
                    compRight = string.IsNullOrEmpty(compRight) ? "" :
                        Path.GetDirectoryName(compRight);
                }

                var mergeExe = Configs.Current.Get(ConfigKey.FilepathWinMerge);
                if (string.IsNullOrEmpty(mergeExe) || !File.Exists(mergeExe))
                {
                    Utils.MessageBox("Location for winmerge not set, go to the main window and " +
                        "select the menuitem Options->Set Winmerge location...");
                }
                else
                {
                    var args = new string[] { compLeft, compRight };
                    Utils.Run(mergeExe, args,
                        shellExecute: false, waitForExit: false, hideWindow: false);
                }
            }
        }

        private void copyAllFilepathsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var query = from item in SelectedItems()
                            select item.SubItems[1].Text + "\t" + item.SubItems[2].Text;

            if (query.FirstOrDefault() != null)
            {
                Clipboard.SetText(string.Join(Utils.NL, query));
            }
        }

        private void copyLeftFilepathsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var query = from item in SelectedItems()
                        where item.FileInfoLeft != null
                        select item.GetLeft(_settings.LeftDirectory);

            if (query.FirstOrDefault() != null)
            {
                Clipboard.SetText(string.Join(Utils.NL, query));
            }
        }

        private void copyRightFilepathsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var query = from item in SelectedItems()
                        where item.FileInfoRight != null
                        select item.GetRight(_settings.RightDirectory);

            if (query.FirstOrDefault() != null)
            {
                Clipboard.SetText(string.Join(Utils.NL, query));
            }
        }

        private void searchMovedFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.MessageBox("We'll go through every deleted file (exists on the Left " +
                "but not the Right) and see if it is just the result of a " +
                "moved or renamed file (a file with same contents already exists on Right).");

            var query = (from item in listView.Items.Cast<FileComparisonResult>()
                        where item.Type == FileComparisonResultType.Left_Only
                        select item).ToList();

            StartBgAction(() => {
                // retrieve a list of the items that are actually moved files
                var results = SortFilesSearchDuplicates.SearchMovedFiles(
                    _settings.LeftDirectory, _settings.RightDirectory, query);

                WrapInvoke(() => {
                    // show the result in the UI, in the left-most column
                    Utils.MessageBox("Found " + results.Count + " moved file(s).");
                    foreach (var item in results)
                    {
                        item.Item1.SubItems[0].Text = item.Item2;
                    }

                    listView.Refresh();
                });
            });
        }

        private void searchForIdenticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.MessageBox("We'll go through every supposedly-changed file and see if it is " +
                "truly a changed file, or if only the write-times are different.");

            var items = listView.Items.Cast<FileComparisonResult>().ToList();

            StartBgAction(() => {
                // retrieve a list of the supposedly-changed items that are actually identical
                var results =
                    SortFilesSearchDuplicates.SearchForIdenticalFilesWithDifferentWriteTimes(
                    _settings.LeftDirectory, _settings.RightDirectory, items);

                WrapInvoke(() => {
                    // show the result in the UI, in the left-most column
                    Utils.MessageBox("Found " + results.Count + " identical file(s).");
                    foreach (var item in results)
                    {
                        item.SubItems[0].Text = "Identical Contents";
                    }

                    listView.Refresh();
                });
            });
        }

        private void propagateMovesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // some files were renamed on the left. let's propagate these renames on the right.
            var moves = new List<IUndoableFileOp>();
            foreach (var item in SelectedItems())
            {
                if (item.Type != FileComparisonResultType.Left_Only ||
                    !item.SubItems[0].Text.Contains(Utils.Sep))
                {
                    continue;
                }

                var src = _settings.RightDirectory + item.SubItems[0].Text;
                var dest = _settings.RightDirectory + item.FileInfoLeft.Filename;
                var existsOnLeft = File.Exists(
                    _settings.LeftDirectory + item.SubItems[0].Text);

                // if does exist on left: this is a copied file
                // if doesn't exist on left: this is a renamed file
                if (existsOnLeft)
                {
                    moves.Add(new FileOpFileCopy(src, dest));
                }
                else
                {
                    moves.Add(new FileOpFileMove(src, dest));
                }
            }

            if (moves.Count == 0)
            {
                Utils.MessageBox("None of the selected items is a moved file " +
                    "found by 'Search for Moved Files...'");
            }
            else
            {
                tbRight.Visible = true;
                moveFiles(moves, tbRight);
                Utils.MessageBox("" + moves.Count + " move(s) performed. " +
                    "Run File->Refresh to update UI.");
            }
        }

        private void propagateWriteTimesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // some files were identical. let's propagate LMT times from left to right.
            var moves = new List<IUndoableFileOp>();
            foreach (var item in SelectedItems())
            {
                if (item.Type == FileComparisonResultType.Changed &&
                    item.SubItems[0].Text == "Identical Contents")
                {
                    // set the LMT on the right to be the LMT of the left
                    var path = _settings.RightDirectory + item.FileInfoRight.Filename;
                    moves.Add(new FileOpFileSetWritetime(path,
                        item.FileInfoRight.LastModifiedTime,
                        item.FileInfoLeft.LastModifiedTime));
                }
            }

            if (moves.Count == 0)
            {
                Utils.MessageBox("None of the selected items is an identical file " +
                    "found by 'Search for Identical Files...'");
            }
            else
            {
                tbRight.Visible = true;
                moveFiles(moves, tbRight);
                Utils.MessageBox("Write time updated for " + moves.Count + " file(s). " +
                    "Run File->Refresh to update UI.");
            }
        }

        IEnumerable<FileComparisonResult> SelectedItems()
        {
            // for testability, override what selected items are seen.
            if (_testSelectedItems.Count > 0)
            {
                return _testSelectedItems;
            }
            else
            {
                return listView.SelectedItems.Cast<FileComparisonResult>();
            }
        }
    }

    // Allow undoing file operations, following the Command design pattern.
    // Overwrites are not allowed. Existing files should first be Move()d out of the way.
    // This enables perfect undo-ability for all moves and copies.
    sealed class FileOpFileMove : IUndoableFileOp
    {
        public FileOpFileMove(string source, string dest)
        {
            Source = source;
            Dest = dest;
        }

        public string Source { get; }
        public string Dest { get; }

        public override string ToString()
        {
            return "Moved " + Source + " to " + Dest;
        }

        public void Do()
        {
            if (File.Exists(Dest))
            {
                throw new IOException("File already exists at " + Dest);
            }
            else if (!Directory.Exists(Path.GetDirectoryName(Dest)))
            {
                // create missing directories
                Directory.CreateDirectory(Path.GetDirectoryName(Dest));
            }

            File.Move(Source, Dest);
        }

        public void Undo()
        {
            if (File.Exists(Source))
            {
                throw new IOException("File already exists at " + Source);
            }

            File.Move(Dest, Source);
        }
    }

    sealed class FileOpFileCopy : IUndoableFileOp
    {
        public FileOpFileCopy(string source, string dest)
        {
            Source = source;
            Dest = dest;
        }

        public string Source { get; }
        public string Dest { get; }

        public override string ToString()
        {
            return "Copied " + Source + " to " + Dest;
        }

        public void Do()
        {
            if (File.Exists(Dest))
            {
                throw new IOException("File already exists at " + Dest);
            }
            else if (!Directory.Exists(Path.GetDirectoryName(Dest)))
            {
                // create missing directories
                Directory.CreateDirectory(Path.GetDirectoryName(Dest));
            }

            File.Copy(Source, Dest);
        }

        public void Undo()
        {
            string hashSource = Utils.GetSha512(Source);
            string hashDest = Utils.GetSha512(Dest);
            if (!File.Exists(Source) || !File.Exists(Dest) || hashSource != hashDest)
            {
                throw new IOException("Cannot undo Copy because " + Source +
                    " and " + Dest + " are not equal files");
            }

            File.Delete(Dest);
        }
    }

    sealed class FileOpFileSetWritetime : IUndoableFileOp
    {
        public FileOpFileSetWritetime(string path, DateTime timeFrom, DateTime timeTo)
        {
            Source = path;
            Dest = path;
            TimeFrom = timeFrom;
            TimeTo = timeTo;
        }

        public string Source { get; }
        public string Dest { get; }
        public DateTime TimeFrom { get; }
        public DateTime TimeTo { get; }

        public override string ToString()
        {
            return "Set write-time " + Source + " to " + TimeTo;
        }

        public void Do()
        {
            File.SetLastWriteTimeUtc(Source, TimeTo);
        }

        public void Undo()
        {
            File.SetLastWriteTimeUtc(Source, TimeFrom);
        }
    }
}
