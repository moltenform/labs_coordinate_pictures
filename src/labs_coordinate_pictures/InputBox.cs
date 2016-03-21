using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;

namespace labs_coordinate_pictures
{
    public class InputBoxForm : Form
    {
        public enum History
        {
            None,
            OpenImageDirectory,
            OpenMusicDirectory,
            Rename,
        }

        public static string GetStrInput(string strPrompt, string strCurrent = null, History history = History.None, string[] more = null, bool useClipboard = true)
        {
            InputBoxForm myForm = new InputBoxForm(history);
            myForm.label1.Text = strPrompt;

            List<string> comboEntries = new List<string>();
            if (!string.IsNullOrEmpty(strCurrent))
                comboEntries.Add(strCurrent);

            if (!string.IsNullOrEmpty(Utils.GetClipboard()))
                comboEntries.Add(Utils.GetClipboard());

            if (history != History.None)
                comboEntries.AddRange(myForm.saver.Get());

            if (more != null)
                comboEntries.AddRange(more);

            myForm.comboBox1.Items.Clear();
            foreach (var s in comboEntries)
                myForm.comboBox1.Items.Add(s);

            myForm.comboBox1.Text = comboEntries.Count > 0 ? comboEntries[0] : "";
            myForm.ShowDialog(new Form());
            if (myForm.DialogResult != DialogResult.OK)
                return null;

            // save to history
            myForm.saver.AddToHistory(myForm.comboBox1.Text);
            return myForm.comboBox1.Text;
        }

        public static int? GetInteger(string strPrompt, int nDefault = 0, History history = History.None)
        {
            int fromClipboard = 0;
            string s = GetStrInput(strPrompt, nDefault.ToString(), history, 
                useClipboard: int.TryParse(Utils.GetClipboard(), out fromClipboard));

            int result = 0;
            if (s == null || s == "" || !int.TryParse(s, out result))
                return null;
            else
                return result;
        }

        private System.ComponentModel.Container components = null;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private ComboBox comboBox1;
        private System.Windows.Forms.Label label1;
        HistorySaver saver;
        public InputBoxForm(History currentKey)
        {
            InitializeComponent();
            saver = new HistorySaver(currentKey);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = " ";
            this.comboBox1.Focus();
            this.AllowDrop = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        // originally based on http://www.java2s.com/Code/CSharp/GUI-Windows-Form/Defineyourowndialogboxandgetuserinput.htm
        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(410, 187);
            this.label1.TabIndex = 6;
            this.label1.Text = "Type in your message.";
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(259, 246);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(70, 24);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "OK";
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(335, 246);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(70, 24);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(22, 208);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(383, 21);
            this.comboBox1.TabIndex = 1;
            // 
            // InputBoxForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(434, 287);
            this.ControlBox = false;
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InputBoxForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Input Box Dialog";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.InputBoxForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.InputBoxForm_DragEnter);
            this.ResumeLayout(false);

        }
        #endregion

        private void InputBoxForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void InputBoxForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                string[] filePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
                string filePath = filePaths[0];
                if (!String.IsNullOrEmpty(filePath))
                {
                    comboBox1.Text = filePath;
                }
            }
        }
    }


    public class HistorySaver
    {
        public const int cMaxHistoryEntries = 10;
        public const int cMaxEntryLength = 300;
        InputBoxForm.History _historyKey = InputBoxForm.History.None;
        ConfigsPersistedKeys _configsKey = ConfigsPersistedKeys.None;
        string[] _returned;
        public HistorySaver(InputBoxForm.History historyKey)
        {
            _historyKey = historyKey;
            if (_historyKey != InputBoxForm.History.None)
            {
                var strKey = "MRU" + _historyKey.ToString();
                if (!Enum.TryParse(strKey, out _configsKey))
                    throw new CoordinatePicturesException("not a key:" + strKey);
            }
        }

        public string[] Get()
        {
            if (_historyKey != InputBoxForm.History.None)
            {
                _returned = Configs.Current.Get(_configsKey).Split(
                    new string[] { "||||" }, StringSplitOptions.RemoveEmptyEntries);
                return _returned;
            }
            else
            {
                return new string[] { };
            }
        }

        public void AddToHistory(string s)
        {
            if (_historyKey != InputBoxForm.History.None)
            {
                if (_returned == null)
                    Get();

                var index = Array.IndexOf(_returned, s);
                if (!string.IsNullOrEmpty(s) && index != 0 && s.Length < cMaxEntryLength && !s.Contains("||||"))
                {
                    List<string> listNext = new List<string>(_returned);

                    // if it's also elsewhere in the list, remove that one
                    if (index != -1)
                        listNext.RemoveAt(index);

                    // insert new entry at the top
                    listNext.Insert(0, s);

                    // if we've reached the limit, cut out the extra ones
                    while (listNext.Count > cMaxHistoryEntries)
                        listNext.RemoveAt(listNext.Count - 1);

                    // reset our cached list
                    Configs.Current.Set(_configsKey, string.Join("||||", listNext));
                    Get();
                }
            }
        }
    }
}