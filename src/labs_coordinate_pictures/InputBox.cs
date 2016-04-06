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
        System.ComponentModel.Container _components = null;
        Button _btnCancel;
        Button _btnOK;
        ComboBox _comboBox;
        Label _label;
        PersistMostRecentlyUsedList _mru;

        public InputBoxForm(InputBoxHistory currentKey)
        {
            InitializeComponent();
            _mru = new PersistMostRecentlyUsedList(currentKey);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = " ";
            this._comboBox.Focus();
            this.AllowDrop = true;
        }

        // add MRU history, suggestions, and clipboard contents to the list of examples.
        public static IEnumerable<string> GetInputSuggestions(string strCurrent, InputBoxHistory history, PersistMostRecentlyUsedList saver, bool useClipboard, bool mustBeDirectory, string[] more)
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
                form._label.Text = strPrompt;

                var entries = GetInputSuggestions(strCurrent, history, form._mru, useClipboard, mustBeDirectory, more).ToArray();
                form._comboBox.Items.Clear();
                foreach (var s in entries)
                {
                    form._comboBox.Items.Add(s);
                }

                form._comboBox.Text = entries.Length > 0 ? entries[0] : "";
                form.ShowDialog();
                if (form.DialogResult != DialogResult.OK)
                {
                    return null;
                }

                if (mustBeDirectory && !Directory.Exists(form._comboBox.Text))
                {
                    MessageBox.Show("Directory does not exist");
                    return null;
                }

                // save to history
                form._mru.AddToHistory(form._comboBox.Text);
                return form._comboBox.Text;
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
                if (_components != null)
                {
                    _components.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        // originally based on http://www.java2s.com/Code/CSharp/GUI-Windows-Form/Defineyourowndialogboxandgetuserinput.htm
        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this._label = new System.Windows.Forms.Label();
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this._comboBox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();

            // _label
            this._label.Location = new System.Drawing.Point(12, 8);
            this._label.Name = "_label";
            this._label.Size = new System.Drawing.Size(410, 187);
            this._label.TabIndex = 6;
            this._label.Text = "Type in your message.";

            // _btnOK
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.Location = new System.Drawing.Point(259, 246);
            this._btnOK.Name = "_btnOK";
            this._btnOK.Size = new System.Drawing.Size(70, 24);
            this._btnOK.TabIndex = 2;
            this._btnOK.Text = "OK";

            // _btnCancel
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.Location = new System.Drawing.Point(335, 246);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(70, 24);
            this._btnCancel.TabIndex = 3;
            this._btnCancel.Text = "Cancel";

            // _comboBox
            this._comboBox.FormattingEnabled = true;
            this._comboBox.Location = new System.Drawing.Point(22, 208);
            this._comboBox.Name = "_comboBox";
            this._comboBox.Size = new System.Drawing.Size(383, 21);
            this._comboBox.TabIndex = 1;

            // InputBoxForm
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(434, 287);
            this.ControlBox = false;
            this.Controls.Add(this._comboBox);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this._label);
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
                    _comboBox.Text = filePath;
                }
            }
        }
    }

    // save MRU history, limits number of entries with a queue structure.
    public class PersistMostRecentlyUsedList
    {
        public const int MaxHistoryEntries = 10;
        public const int MaxEntryLength = 300;
        readonly string _delimiter = "||||";
        InputBoxHistory _historyKey = InputBoxHistory.None;
        ConfigKey _configsKey = ConfigKey.None;
        Configs _configs;
        string[] _currentItems;
        public PersistMostRecentlyUsedList(InputBoxHistory historyKey,
            Configs configs = null)
        {
            _historyKey = historyKey;
            _configs = configs ?? Configs.Current;
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
                _currentItems = _configs.Get(_configsKey).Split(
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

                    // save to configs
                    _configs.Set(_configsKey, string.Join(_delimiter, listNext));

                    // refresh in-memory cache
                    Get();
                }
            }
        }
    }
}