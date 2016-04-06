// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public class InputBoxForm : Form
    {
        System.ComponentModel.Container components = null;
        Button btnCancel;
        Button btnOK;
        ComboBox comboBox1;
        Label label1;
        HistorySaver saver;

        public InputBoxForm(InputBoxHistory currentKey)
        {
            InitializeComponent();
            saver = new HistorySaver(currentKey);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = " ";
            this.comboBox1.Focus();
            this.AllowDrop = true;
        }

        // add MRU history, suggestions, and clipboard contents to the list of examples.
        public static IEnumerable<string> GetInputSuggestions(string strCurrent, InputBoxHistory history, HistorySaver saver, bool useClipboard, bool mustBeDirectory, string[] more)
        {
            List<string> comboEntries = new List<string>();
            if (!string.IsNullOrEmpty(strCurrent))
                comboEntries.Add(strCurrent);

            if (useClipboard && !string.IsNullOrEmpty(Utils.GetClipboard()))
                comboEntries.Add(Utils.GetClipboard());

            if (history != InputBoxHistory.None)
                comboEntries.AddRange(saver.Get());

            if (more != null)
                comboEntries.AddRange(more);

            return comboEntries.Where(entry => FilenameUtils.IsPathRooted(entry) || !mustBeDirectory);
        }

        // ask user for string input.
        public static string GetStrInput(string strPrompt, string strCurrent = null, InputBoxHistory history = InputBoxHistory.None, string[] more = null, bool useClipboard = true, bool mustBeDirectory = false)
        {
            using (InputBoxForm form = new InputBoxForm(history))
            {
                form.label1.Text = strPrompt;

                var entries = GetInputSuggestions(strCurrent, history, form.saver, useClipboard, mustBeDirectory, more).ToArray();
                form.comboBox1.Items.Clear();
                foreach (var s in entries)
                {
                    form.comboBox1.Items.Add(s);
                }

                form.comboBox1.Text = entries.Length > 0 ? entries[0] : "";
                form.ShowDialog();
                if (form.DialogResult != DialogResult.OK)
                {
                    return null;
                }

                if (mustBeDirectory && !Directory.Exists(form.comboBox1.Text))
                {
                    MessageBox.Show("Directory does not exist");
                    return null;
                }

                // save to history
                form.saver.AddToHistory(form.comboBox1.Text);
                return form.comboBox1.Text;
            }
        }

        public static int? GetInteger(string strPrompt, int nDefault = 0, InputBoxHistory history = InputBoxHistory.None)
        {
            int fromClipboard = 0;
            var clipboardContainsInt = int.TryParse(Utils.GetClipboard(), out fromClipboard);
            string s = GetStrInput(strPrompt, nDefault.ToString(), history,
                useClipboard: clipboardContainsInt);

            int result = 0;
            if (s == null || s == "" || !int.TryParse(s, out result))
                return null;
            else
                return result;
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

            // label1
            this.label1.Location = new System.Drawing.Point(12, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(410, 187);
            this.label1.TabIndex = 6;
            this.label1.Text = "Type in your message.";

            // btnOK
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(259, 246);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(70, 24);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "OK";

            // btnCancel
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(335, 246);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(70, 24);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";

            // comboBox1
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(22, 208);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(383, 21);
            this.comboBox1.TabIndex = 1;

            // InputBoxForm
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
                string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                string filePath = filePaths[0];
                if (!string.IsNullOrEmpty(filePath))
                {
                    comboBox1.Text = filePath;
                }
            }
        }
    }

    // save MRU history, limits number of entries with a queue structure.
    public class HistorySaver
    {
        public const int MaxHistoryEntries = 10;
        public const int MaxEntryLength = 300;
        readonly string _delimiter = "||||";
        InputBoxHistory _historyKey = InputBoxHistory.None;
        ConfigKey _configsKey = ConfigKey.None;
        string[] _currentItems;
        public HistorySaver(InputBoxHistory historyKey)
        {
            _historyKey = historyKey;
            if (_historyKey != InputBoxHistory.None)
            {
                var strKey = "MRU" + _historyKey.ToString();
                if (!Enum.TryParse(strKey, out _configsKey))
                    throw new CoordinatePicturesException("not a key:" + strKey);
            }
        }

        public string[] Get()
        {
            if (_historyKey != InputBoxHistory.None)
            {
                _currentItems = Configs.Current.Get(_configsKey).Split(
                    new string[] { _delimiter }, StringSplitOptions.RemoveEmptyEntries);
                return _currentItems;
            }
            else
            {
                return new string[] { };
            }
        }

        public void AddToHistory(string s)
        {
            if (_historyKey != InputBoxHistory.None)
            {
                if (_currentItems == null)
                    Get();

                // only add if it's not already in the list, and s does not contain _delimiter.
                var index = Array.IndexOf(_currentItems, s);
                if (!string.IsNullOrEmpty(s) && index != 0 && s.Length < MaxEntryLength && !s.Contains(_delimiter))
                {
                    List<string> listNext = new List<string>(_currentItems);

                    // if it's also elsewhere in the list, remove that one
                    if (index != -1)
                        listNext.RemoveAt(index);

                    // insert new entry at the top
                    listNext.Insert(0, s);

                    // if we've reached the limit, cut out the extra ones
                    while (listNext.Count > MaxHistoryEntries)
                        listNext.RemoveAt(listNext.Count - 1);

                    // reset our cached list
                    Configs.Current.Set(_configsKey, string.Join(_delimiter, listNext));
                    Get();
                }
            }
        }
    }
}