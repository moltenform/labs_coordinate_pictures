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
        bool _startedLoading;
        bool _startedFindMovedFiles;
        FileComparisonResult[] _list = new FileComparisonResult[] { };

        public FormSortFilesList(SortFilesAction action, SortFilesSettings settings)
        {
            InitializeComponent();
            _action = action;
            _settings = settings;
            listView.SmallImageList = imageList;
            listView.VirtualListSize = _list.Length;
            lblAction.Text = "";
            lblLeft.Text = "";
            lblRight.Text = "";
            linkLabel1.Text = "";
        }

        private void listView_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (e.ItemIndex >= 0 && e.ItemIndex < _list.Length)
            {
                e.Item = _list[e.ItemIndex];
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!_startedFindMovedFiles && _startedLoading && _action == SortFilesAction.SearchDifferences)
            {
                _startedFindMovedFiles = true;
                Utils.RunLongActionInThread(this, new Action(FindMovedFilesPartOne));
            }
        }

        private void FormSortFilesList_Load(object sender, EventArgs e)
        {
            if (!_startedLoading)
            {
                _startedLoading = true;
                if (_action == SortFilesAction.SearchDifferences)
                {
                    Utils.RunLongActionInThread(this, new Action(FindMovedFilesPartOne));
                }
            }
        }

        void FindMovedFilesPartOne()
        {
            // iterate through directories, on worker thread
            //var items = FindMovedFiles.FindQuickDifferencesByModifiedTimeAndFilesize(_settings).ToArray();

            // update UI on main thread
            this.Invoke((MethodInvoker)(() =>
            {
                this._list = null; //todo: set items
                this.listView.VirtualListSize = this._list.Length;
                this.listView.Refresh();
                
            }));
        }
    }
}
