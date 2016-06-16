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
            this.colItemType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lblLeft = new System.Windows.Forms.Label();
            this.lblRight = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.lblAction, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.listView, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblLeft, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.lblRight, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 4);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(449, 345);
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
            this.colItemType,
            this.colStatus,
            this.colPath});
            this.listView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView.FullRowSelect = true;
            this.listView.Location = new System.Drawing.Point(3, 16);
            this.listView.Name = "listView";
            this.listView.Size = new System.Drawing.Size(443, 200);
            this.listView.TabIndex = 1;
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.View = System.Windows.Forms.View.Details;
            this.listView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listView_ColumnClick);
            this.listView.SelectedIndexChanged += new System.EventHandler(this.listView_SelectedIndexChanged);
            // 
            // colItemType
            // 
            this.colItemType.Text = " ";
            this.colItemType.Width = 38;
            // 
            // colStatus
            // 
            this.colStatus.Text = "Type";
            // 
            // colPath
            // 
            this.colPath.Text = "Path";
            this.colPath.Width = 305;
            // 
            // lblLeft
            // 
            this.lblLeft.AutoSize = true;
            this.lblLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLeft.Location = new System.Drawing.Point(3, 219);
            this.lblLeft.Name = "lblLeft";
            this.lblLeft.Size = new System.Drawing.Size(443, 13);
            this.lblLeft.TabIndex = 2;
            this.lblLeft.Text = "label2";
            // 
            // lblRight
            // 
            this.lblRight.AutoSize = true;
            this.lblRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRight.Location = new System.Drawing.Point(3, 232);
            this.lblRight.Name = "lblRight";
            this.lblRight.Size = new System.Drawing.Size(443, 13);
            this.lblRight.TabIndex = 3;
            this.lblRight.Text = "label3";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.linkLabel1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 248);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(443, 94);
            this.panel1.TabIndex = 4;
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(9, 17);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(10, 13);
            this.linkLabel1.TabIndex = 0;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "f";
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
            // FormSortFilesList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(449, 345);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "FormSortFilesList";
            this.Text = " ";
            this.Load += new System.EventHandler(this.FormSortFilesList_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label lblAction;
        private System.Windows.Forms.ListView listView;
        private System.Windows.Forms.Label lblLeft;
        private System.Windows.Forms.Label lblRight;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.ColumnHeader colItemType;
        private System.Windows.Forms.ColumnHeader colStatus;
        private System.Windows.Forms.ColumnHeader colPath;
        private System.Windows.Forms.ImageList imageList;
    }
}