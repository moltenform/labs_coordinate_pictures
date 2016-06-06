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
        FilePathsListViewItem[] _list = new FilePathsListViewItem[] { };

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
            var items = FindMovedFiles.FindQuickDifferencesByModifiedTimeAndFilesize(_settings).ToArray();

            // update UI on main thread
            this.Invoke((MethodInvoker)(() =>
            {
                this._list = items;
                this.listView.VirtualListSize = this._list.Length;
                this.listView.Refresh();

                lblAction.Text = "Currently shown: differences between directories.";
                linkLabel1.Text = "Currently, differences between the directories are shown. \r\n" +
                    "Click here to compute file hashes to see which \r\n" +
                    "differences are just moved files.";
                linkLabel1.Links.Clear();
                linkLabel1.Links.Add(linkLabel1.Text.IndexOf("here ", StringComparison.Ordinal),
                    "here".Length, "link");
            }));
        }

        void FindMovedFilesPartTwo()
        {
            // iterate through directories, on worker thread
            var items = FindMovedFiles.DifferencesToFindDupes(this._list).ToArray();

            // update UI on main thread
            this.Invoke((MethodInvoker)(() =>
            {
                this._list = items;
                this.listView.VirtualListSize = this._list.Length;
                this.listView.Refresh();

                lblAction.Text = "Currently shown: differences between directories, showing moved files.";
                linkLabel1.Text = lblAction.Text;
            }));
        }
    }

    public class FilePathsListViewItem : ListViewItem
    {
        public FilePathsListViewItem(string firstPath, string secondPath, FilePathsListViewItemType status,
            long firstlength, long firstfiletime, long secondlength, long secondfiletime)
            : base(new string[] {
                "File",
                status.ToString().Replace("_", " "),
                firstPath }, (int)status)
        {
            FirstPath = firstPath;
            SecondPath = secondPath;
            Status = status;
            FirstFileLength = firstlength;
            FirstLastModifiedTime = firstfiletime;
            SecondFileLength = secondlength;
            SecondLastModifiedTime = secondfiletime;
        }

        public string FirstPath { get; private set; }
        public string SecondPath { get; private set; }
        public FilePathsListViewItemType Status { get; private set; }
        public long FirstFileLength { get; private set; }
        public long FirstLastModifiedTime { get; private set; }
        public long SecondFileLength { get; private set; }
        public long SecondLastModifiedTime { get; private set; }
    }
}
