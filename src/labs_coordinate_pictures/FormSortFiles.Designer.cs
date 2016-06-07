namespace labs_coordinate_pictures
{
    partial class FormSortFiles
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
            this.lblAction = new System.Windows.Forms.Label();
            this.lblLeftDirDesc = new System.Windows.Forms.Label();
            this.lblRightDirDesc = new System.Windows.Forms.Label();
            this.cmbLeftDir = new System.Windows.Forms.ComboBox();
            this.cmbRightDir = new System.Windows.Forms.ComboBox();
            this.btnSetLeftDir = new System.Windows.Forms.Button();
            this.btnSetRightDir = new System.Windows.Forms.Button();
            this.btnSwap = new System.Windows.Forms.Button();
            this.checkAllowDifferSeconds = new System.Windows.Forms.CheckBox();
            this.checkMirror = new System.Windows.Forms.CheckBox();
            this.txtSkipDirs = new System.Windows.Forms.TextBox();
            this.lblSkipDirs = new System.Windows.Forms.Label();
            this.lblSkipFiles = new System.Windows.Forms.Label();
            this.txtSkipFiles = new System.Windows.Forms.TextBox();
            this.btnShowRobo = new System.Windows.Forms.Button();
            this.txtShowRobo = new System.Windows.Forms.TextBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.checkAllowDifferDST = new System.Windows.Forms.CheckBox();
            this.checkPreview = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // lblAction
            // 
            this.lblAction.AutoSize = true;
            this.lblAction.Location = new System.Drawing.Point(12, 9);
            this.lblAction.Name = "lblAction";
            this.lblAction.Size = new System.Drawing.Size(40, 13);
            this.lblAction.TabIndex = 0;
            this.lblAction.Text = "Action:";
            // 
            // lblLeftDirDesc
            // 
            this.lblLeftDirDesc.AutoSize = true;
            this.lblLeftDirDesc.Location = new System.Drawing.Point(15, 70);
            this.lblLeftDirDesc.Name = "lblLeftDirDesc";
            this.lblLeftDirDesc.Size = new System.Drawing.Size(50, 26);
            this.lblLeftDirDesc.TabIndex = 0;
            this.lblLeftDirDesc.Text = "Source \r\ndirectory:";
            // 
            // lblRightDirDesc
            // 
            this.lblRightDirDesc.AutoSize = true;
            this.lblRightDirDesc.Location = new System.Drawing.Point(15, 123);
            this.lblRightDirDesc.Name = "lblRightDirDesc";
            this.lblRightDirDesc.Size = new System.Drawing.Size(60, 26);
            this.lblRightDirDesc.TabIndex = 0;
            this.lblRightDirDesc.Text = "Destination\r\ndirectory:";
            // 
            // cmbLeftDir
            // 
            this.cmbLeftDir.AllowDrop = true;
            this.cmbLeftDir.FormattingEnabled = true;
            this.cmbLeftDir.Location = new System.Drawing.Point(97, 70);
            this.cmbLeftDir.Name = "cmbLeftDir";
            this.cmbLeftDir.Size = new System.Drawing.Size(331, 21);
            this.cmbLeftDir.TabIndex = 1;
            // 
            // cmbRightDir
            // 
            this.cmbRightDir.AllowDrop = true;
            this.cmbRightDir.FormattingEnabled = true;
            this.cmbRightDir.Location = new System.Drawing.Point(97, 123);
            this.cmbRightDir.Name = "cmbRightDir";
            this.cmbRightDir.Size = new System.Drawing.Size(331, 21);
            this.cmbRightDir.TabIndex = 1;
            // 
            // btnSetLeftDir
            // 
            this.btnSetLeftDir.Location = new System.Drawing.Point(435, 70);
            this.btnSetLeftDir.Name = "btnSetLeftDir";
            this.btnSetLeftDir.Size = new System.Drawing.Size(51, 21);
            this.btnSetLeftDir.TabIndex = 2;
            this.btnSetLeftDir.Text = "...";
            this.btnSetLeftDir.UseVisualStyleBackColor = true;
            this.btnSetLeftDir.Click += new System.EventHandler(this.btnSetLeftDir_Click);
            // 
            // btnSetRightDir
            // 
            this.btnSetRightDir.Location = new System.Drawing.Point(435, 123);
            this.btnSetRightDir.Name = "btnSetRightDir";
            this.btnSetRightDir.Size = new System.Drawing.Size(51, 21);
            this.btnSetRightDir.TabIndex = 2;
            this.btnSetRightDir.Text = "...";
            this.btnSetRightDir.UseVisualStyleBackColor = true;
            this.btnSetRightDir.Click += new System.EventHandler(this.btnSetRightDir_Click);
            // 
            // btnSwap
            // 
            this.btnSwap.Location = new System.Drawing.Point(435, 96);
            this.btnSwap.Name = "btnSwap";
            this.btnSwap.Size = new System.Drawing.Size(51, 21);
            this.btnSwap.TabIndex = 2;
            this.btnSwap.Text = "Swap";
            this.btnSwap.UseVisualStyleBackColor = true;
            this.btnSwap.Click += new System.EventHandler(this.btnSwap_Click);
            // 
            // checkAllowDifferSeconds
            // 
            this.checkAllowDifferSeconds.AutoSize = true;
            this.checkAllowDifferSeconds.Location = new System.Drawing.Point(15, 173);
            this.checkAllowDifferSeconds.Name = "checkAllowDifferSeconds";
            this.checkAllowDifferSeconds.Size = new System.Drawing.Size(436, 17);
            this.checkAllowDifferSeconds.TabIndex = 3;
            this.checkAllowDifferSeconds.Text = "Allow file times to differ by less than $ seconds (for FAT systems with imprecise" +
    " filetimes)";
            this.checkAllowDifferSeconds.UseVisualStyleBackColor = true;
            // 
            // checkMirror
            // 
            this.checkMirror.AutoSize = true;
            this.checkMirror.Checked = true;
            this.checkMirror.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkMirror.Location = new System.Drawing.Point(15, 218);
            this.checkMirror.Name = "checkMirror";
            this.checkMirror.Size = new System.Drawing.Size(157, 17);
            this.checkMirror.TabIndex = 3;
            this.checkMirror.Text = "Mirror (update modified files)";
            this.checkMirror.UseVisualStyleBackColor = true;
            // 
            // txtSkipDirs
            // 
            this.txtSkipDirs.Location = new System.Drawing.Point(105, 245);
            this.txtSkipDirs.Multiline = true;
            this.txtSkipDirs.Name = "txtSkipDirs";
            this.txtSkipDirs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtSkipDirs.Size = new System.Drawing.Size(323, 40);
            this.txtSkipDirs.TabIndex = 6;
            // 
            // lblSkipDirs
            // 
            this.lblSkipDirs.AutoSize = true;
            this.lblSkipDirs.Location = new System.Drawing.Point(13, 248);
            this.lblSkipDirs.Name = "lblSkipDirs";
            this.lblSkipDirs.Size = new System.Drawing.Size(82, 13);
            this.lblSkipDirs.TabIndex = 0;
            this.lblSkipDirs.Text = "Skip directories:";
            // 
            // lblSkipFiles
            // 
            this.lblSkipFiles.AutoSize = true;
            this.lblSkipFiles.Location = new System.Drawing.Point(13, 294);
            this.lblSkipFiles.Name = "lblSkipFiles";
            this.lblSkipFiles.Size = new System.Drawing.Size(52, 13);
            this.lblSkipFiles.TabIndex = 0;
            this.lblSkipFiles.Text = "Skip files:";
            // 
            // txtSkipFiles
            // 
            this.txtSkipFiles.Location = new System.Drawing.Point(105, 291);
            this.txtSkipFiles.Multiline = true;
            this.txtSkipFiles.Name = "txtSkipFiles";
            this.txtSkipFiles.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtSkipFiles.Size = new System.Drawing.Size(323, 40);
            this.txtSkipFiles.TabIndex = 6;
            // 
            // btnShowRobo
            // 
            this.btnShowRobo.Location = new System.Drawing.Point(237, 347);
            this.btnShowRobo.Name = "btnShowRobo";
            this.btnShowRobo.Size = new System.Drawing.Size(134, 40);
            this.btnShowRobo.TabIndex = 2;
            this.btnShowRobo.Text = "Show Robocopy Command";
            this.btnShowRobo.UseVisualStyleBackColor = true;
            this.btnShowRobo.Click += new System.EventHandler(this.btnShowRobo_Click);
            // 
            // txtShowRobo
            // 
            this.txtShowRobo.BackColor = System.Drawing.SystemColors.Control;
            this.txtShowRobo.Location = new System.Drawing.Point(12, 347);
            this.txtShowRobo.Multiline = true;
            this.txtShowRobo.Name = "txtShowRobo";
            this.txtShowRobo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtShowRobo.Size = new System.Drawing.Size(219, 40);
            this.txtShowRobo.TabIndex = 6;
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(377, 347);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(109, 40);
            this.btnStart.TabIndex = 2;
            this.btnStart.Text = "Run Robocopy Command";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // checkAllowDifferDST
            // 
            this.checkAllowDifferDST.AutoSize = true;
            this.checkAllowDifferDST.Location = new System.Drawing.Point(15, 195);
            this.checkAllowDifferDST.Name = "checkAllowDifferDST";
            this.checkAllowDifferDST.Size = new System.Drawing.Size(389, 17);
            this.checkAllowDifferDST.TabIndex = 3;
            this.checkAllowDifferDST.Text = "Allow file times to differ by exactly one hour (compensate for DST differences)";
            this.checkAllowDifferDST.UseVisualStyleBackColor = true;
            // 
            // checkPreview
            // 
            this.checkPreview.AutoSize = true;
            this.checkPreview.Location = new System.Drawing.Point(285, 218);
            this.checkPreview.Name = "checkPreview";
            this.checkPreview.Size = new System.Drawing.Size(86, 17);
            this.checkPreview.TabIndex = 3;
            this.checkPreview.Text = "Preview only";
            this.checkPreview.UseVisualStyleBackColor = true;
            // 
            // FormSortFiles
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(505, 399);
            this.Controls.Add(this.txtShowRobo);
            this.Controls.Add(this.txtSkipFiles);
            this.Controls.Add(this.txtSkipDirs);
            this.Controls.Add(this.checkPreview);
            this.Controls.Add(this.checkMirror);
            this.Controls.Add(this.checkAllowDifferDST);
            this.Controls.Add(this.checkAllowDifferSeconds);
            this.Controls.Add(this.btnSetRightDir);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnShowRobo);
            this.Controls.Add(this.btnSwap);
            this.Controls.Add(this.btnSetLeftDir);
            this.Controls.Add(this.cmbRightDir);
            this.Controls.Add(this.lblSkipFiles);
            this.Controls.Add(this.cmbLeftDir);
            this.Controls.Add(this.lblSkipDirs);
            this.Controls.Add(this.lblRightDirDesc);
            this.Controls.Add(this.lblLeftDirDesc);
            this.Controls.Add(this.lblAction);
            this.Name = "FormSortFiles";
            this.Text = " ";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblAction;
        private System.Windows.Forms.Label lblLeftDirDesc;
        private System.Windows.Forms.Label lblRightDirDesc;
        private System.Windows.Forms.ComboBox cmbLeftDir;
        private System.Windows.Forms.ComboBox cmbRightDir;
        private System.Windows.Forms.Button btnSetLeftDir;
        private System.Windows.Forms.Button btnSetRightDir;
        private System.Windows.Forms.Button btnSwap;
        private System.Windows.Forms.CheckBox checkAllowDifferSeconds;
        private System.Windows.Forms.CheckBox checkMirror;
        private System.Windows.Forms.TextBox txtSkipDirs;
        private System.Windows.Forms.Label lblSkipDirs;
        private System.Windows.Forms.Label lblSkipFiles;
        private System.Windows.Forms.TextBox txtSkipFiles;
        private System.Windows.Forms.Button btnShowRobo;
        private System.Windows.Forms.TextBox txtShowRobo;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.CheckBox checkAllowDifferDST;
        private System.Windows.Forms.CheckBox checkPreview;
    }
}