// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public partial class FormPersonalText : Form
    {
        string _currentfile = null;
        UndoStack<UndoableTextOperation> _undo = new UndoStack<UndoableTextOperation>();
        Dictionary<string, string> _categoryKeyBindings = new Dictionary<string, string>();
        public FormPersonalText()
        {
            InitializeComponent();
            this.KeyPreview = true;
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Please select a text file.";
            dialog.CheckPathExists = true;
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text files (*.txt)|*.txt";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _currentfile = dialog.FileName;

                // make a backup file if one doesn't already exist
                var backupname = _currentfile + ".coordinatepicturesbackup.txt";
                if (!File.Exists(backupname))
                {
                    File.Copy(_currentfile, backupname);
                }

                Reload();
            }
        }

        void Reload()
        {
            if (_currentfile != null)
            {
                // refresh listbox contents
                string[] lines = File.ReadAllLines(_currentfile, Encoding.UTF8);
                listBox.Items.Clear();
                foreach (var line in lines)
                {
                    listBox.Items.Add(line);
                }

                // get categories string
                var categories = Configs.Current.Get(ConfigKey.CategoriesModeText);
                if (categories == null || categories.Length == 0)
                {
                    categories = "G/good/good|B/bad/bad";
                    Configs.Current.Set(ConfigKey.CategoriesModeText, categories);
                }

                // refresh categories in ui
                _categoryKeyBindings.Clear();
                lblCategories.Text = "";
                var tuples = ModeUtils.CategoriesStringToTuple(categories);
                foreach (var tuple in tuples)
                {
                    lblCategories.Text += tuple.Item1 + "    " +
                        tuple.Item2 + Utils.NL + Utils.NL;
                    _categoryKeyBindings[tuple.Item1] = tuple.Item3;
                }
            }
        }

        void AssignCategory(string categoryName)
        {
            if (listBox.Items.Count > 0)
            {
                string destfile = Path.GetDirectoryName(_currentfile) + Utils.Sep +
                    "coordinatepictures_" + categoryName + ".txt";
                AddToFile(destfile);
                listBox.SelectedItems.Clear();
                listBox.SelectedItems.Add(0);
            }
        }

        void AddToFile(string destfile)
        {
            UndoableTextOperation op = new UndoableTextOperation();
            op.Destfile = destfile;
            op.Srcfile = _currentfile;
            op.ListIndices = listBox.SelectedIndices.Cast<int>().ToArray();
            op.TextContents = listBox.SelectedItems.Cast<string>().ToArray();
            if (op.ListIndices.Length > 0)
            {
                op.Do(this.listBox);
                _undo.Add(op);
            }
            else
            {
                MessageBox.Show("Nothing selected.");
            }
        }

        private void mnuUndo_Click(object sender, EventArgs e)
        {
            UndoableTextOperation op = _undo.PeekUndo();
            if (op != null)
            {
                if (Utils.AskToConfirm("Undo moving " + op.TextContents.Length +
                    " lines of text to " + op.Destfile + "?"))
                {
                    op.Undo(this.listBox);
                    _undo.Undo();
                }
            }
        }

        private void editCategoriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (FormGallery.EditCategories("", ConfigKey.CategoriesModeText, InputBoxHistory.None))
            {
                Reload();
            }
        }

        private void FormPersonalText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Shift && !e.Control && !e.Alt)
            {
                var categoryId = FormGallery.CheckKeyBindingsToAssignCategory(e.KeyCode,
                    _categoryKeyBindings);
                if (categoryId != null)
                    AssignCategory(categoryId);

                e.Handled = true; // prevent propagation
                e.SuppressKeyPress = true; // don't want the listbox to pick this up
            }
        }

        private void FormPersonalText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Shift && !e.Control && !e.Alt &&
                ((e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z) ||
                (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)))
            {
                e.Handled = true; // prevent propagation
                e.SuppressKeyPress = true; // don't want the listbox to pick this up
            }
        }

        private void copyFilenamesInADirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string file = Utils.AskOpenFileDialog("Choose one file from the directory...");
            if (file != null)
            {
                var dir = Path.GetDirectoryName(file);
                var s = "";
                foreach (var fl in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    s += Path.GetFileName(fl) + "\r\n";
                }

                Clipboard.SetText(s);
                MessageBox.Show("Filenames copied.");
            }
        }

        private void getHtmlFromClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText(TextDataFormat.Html))
            {
                var html = Clipboard.GetText(TextDataFormat.Html);
                Clipboard.SetText(html, TextDataFormat.UnicodeText);
                MessageBox.Show("got html from clipboard");
            }
            else
            {
                MessageBox.Show("clipboard doesn't appear to have html");
            }
        }

        private void getURLsInCopiedTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                var urlsSeen = new HashSet<string>();
                var urls = new List<string>();
                foreach (var match in Regex.Matches(Clipboard.GetText(), "https?://[^ '\"<>]+"))
                {
                    var url = ((Match)match).ToString();
                    if (!urlsSeen.Contains(url))
                    {
                        urlsSeen.Add(url);
                        urls.Add(url);
                    }
                }

                if (Utils.AskToConfirm("got " + urls.Count + " urls. keep only youtube?"))
                {
                    var c = urls.Where((s) => s.Contains("//www.youtube.com/watch?v="));
                    Clipboard.SetText(string.Join("\r\n", c));
                }
                else
                {
                    Clipboard.SetText(string.Join("\r\n", urls));
                }
            }
            else
            {
                MessageBox.Show("clipboard doesn't appear to have text");
            }
        }
    }

    public class UndoableTextOperation
    {
        public string Srcfile { get; set; }
        public string Destfile { get; set; }
        public string[] TextContents { get; set; }
        public int[] ListIndices { get; set; }

        public void Do(ListBox listbox)
        {
            listbox.SelectedItems.Clear();
            TestUtil.IsEq(ListIndices.Length, TextContents.Length);
            for (int i = ListIndices.Length - 1; i >= 0; i--)
            {
                listbox.Items.RemoveAt(ListIndices[i]);
            }

            // write to dest file
            File.AppendAllLines(Destfile, TextContents);

            // rewrite src file
            var srclines = listbox.Items.Cast<string>();
            File.WriteAllLines(Srcfile, srclines);
        }

        public void Undo(ListBox listbox)
        {
            // add back to source file
            listbox.SelectedItems.Clear();
            TestUtil.IsEq(ListIndices.Length, TextContents.Length);
            for (int i = 0; i < ListIndices.Length; i++)
            {
                listbox.Items.Insert(ListIndices[i], TextContents[i]);
            }

            // rewrite src file
            var srclines = listbox.Items.Cast<string>();
            File.WriteAllLines(Srcfile, srclines);

            // remove from the dest file -- as long as it actually is the content we expect.
            var linesInDest = File.ReadAllLines(Destfile, Encoding.UTF8);
            var hasExpectedContent = doesHaveExpectedContent(linesInDest, TextContents);
            if (hasExpectedContent)
            {
                var newlines = new List<string>(linesInDest);
                newlines.RemoveRange(linesInDest.Length - TextContents.Length, TextContents.Length);

                // rewrite dest file
                File.WriteAllLines(Destfile, newlines);
            }
        }

        static bool doesHaveExpectedContent(string[] linesInDest, string[] textContents)
        {
            if (linesInDest.Length < textContents.Length)
            {
                MessageBox.Show("We re-added the text but could not fully undo. " +
                    "File is shorter than expected");
                return false;
            }

            for (int i = 0; i < textContents.Length; i++)
            {
                var expected = textContents[i];
                var got = linesInDest[(linesInDest.Length - textContents.Length) + i];
                if (expected != got)
                {
                    MessageBox.Show("We re-added the text but could not fully undo. Expected '" +
                        expected + "' but got '" + got + "'.");
                    return false;
                }
            }

            return true;
        }
    }
}
