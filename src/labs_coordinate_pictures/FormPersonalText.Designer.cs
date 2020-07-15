namespace labs_coordinate_pictures
{
    partial class FormPersonalText
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormPersonalText));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lblCurrentStatus = new System.Windows.Forms.Label();
            this.lblCategories = new System.Windows.Forms.Label();
            this.listBox = new System.Windows.Forms.ListBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuUndo = new System.Windows.Forms.ToolStripMenuItem();
            this.categoriesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editCategoriesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.playlistToolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyFilenamesInADirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.getHtmlFromClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.getURLsInCopiedTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuCopy = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuEditLocal = new System.Windows.Forms.ToolStripMenuItem();
            this.tableLayoutPanel1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 133F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.lblCurrentStatus, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblCategories, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.listBox, 1, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 28);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1539, 362);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // lblCurrentStatus
            // 
            this.lblCurrentStatus.AutoSize = true;
            this.lblCurrentStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCurrentStatus.Location = new System.Drawing.Point(137, 0);
            this.lblCurrentStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCurrentStatus.Name = "lblCurrentStatus";
            this.lblCurrentStatus.Size = new System.Drawing.Size(1398, 31);
            this.lblCurrentStatus.TabIndex = 0;
            this.lblCurrentStatus.Text = "Choose File->Open File... to begin.";
            // 
            // lblCategories
            // 
            this.lblCategories.AutoSize = true;
            this.lblCategories.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCategories.Location = new System.Drawing.Point(4, 31);
            this.lblCategories.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCategories.Name = "lblCategories";
            this.lblCategories.Size = new System.Drawing.Size(125, 331);
            this.lblCategories.TabIndex = 1;
            this.lblCategories.Text = "categories";
            // 
            // listBox
            // 
            this.listBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox.FormattingEnabled = true;
            this.listBox.ItemHeight = 16;
            this.listBox.Location = new System.Drawing.Point(137, 35);
            this.listBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.listBox.Name = "listBox";
            this.listBox.ScrollAlwaysVisible = true;
            this.listBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBox.Size = new System.Drawing.Size(1398, 323);
            this.listBox.TabIndex = 2;
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.categoriesToolStripMenuItem,
            this.playlistToolsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(8, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(1539, 28);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openFileToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(44, 24);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openFileToolStripMenuItem
            // 
            this.openFileToolStripMenuItem.Name = "openFileToolStripMenuItem";
            this.openFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openFileToolStripMenuItem.Size = new System.Drawing.Size(216, 26);
            this.openFileToolStripMenuItem.Text = "Open File...";
            this.openFileToolStripMenuItem.Click += new System.EventHandler(this.openFileToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuUndo,
            this.mnuCopy,
            this.mnuEditLocal});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(47, 24);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // mnuUndo
            // 
            this.mnuUndo.Name = "mnuUndo";
            this.mnuUndo.ShortcutKeyDisplayString = "";
            this.mnuUndo.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
            this.mnuUndo.Size = new System.Drawing.Size(216, 26);
            this.mnuUndo.Text = "Undo";
            this.mnuUndo.Click += new System.EventHandler(this.mnuUndo_Click);
            // 
            // categoriesToolStripMenuItem
            // 
            this.categoriesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.editCategoriesToolStripMenuItem});
            this.categoriesToolStripMenuItem.Name = "categoriesToolStripMenuItem";
            this.categoriesToolStripMenuItem.Size = new System.Drawing.Size(92, 24);
            this.categoriesToolStripMenuItem.Text = "Categories";
            // 
            // editCategoriesToolStripMenuItem
            // 
            this.editCategoriesToolStripMenuItem.Name = "editCategoriesToolStripMenuItem";
            this.editCategoriesToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.editCategoriesToolStripMenuItem.Size = new System.Drawing.Size(244, 26);
            this.editCategoriesToolStripMenuItem.Text = "Edit Categories...";
            this.editCategoriesToolStripMenuItem.Click += new System.EventHandler(this.editCategoriesToolStripMenuItem_Click);
            // 
            // playlistToolsToolStripMenuItem
            // 
            this.playlistToolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyFilenamesInADirectoryToolStripMenuItem,
            this.getHtmlFromClipboardToolStripMenuItem,
            this.getURLsInCopiedTextToolStripMenuItem});
            this.playlistToolsToolStripMenuItem.Name = "playlistToolsToolStripMenuItem";
            this.playlistToolsToolStripMenuItem.Size = new System.Drawing.Size(102, 24);
            this.playlistToolsToolStripMenuItem.Text = "PlaylistTools";
            // 
            // copyFilenamesInADirectoryToolStripMenuItem
            // 
            this.copyFilenamesInADirectoryToolStripMenuItem.Name = "copyFilenamesInADirectoryToolStripMenuItem";
            this.copyFilenamesInADirectoryToolStripMenuItem.Size = new System.Drawing.Size(277, 26);
            this.copyFilenamesInADirectoryToolStripMenuItem.Text = "Copy filenames in a directory";
            this.copyFilenamesInADirectoryToolStripMenuItem.Click += new System.EventHandler(this.copyFilenamesInADirectoryToolStripMenuItem_Click);
            // 
            // getHtmlFromClipboardToolStripMenuItem
            // 
            this.getHtmlFromClipboardToolStripMenuItem.Name = "getHtmlFromClipboardToolStripMenuItem";
            this.getHtmlFromClipboardToolStripMenuItem.Size = new System.Drawing.Size(277, 26);
            this.getHtmlFromClipboardToolStripMenuItem.Text = "Get clipboard as HTML";
            this.getHtmlFromClipboardToolStripMenuItem.Click += new System.EventHandler(this.getHtmlFromClipboardToolStripMenuItem_Click);
            // 
            // getURLsInCopiedTextToolStripMenuItem
            // 
            this.getURLsInCopiedTextToolStripMenuItem.Name = "getURLsInCopiedTextToolStripMenuItem";
            this.getURLsInCopiedTextToolStripMenuItem.Size = new System.Drawing.Size(277, 26);
            this.getURLsInCopiedTextToolStripMenuItem.Text = "Get URLs in copied text";
            this.getURLsInCopiedTextToolStripMenuItem.Click += new System.EventHandler(this.getURLsInCopiedTextToolStripMenuItem_Click);
            // 
            // mnuCopy
            // 
            this.mnuCopy.Name = "mnuCopy";
            this.mnuCopy.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.mnuCopy.Size = new System.Drawing.Size(216, 26);
            this.mnuCopy.Text = "Copy";
            // 
            // mnuEditLocal
            // 
            this.mnuEditLocal.Name = "mnuEditLocal";
            this.mnuEditLocal.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.mnuEditLocal.Size = new System.Drawing.Size(216, 26);
            this.mnuEditLocal.Text = "Edit Local";
            // 
            // FormPersonalText
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1539, 390);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "FormPersonalText";
            this.Text = "FormPersonalText";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FormPersonalText_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FormPersonalText_KeyUp);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label lblCurrentStatus;
        private System.Windows.Forms.Label lblCategories;
        private System.Windows.Forms.ListBox listBox;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mnuUndo;
        private System.Windows.Forms.ToolStripMenuItem openFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem categoriesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editCategoriesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem playlistToolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyFilenamesInADirectoryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem getHtmlFromClipboardToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem getURLsInCopiedTextToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mnuCopy;
        private System.Windows.Forms.ToolStripMenuItem mnuEditLocal;
    }
}