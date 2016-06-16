namespace labs_coordinate_pictures
{
    partial class FormSortFilesList
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSortFilesList));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lblAction = new System.Windows.Forms.Label();
            this.listView = new System.Windows.Forms.ListView();
            this.colIcon = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.btnShowRight = new System.Windows.Forms.Button();
            this.btnShowLeft = new System.Windows.Forms.Button();
            this.btnDeleteRight = new System.Windows.Forms.Button();
            this.btnDeleteLeft = new System.Windows.Forms.Button();
            this.btnCopyFileRight = new System.Windows.Forms.Button();
            this.btnCopyFileLeft = new System.Windows.Forms.Button();
            this.tbRight = new System.Windows.Forms.TextBox();
            this.lblOnLeft = new System.Windows.Forms.Label();
            this.lblOnRight = new System.Windows.Forms.Label();
            this.tbLeft = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnCopyFilenames = new System.Windows.Forms.Button();
            this.btnCompareMerge = new System.Windows.Forms.Button();
            this.btnUndo = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.btnDetails = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.lblAction, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.listView, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 3);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(449, 460);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // lblAction
            // 
            this.lblAction.AutoSize = true;
            this.lblAction.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAction.Location = new System.Drawing.Point(3, 0);
            this.lblAction.Name = "lblAction";
            this.lblAction.Size = new System.Drawing.Size(443, 13);
            this.lblAction.TabIndex = 0;
            this.lblAction.Text = "label1";
            // 
            // listView
            // 
            this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colIcon,
            this.colType,
            this.colPath});
            this.listView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView.FullRowSelect = true;
            this.listView.Location = new System.Drawing.Point(3, 16);
            this.listView.Name = "listView";
            this.listView.Size = new System.Drawing.Size(443, 191);
            this.listView.TabIndex = 1;
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.View = System.Windows.Forms.View.Details;
            this.listView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listView_ColumnClick);
            this.listView.SelectedIndexChanged += new System.EventHandler(this.listView_SelectedIndexChanged);
            // 
            // colIcon
            // 
            this.colIcon.Text = " ";
            this.colIcon.Width = 38;
            // 
            // colType
            // 
            this.colType.Text = "Type";
            this.colType.Width = 56;
            // 
            // colPath
            // 
            this.colPath.Text = "Path";
            this.colPath.Width = 305;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.btnShowRight, 1, 4);
            this.tableLayoutPanel2.Controls.Add(this.btnShowLeft, 0, 4);
            this.tableLayoutPanel2.Controls.Add(this.btnDeleteRight, 1, 3);
            this.tableLayoutPanel2.Controls.Add(this.btnDeleteLeft, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.btnCopyFileRight, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.btnCopyFileLeft, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.tbRight, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.lblOnLeft, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.lblOnRight, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.tbLeft, 0, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 213);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 5;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(443, 214);
            this.tableLayoutPanel2.TabIndex = 5;
            // 
            // btnShowRight
            // 
            this.btnShowRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnShowRight.Location = new System.Drawing.Point(224, 189);
            this.btnShowRight.Name = "btnShowRight";
            this.btnShowRight.Size = new System.Drawing.Size(216, 22);
            this.btnShowRight.TabIndex = 10;
            this.btnShowRight.Text = "Show in Explorer";
            this.btnShowRight.UseVisualStyleBackColor = true;
            // 
            // btnShowLeft
            // 
            this.btnShowLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnShowLeft.Location = new System.Drawing.Point(3, 189);
            this.btnShowLeft.Name = "btnShowLeft";
            this.btnShowLeft.Size = new System.Drawing.Size(215, 22);
            this.btnShowLeft.TabIndex = 9;
            this.btnShowLeft.Text = "Show in Explorer";
            this.btnShowLeft.UseVisualStyleBackColor = true;
            // 
            // btnDeleteRight
            // 
            this.btnDeleteRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDeleteRight.Location = new System.Drawing.Point(224, 159);
            this.btnDeleteRight.Name = "btnDeleteRight";
            this.btnDeleteRight.Size = new System.Drawing.Size(216, 24);
            this.btnDeleteRight.TabIndex = 8;
            this.btnDeleteRight.Text = "Delete on Right";
            this.btnDeleteRight.UseVisualStyleBackColor = true;
            // 
            // btnDeleteLeft
            // 
            this.btnDeleteLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDeleteLeft.Location = new System.Drawing.Point(3, 159);
            this.btnDeleteLeft.Name = "btnDeleteLeft";
            this.btnDeleteLeft.Size = new System.Drawing.Size(215, 24);
            this.btnDeleteLeft.TabIndex = 7;
            this.btnDeleteLeft.Text = "Delete on Left";
            this.btnDeleteLeft.UseVisualStyleBackColor = true;
            // 
            // btnCopyFileRight
            // 
            this.btnCopyFileRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCopyFileRight.Location = new System.Drawing.Point(224, 129);
            this.btnCopyFileRight.Name = "btnCopyFileRight";
            this.btnCopyFileRight.Size = new System.Drawing.Size(216, 24);
            this.btnCopyFileRight.TabIndex = 6;
            this.btnCopyFileRight.Text = "<- Copy File";
            this.btnCopyFileRight.UseVisualStyleBackColor = true;
            // 
            // btnCopyFileLeft
            // 
            this.btnCopyFileLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCopyFileLeft.Location = new System.Drawing.Point(3, 129);
            this.btnCopyFileLeft.Name = "btnCopyFileLeft";
            this.btnCopyFileLeft.Size = new System.Drawing.Size(215, 24);
            this.btnCopyFileLeft.TabIndex = 5;
            this.btnCopyFileLeft.Text = "Copy File ->";
            this.btnCopyFileLeft.UseVisualStyleBackColor = true;
            // 
            // txtRight
            // 
            this.tbRight.BackColor = System.Drawing.SystemColors.Control;
            this.tbRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbRight.Location = new System.Drawing.Point(224, 16);
            this.tbRight.Multiline = true;
            this.tbRight.Name = "txtRight";
            this.tbRight.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbRight.Size = new System.Drawing.Size(216, 107);
            this.tbRight.TabIndex = 3;
            // 
            // lblOnLeft
            // 
            this.lblOnLeft.AutoSize = true;
            this.lblOnLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOnLeft.Location = new System.Drawing.Point(3, 0);
            this.lblOnLeft.Name = "lblOnLeft";
            this.lblOnLeft.Size = new System.Drawing.Size(215, 13);
            this.lblOnLeft.TabIndex = 0;
            this.lblOnLeft.Text = "Left:";
            // 
            // lblOnRight
            // 
            this.lblOnRight.AutoSize = true;
            this.lblOnRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOnRight.Location = new System.Drawing.Point(224, 0);
            this.lblOnRight.Name = "lblOnRight";
            this.lblOnRight.Size = new System.Drawing.Size(216, 13);
            this.lblOnRight.TabIndex = 1;
            this.lblOnRight.Text = "Right:";
            // 
            // txtLeft
            // 
            this.tbLeft.BackColor = System.Drawing.SystemColors.Control;
            this.tbLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbLeft.Location = new System.Drawing.Point(3, 16);
            this.tbLeft.Multiline = true;
            this.tbLeft.Name = "txtLeft";
            this.tbLeft.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbLeft.Size = new System.Drawing.Size(215, 107);
            this.tbLeft.TabIndex = 2;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnCopyFilenames);
            this.panel1.Controls.Add(this.btnDetails);
            this.panel1.Controls.Add(this.btnCompareMerge);
            this.panel1.Controls.Add(this.btnUndo);
            this.panel1.Controls.Add(this.btnRefresh);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 433);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(443, 24);
            this.panel1.TabIndex = 6;
            // 
            // btnCopyFilenames
            // 
            this.btnCopyFilenames.Location = new System.Drawing.Point(170, 1);
            this.btnCopyFilenames.Name = "btnCopyFilenames";
            this.btnCopyFilenames.Size = new System.Drawing.Size(102, 23);
            this.btnCopyFilenames.TabIndex = 0;
            this.btnCopyFilenames.Text = "Copy Filenames";
            this.btnCopyFilenames.UseVisualStyleBackColor = true;
            this.btnCopyFilenames.Click += new System.EventHandler(this.btnCopyFilenames_Click);
            // 
            // btnCompareMerge
            // 
            this.btnCompareMerge.Location = new System.Drawing.Point(6, 1);
            this.btnCompareMerge.Name = "btnCompareMerge";
            this.btnCompareMerge.Size = new System.Drawing.Size(75, 23);
            this.btnCompareMerge.TabIndex = 0;
            this.btnCompareMerge.Text = "Compare...";
            this.btnCompareMerge.UseVisualStyleBackColor = true;
            this.btnCompareMerge.Click += new System.EventHandler(this.btnCompareMerge_Click);
            // 
            // btnUndo
            // 
            this.btnUndo.Location = new System.Drawing.Point(359, 1);
            this.btnUndo.Name = "btnUndo";
            this.btnUndo.Size = new System.Drawing.Size(75, 23);
            this.btnUndo.TabIndex = 0;
            this.btnUndo.Text = "Undo...";
            this.btnUndo.UseVisualStyleBackColor = true;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(278, 1);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 23);
            this.btnRefresh.TabIndex = 0;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // imageList
            // 
            this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList.Images.SetKeyName(0, "list-remove.png");
            this.imageList.Images.SetKeyName(1, "files_differ.png");
            this.imageList.Images.SetKeyName(2, "files_leftonly.png");
            this.imageList.Images.SetKeyName(3, "files_rightonly.png");
            this.imageList.Images.SetKeyName(4, "files_same.png");
            this.imageList.Images.SetKeyName(5, "move.png");
            // 
            // btnDetails
            // 
            this.btnDetails.Location = new System.Drawing.Point(87, 1);
            this.btnDetails.Name = "btnDetails";
            this.btnDetails.Size = new System.Drawing.Size(75, 23);
            this.btnDetails.TabIndex = 0;
            this.btnDetails.Text = "Details";
            this.btnDetails.UseVisualStyleBackColor = true;
            this.btnDetails.Click += new System.EventHandler(this.btnDetails_Click);
            // 
            // FormSortFilesList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(449, 460);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "FormSortFilesList";
            this.Text = " ";
            this.Load += new System.EventHandler(this.FormSortFilesList_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label lblAction;
        private System.Windows.Forms.ListView listView;
        private System.Windows.Forms.ColumnHeader colIcon;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ColumnHeader colPath;
        private System.Windows.Forms.ImageList imageList;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label lblOnLeft;
        private System.Windows.Forms.Label lblOnRight;
        private System.Windows.Forms.TextBox tbLeft;
        private System.Windows.Forms.TextBox tbRight;
        private System.Windows.Forms.Button btnCopyFileLeft;
        private System.Windows.Forms.Button btnCopyFileRight;
        private System.Windows.Forms.Button btnDeleteLeft;
        private System.Windows.Forms.Button btnDeleteRight;
        private System.Windows.Forms.Button btnShowRight;
        private System.Windows.Forms.Button btnShowLeft;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnCopyFilenames;
        private System.Windows.Forms.Button btnCompareMerge;
        private System.Windows.Forms.Button btnUndo;
        private System.Windows.Forms.Button btnDetails;
    }
}