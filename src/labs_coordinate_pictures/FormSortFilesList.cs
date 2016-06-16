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
    public partial class FormSortFilesList : Form
    {
        SortFilesAction _action;
        SortFilesSettings _settings;
        FileComparisonResult[] _results;
        string _caption;
        int _sortCol;

        public FormSortFilesList(SortFilesAction action, SortFilesSettings settings, string caption)
        {
            InitializeComponent();
            _action = action;
            _settings = settings;
            _caption = caption;

            listView.SmallImageList = imageList;
            lblAction.Text = "Searching...";
            lblLeft.Text = "";
            lblRight.Text = "";
            linkLabel1.Text = "";
        }

        private void FormSortFilesList_Load(object sender, EventArgs e)
        {
            if (listView.Items != null && listView.Items.Count > 0)
            {
                return;
            }

            RefreshItems();
        }

        void RefreshItems()
        {
            Utils.RunLongActionInThread(this, new Action(RunSortFilesAction));
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
                listView.Items.Clear();
                listView.Items.AddRange(_results);
                listView.Refresh();
                lblAction.Text = _caption + "\r\n";
                lblAction.Text += "" + _results.Length + " file(s) listed:";
            }));
        }

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {

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
    }
}
