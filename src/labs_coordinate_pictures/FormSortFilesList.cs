using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
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

        object _lock = new object();
        int _sortCol = int.MaxValue;
        string _caption;

        public FormSortFilesList(SortFilesAction action, SortFilesSettings settings, string caption)
        {
            InitializeComponent();
            _action = action;
            _settings = settings;
            _caption = caption;

            listView.SmallImageList = imageList;
            lblAction.Text = "Searching...";
            btnCopyFileLeft.Click += (o, e) => OnClickCopyFile(true);
            btnCopyFileRight.Click += (o, e) => OnClickCopyFile(false);
            btnDeleteLeft.Click += (o, e) => OnClickDeleteFile(true);
            btnDeleteRight.Click += (o, e) => OnClickDeleteFile(false);
            btnShowLeft.Click += (o, e) => OnClickShowFile(true);
            btnShowRight.Click += (o, e) => OnClickShowFile(false);
        }

        private void FormSortFilesList_Load(object sender, EventArgs e)
        {
            if (listView.Items != null && listView.Items.Count > 0)
            {
                return;
            }

            btnRefresh_Click();
        }

        void RunSortFilesAction()
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
            Invoke((MethodInvoker)(() =>
            {
                listView_ColumnClick(null, new ColumnClickEventArgs(0));
                listView.Columns[1].Width = -2; // autosize to the longest item in the column
                lblAction.Text = _caption + "\r\n";
                lblAction.Text += "" + _results.Length + " file(s) listed:";
            }));
        }

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            Control[] left = new Control[] { lblOnLeft, txtLeft,
                btnCopyFileLeft, btnDeleteLeft, btnShowLeft };
            Control[] right = new Control[] { lblOnRight, txtRight,
                btnCopyFileRight, btnDeleteRight, btnShowRight };

        }

        private void listView_ColumnClick(object sender, ColumnClickEventArgs e)
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

        void OnClickCopyFile(bool left)
        {
            var query = from item in listView.SelectedItems.Cast<FileComparisonResult>()
                        where left ? (item.FileInfoLeft != null) : (item.FileInfoRight != null)
                        select left ? item.FileInfoLeft.Filename : item.FileInfoRight.Filename;

            Clipboard.SetText(string.Join("\r\n", query));
        }

        void OnClickDeleteFile(bool left)
        {
            throw new NotImplementedException();
        }

        void OnClickShowFile(bool left)
        {
            throw new NotImplementedException();
        }

        void btnCompareMerge_Click(object sender, EventArgs e)
        {

        }

        void btnCopyFilenames_Click(object sender, EventArgs e)
        {
            var query = from item in listView.SelectedItems.Cast<ListViewItem>()
                        select item.SubItems[1].Text + "\t" + item.SubItems[2].Text;

            Clipboard.SetText(string.Join("\r\n", query));
        }

        void btnRefresh_Click(object sender = null, EventArgs e = null)
        {
            Utils.RunLongActionInThread(_lock, this, RunSortFilesAction);
        }

        void moveFiles(List<Tuple<string, string>> fileMoves, bool needConfirm, TextBox tbLog)
        {
            
        }

        void btnUndo_Click(object sender, EventArgs e)
        {
           
        }
    }
}