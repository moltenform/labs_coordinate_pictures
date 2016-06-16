using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public partial class FormSortFilesList : Form
    {
        SortFilesAction _action;
        SortFilesSettings _settings;
        FileComparisonResult[] _results;
        UndoStack<List<Tuple<string, string>>> _undoFileMoves =
            new UndoStack<List<Tuple<string, string>>>();

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
            _testSelectedItems = new List<FileComparisonResult>();

            listView.SmallImageList = imageList;
            lblAction.Text = "Searching...";
            btnCopyFileLeft.Click += (o, e) => OnClickCopyFile(true, true);
            btnCopyFileRight.Click += (o, e) => OnClickCopyFile(false, true);
            btnDeleteLeft.Click += (o, e) => OnClickDeleteFile(true, true);
            btnDeleteRight.Click += (o, e) => OnClickDeleteFile(false, true);
            btnShowLeft.Click += (o, e) => OnClickShowFile(true);
            btnShowRight.Click += (o, e) => OnClickShowFile(false);
            btnUndo.Click += (o, e) => OnUndoClick(true);
        }

        private void FormSortFilesList_Load(object sender, EventArgs e)
        {
            if (listView.Items != null && listView.Items.Count > 0)
            {
                return;
            }

            btnRefresh_Click();
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
                    Utils.MessageErr("Unexpected action.");
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

            // only collect info we have cached, avoid hitting the disk, for responsiveness
            StringBuilder sbLeftFiles = new StringBuilder();
            StringBuilder sbLeftStats = new StringBuilder();
            StringBuilder sbRightFiles = new StringBuilder();
            StringBuilder sbRightStats = new StringBuilder();
            foreach (var item in SelectedItems())
            {
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

        private void btnDetails_Click(object sender, EventArgs e)
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

        internal void listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            IEnumerable<FileComparisonResult> displayResults = null;
            if (e.Column == 0)
            {
                // use original order
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
            
        }

        internal void OnClickDeleteFile(bool left, bool needConfirm)
        {
            if (CheckSelectedItemsSameType() || _synchronous)
            {
                var moves = new List<Tuple<string, string>>();
                foreach (var item in SelectedItems())
                {
                    var path = left ? item.GetLeft(_settings.LeftDirectory) :
                        item.GetRight(_settings.RightDirectory);

                    if (path != null)
                    {
                        moves.Add(Tuple.Create(path, Utils.GetSoftDeleteDestination(path)));
                    }
                }

                if (!needConfirm || Utils.AskToConfirm("Delete " + moves.Count + " files?"))
                {
                    moveFiles(moves, left ? tbLeft : tbRight);
                }
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

        void btnCompareMerge_Click(object sender, EventArgs e)
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
                    Utils.Run(mergeExe, args, shellExecute: false, waitForExit: false, hideWindow: false);
                }
            }
        }

        void btnCopyFilenames_Click(object sender, EventArgs e)
        {
            if (SelectedItems().Count() > 0)
            {
                var query = from item in SelectedItems()
                            select item.SubItems[1].Text + "\t" + item.SubItems[2].Text;

                Clipboard.SetText(string.Join(Utils.NL, query));
            }
        }

        void btnRefresh_Click(object sender = null, EventArgs e = null)
        {
            StartBgAction(RunSortFilesAction);
        }

        internal void GetTestHooks(out TextBox outTbLeft, out TextBox outTbRight,
            out ListView outListView, out List<FileComparisonResult> outSelectedItems,
            out UndoStack<List<Tuple<string, string>>> outUndoStack)
        {
            outTbLeft = tbLeft;
            outTbRight = tbRight;
            outListView = listView;
            outSelectedItems = _testSelectedItems;
            outUndoStack = _undoFileMoves;
        }

        void moveFiles(List<Tuple<string, string>> fileMoves, TextBox tbLog)
        {
            StartBgAction(() =>
            {
                // clear UI
                WrapInvoke(() =>
                {
                    tbLeft.Text = "";
                    tbRight.Text = "";
                });

                foreach (var move in fileMoves)
                {
                    // move file, exceptions are ok
                    File.Move(move.Item1, move.Item2);

                    // print to UI
                    WrapInvoke(() =>
                    {
                        tbLog.AppendText(Utils.NL + Utils.NL + "Moved " + move.Item1 + " to " + move.Item2);
                    });
                }

                // print to UI
                WrapInvoke(() =>
                {
                    tbLog.AppendText(Utils.NL + Utils.NL + "Complete.");
                });

                // add to undo stack
                _undoFileMoves.Add(fileMoves);
            });
        }

        internal void OnUndoClick(bool needConfirm)
        {
            StartBgAction(() => {
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
                        from item in fileMoves select "Move " + item.Item2 + " to " + item.Item1);
                    if (!needConfirm || Utils.AskToConfirm("Move these files?" + Utils.NL + preview))
                    {
                        // if an exception occurs partially through, our progress will be saved
                        while (fileMoves.Count > 0)
                        {
                            var move = fileMoves[fileMoves.Count - 1];

                            // move file, exceptions are ok and will be caught by RunLongActionInThread
                            File.Move(move.Item2, move.Item1);

                            // remove from list if successful.
                            fileMoves.RemoveAt(fileMoves.Count - 1);

                            // print to UI
                            WrapInvoke(() =>
                            {
                                tbLeft.AppendText(Utils.NL + Utils.NL + "Moved " + move.Item2 + " to " + move.Item1);
                            });
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
            if (_synchronous)
            {
                fn();
            }
            else
            {
                this.Invoke(fn);
            }
        }

        IEnumerable<FileComparisonResult> SelectedItems()
        {
            if (_synchronous)
            {
                return _testSelectedItems;
            }
            else
            {
                return listView.SelectedItems.Cast<FileComparisonResult>();
            }
        }
    }
}
