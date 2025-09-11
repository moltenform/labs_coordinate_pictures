// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public partial class FormPersonalTwitter : Form
    {
        /*
         * how to use FormPersonalTwitter
         * ...by Ben, 2 yrs after i wrote this code...
         * 
         * 1) find combine_images.py and move_back_after_ps_resize.py, put them in a dir like
         * C:\data\data_1\b\documents\WText\twitter2018\for-coordinate-pictures\tools
         * 2) find tweet_template.html and normalize.css, put them in a dir like
         * C:\data\data_1\b\documents\WText\twitter2018\for-coordinate-pictures\tools\tools_text
         * 
         * run labs_coordinate_pictures. turn on 'personal' mode if it's not already (ctrl-alt-e)
         * from options menu, "set sorttwitter input location" -- doesn't matter, set it to anything
         * from options menu, "set sorttwitter output location" -- set it to C:\data\data_1\b\documents\WText\twitter2018\for-coordinate-pictures
         * 
         * go to C:\data\l3\get twitter\archives and pick a target directory
         * if there is a file "currentTweets.txt" you are good to go
         * if there is a only file "currentIndexOfTweets.txt" then...
         *      the best approach is probably to unrar the mthml file and
         *      run the python script again, (skipping downloading images), in order to get a "currentTweets.txt"
         *
         * if the 2 attached images are both copies of the same--
         * hit shift-B to say "we want to use the big image"
         * hit shift-S to say "we want to use the small image"
         * middle-click an attached picture to remove it
         * 
         * hit delete on bad tweets
         * hit Shift-G on good tweets
         * hit Shift-D on semi-good tweets
         * 
         * 
         * 
         * 
         * */
        PictureBox[] _arPictureBox = new PictureBox[14];
        Label[] _arPictureBoxCaptions = new Label[14];
        string pathPythonScript;
        TweetInfoCollection _tweetsWorking;
        TweetInfoCollection _tweetsLvl1;
        TweetInfoCollection _tweetsLvl2;
        TweetInfoCollection _tweetsLvl3;
        TweetInfoCollection[] _arrCollections;
        string _dir;
        int _curIndex = 0;
        UndoStack<IUndoableCommand> _undoStack = new UndoStack<IUndoableCommand>();

        public FormPersonalTwitter(string dirPassedIn=null)
        {
            InitializeComponent();
            pathPythonScript = Path.Combine(Configs.Current.Get(ConfigKey.FilepathSortTwitterImagesDestinationDir), "tools", "combine_images.py");

            _dir = dirPassedIn;
            if (_dir == null)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Title = "please locate currentTweets.txt - from any archive you want to process";
                dialog.CheckPathExists = true;
                dialog.DefaultExt = ".txt";
                dialog.Filter = "txt files (*.txt)|*.txt";
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;
                TestUtil.IsTrue(dialog.FileName.ToLowerInvariant().Contains("currenttweets.txt"));
                _dir = Path.GetDirectoryName(dialog.FileName);
            }
            if (!File.Exists(_dir + "\\currentTweets.txt"))
            {
                Utils.MessageErr("could not find " + _dir + "\\currentTweets.txt");
                return;
            }

            if (!File.Exists(_dir + "\\currentTweetsOriginal.txt"))
                File.Copy(_dir + "\\currentTweets.txt", _dir + "\\currentTweetsOriginal.txt");

            _tweetsWorking = TweetInfoCollection.FromFile(_dir + "\\currentTweets.txt");
            var tweetsFileinfo = Utils.SplitByString(File.ReadAllText(_dir + "\\currentTweets.txt", Encoding.UTF8), "==================")[0];
            if (!File.Exists(_dir + "\\currentTweetsLvl1.txt"))
                File.WriteAllText(_dir + "\\currentTweetsLvl1.txt", tweetsFileinfo, Encoding.UTF8);
            if (!File.Exists(_dir + "\\currentTweetsLvl2.txt"))
                File.WriteAllText(_dir + "\\currentTweetsLvl2.txt", tweetsFileinfo, Encoding.UTF8);
            if (!File.Exists(_dir + "\\currentTweetsLvl3.txt"))
                File.WriteAllText(_dir + "\\currentTweetsLvl3.txt", tweetsFileinfo, Encoding.UTF8);

            _tweetsLvl1 = TweetInfoCollection.FromFile(_dir + "\\currentTweetsLvl1.txt");
            _tweetsLvl2 = TweetInfoCollection.FromFile(_dir + "\\currentTweetsLvl2.txt");
            _tweetsLvl3 = TweetInfoCollection.FromFile(_dir + "\\currentTweetsLvl3.txt");
            _arrCollections = new TweetInfoCollection[] { _tweetsWorking, _tweetsLvl1, _tweetsLvl2, _tweetsLvl3 };

            for (int i = 0; i < _arPictureBox.Length; i++)
            {
                _arPictureBox[i] = new PictureBox();
                _arPictureBox[i].Dock = DockStyle.Fill;
                _arPictureBox[i].SizeMode = PictureBoxSizeMode.AutoSize;
                _arPictureBox[i].MouseUp += AarPictureBox_MouseUp;
                _arPictureBoxCaptions[i] = new Label();
                _arPictureBoxCaptions[i].Dock = DockStyle.Fill;
            }

            linkLabel1.TabStop = false; // otherwise it eats key presses.
            this.KeyPreview = true;  // important for keyboard shortcuts!
            webBrowser1.WebBrowserShortcutsEnabled = false;  // otherwise it eats key presses.
            webBrowser1.TabStop = false;

            // restrict permissions
            webBrowser1.AllowNavigation = false;

            OnOpenItem();
        }

        private void AarPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            // middle-click an attachment to remove it!
            if (e.Button == System.Windows.Forms.MouseButtons.Middle)
            {
                string attachmentName = ((sender as Control).Tag) as string;
                if (attachmentName != null)
                {
                    var newAttachArray = new List<string>(this._tweetsWorking.tweets[_curIndex].attachments);
                    var index = newAttachArray.FindIndex((s) => s.Equals(attachmentName));
                    if (index != -1)
                    {
                        newAttachArray.RemoveAt(index);
                        var cmd = new AlterAttachedImageCommand();
                        cmd._attachmentsWas = this._tweetsWorking.tweets[_curIndex].attachments;
                        cmd._tweetInfoObj = this._tweetsWorking.tweets[_curIndex];
                        cmd.Perform(newAttachArray);
                        _undoStack.Add(cmd);
                        OnOpenItem();
                    }
                }
            }
        }

        void OnOpenItem()
        {
            this.flowLayoutPanel1.Controls.Clear();
            foreach (var pb in this._arPictureBox)
            {
                pb.ImageLocation = null;
                pb.Tag = null;
            }
            foreach (var lb in this._arPictureBoxCaptions)
                lb.Text = "";

            lblCurFile.Text = "";

            if (_curIndex >= this._tweetsWorking.tweets.Count || _curIndex < 0 || this._tweetsWorking.tweets.Count == 0)
            {
                var toolpath = Path.GetDirectoryName(pathPythonScript) + "\\tools_text";
                var template = File.ReadAllText(toolpath + "\\tweet_template.html", Encoding.UTF8);
                template = template.Replace("%TEXT%", expandIntoHtml("looks done"));
                File.WriteAllText(toolpath + "\\tweet_template_out.html", template, Encoding.UTF8);
                this.webBrowser1.Url = new Uri(toolpath + "\\tweet_template_out.html");
                // this.webBrowser1.DocumentText = "<br/><br/>looks done";
                this.webBrowser1.Refresh();
            }
            else
            {
                var curtweet = this._tweetsWorking.tweets[_curIndex];
                var toolpath = Path.GetDirectoryName(pathPythonScript) + "\\tools_text";
                var template = File.ReadAllText(toolpath+"\\tweet_template.html", Encoding.UTF8 );
                template = template.Replace("%NAMERENDERED%", this._tweetsWorking.namerendered);
                template = template.Replace("%USERNAME%", this._tweetsWorking.account);
                template = template.Replace("%TEXT%", expandIntoHtml(curtweet.text));
                template = template.Replace("%DATE%", curtweet.date);
                template = template.Replace("%AVATARIMG%", _dir + "\\twpics_referenced\\" + _tweetsWorking.avatar);
                File.WriteAllText(toolpath + "\\tweet_template_out.html", template, Encoding.UTF8);
                this.webBrowser1.Url = new Uri(toolpath + "\\tweet_template_out.html");
                this.webBrowser1.Refresh();
                webBrowser1.Parent.Enabled = false;
                SetLinkLabelText(expandIntoHtml(curtweet.text));

                // show attached items
                flowLayoutPanel1.AutoScroll = true;
                flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
                flowLayoutPanel1.WrapContents = false;
                for (int i=0; i<_tweetsWorking.tweets[_curIndex].attachments.Count; i++)
                {
                    var attachpath = _tweetsWorking.tweets[_curIndex].attachments[i];
                    var try1 = _dir + "\\twpics_referenced\\" + attachpath;
                    var try2 = _dir + "\\twpics\\" + attachpath;
                    _arPictureBox[i].ImageLocation = File.Exists(try1) ? try1 : try2;
                    _arPictureBoxCaptions[i].Text = attachpath + " " + Utils.FormatFilesize(_arPictureBox[i].ImageLocation);
                    _arPictureBox[i].Tag = attachpath;
                    flowLayoutPanel1.Controls.Add(this._arPictureBoxCaptions[i]);
                    flowLayoutPanel1.Controls.Add(this._arPictureBox[i]);
                }
            }
        }

        string expandIntoHtml(string s)
        {
            TestUtil.IsTrue(!s.Contains("<") && !s.Contains(">"));
            s = s.Replace("~BR`", "<br />");
            s = s.Replace("~L0`", "<span style=\"color:rgb(0, 151, 223)\">");
            s = s.Replace("~LNK0`", "<span style=\"color:rgb(0, 151, 223)\">");
            s = s.Replace("~L1`", "</span>");
            s = s.Replace("~LNK1`", "</span>");
            return s;
        }

        private void FormFromTwitter_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (!e.Shift && !e.Control && !e.Alt)
            {
                if (e.KeyCode == Keys.Left)
                {
                    _curIndex = Math.Max(0, _curIndex - 1);
                    OnOpenItem();
                }
                else if (e.KeyCode == Keys.Right)
                {
                    _curIndex = Math.Min(this._tweetsWorking.tweets.Count - 1, _curIndex + 1);
                    _curIndex = Math.Max(0, _curIndex);
                    OnOpenItem();
                }
                else if (e.KeyCode == Keys.Home)
                {
                    _curIndex = 0;
                    OnOpenItem();
                }
                else if (e.KeyCode == Keys.End)
                {
                    _curIndex = _tweetsWorking.tweets.Count - 1;
                    _curIndex = Math.Max(0, _curIndex);
                    OnOpenItem();
                }
                else if (e.KeyCode == Keys.Delete)
                {
                    ChooseAttachments("_small.");
                    AssignLevel(1);
                }
                else if (e.KeyCode == Keys.W)
                    ToggleNsfw();
            }
            else if (!e.Shift && e.Control && !e.Alt)
            {
                if (e.KeyCode == Keys.Z)
                    UndoLastMove(true);
                else if (e.KeyCode == Keys.Y)
                    UndoLastMove(false);

            }
            else if (e.Shift && !e.Control && !e.Alt)
            {
               if (e.KeyCode == Keys.D)
                    AssignLevel(2);
                else if (e.KeyCode == Keys.G)
                    AssignLevel(3);
                else if (e.KeyCode == Keys.B)
                    ChooseAttachments("_large.");
                else if (e.KeyCode == Keys.S)
                    ChooseAttachments("_small.");
                else if (e.KeyCode == Keys.M)
                    ChooseAttachments("_medium0.");
            }
            else if (e.Shift && e.Control && !e.Alt)
            {
                if (e.KeyCode == Keys.M)
                    DownloadMediumSized();
                else if (e.KeyCode == Keys.S)
                    SaveAs();
                else if (e.KeyCode == Keys.Delete && Utils.AskToConfirm("mark all as not good? to undo you can manually move the files back"))
                    MarkAllAsNotGood();
            }
        }

        private void MarkAllAsNotGood()
        {
            for (int i=0; i<5000; i++)
            { 
                if (_tweetsWorking.tweets.Count < 1)
                {
                    break;
                }

                _curIndex = 0;
                var tweet = _tweetsWorking.tweets[_curIndex];
                ChooseAttachmentsImpl(tweet, "_small.");
                var cmd = new AssignCategoryCommand();
                cmd._source = 0;
                cmd._dest = 1;
                cmd._arr = _arrCollections;
                _undoStack.Add(cmd);
                cmd.Perform(_curIndex);
            }

            // nuke the undo stack
            _undoStack = new UndoStack<IUndoableCommand>();

            // same as hitting "home" key
            _curIndex = 0; 
            OnOpenItem();
        }

        private void ToggleNsfw()
        {
            if (_tweetsWorking.tweets[_curIndex].origname.Contains("nsfw--"))
            {
                _tweetsWorking.tweets[_curIndex].origname = _tweetsWorking.tweets[_curIndex].origname.Replace("nsfw--", "");
                MessageBox.Show("Now it's not-nsfw");
            }
            else
            {
                _tweetsWorking.tweets[_curIndex].origname = "nsfw--" + _tweetsWorking.tweets[_curIndex].origname;
                MessageBox.Show("Now it's nsfw");
            }
        }

        private void SaveAs()
        {
            for (int i=0; i< _tweetsWorking.tweets[_curIndex].attachments.Count; i++)
            {
                var fname = InputBoxForm.GetStrInput("Saving image " + (i + 1) + ": filename (without extension)?", linkLabel1.Text);
                if (!string.IsNullOrEmpty(fname))
                {
                    var src = _dir + "\\twpics_referenced\\" + _tweetsWorking.tweets[_curIndex].attachments[i];
                    var dest = _dir + "\\..\\" + fname + Path.GetExtension(src);
                    if (File.Exists(dest)) { MessageBox.Show("file already exists w that name"); }
                    else { File.Copy(src, dest, false /*allowOverwrite*/); }
                }
            }
        }

        private void DownloadMediumSized()
        {
            if (!Utils.AskToConfirm("download medium sized?")) return;
            var attachments = _tweetsWorking.tweets[_curIndex].attachments;
            var newAttachments = new List<string>(attachments);
            foreach(var shortname in attachments)
            {
                var shortnameOut = shortname.Replace("_large.", "_medium0.").Replace("_small.", "_medium0.");
                if (shortname == shortnameOut) // this *is* the medium version
                    continue;
                var fullNameIn = _dir + "\\twpics_referenced\\" + shortname;
                var fullNameOut = _dir + "\\twpics_referenced\\" + shortnameOut;
                if (File.Exists(fullNameOut)) // we already downloaded the medium version
                    continue;
                var sArgs = new string[] { "getmediumsized", fullNameIn };
                Utils.RunPythonScript(pathPythonScript, sArgs);
                if (File.Exists(fullNameOut))
                {
                    newAttachments.Add(shortnameOut);
                }
                else
                {
                    MessageBox.Show("warning, we apparently couldn't download a medium-sized version");
                }
            }
            
            if (newAttachments.Count != attachments.Count)
            {
                newAttachments.Sort();
                var cmd = new AlterAttachedImageCommand(); cmd._attachmentsWas = attachments;
                cmd._tweetInfoObj = this._tweetsWorking.tweets[_curIndex];
                cmd.Perform(newAttachments);
                _undoStack.Add(cmd);
                OnOpenItem();
            }
        }

        private bool ChooseAttachmentsImpl(TweetInfo tweet, string contains)
        {
            bool needToRefresh = false;
            var attachments = tweet.attachments;
            if (attachments.Count > 0)
            {
                var newattachments = (from shortfilename in attachments where shortfilename.Contains(contains) select shortfilename).ToList();
                var cmd = new AlterAttachedImageCommand();
                cmd._attachmentsWas = attachments;
                cmd._tweetInfoObj = this._tweetsWorking.tweets[_curIndex];
                cmd.Perform(newattachments);
                _undoStack.Add(cmd);
                needToRefresh = true;
            }
            return needToRefresh;
        }
        private void ChooseAttachments(string contains)
        {
            // e.g. you give this "small." and it will remove everything that's not marked as "small"
            bool needToRefresh = ChooseAttachmentsImpl(_tweetsWorking.tweets[_curIndex], contains);
            if (needToRefresh)
            {
                OnOpenItem();
            }
        }

        private void UndoLastMove(bool isUndo)
        {
            var cmd = _undoStack.PeekUndo();
            if (cmd == null)
            {
                MessageBox.Show("nothing to undo");
            }
            else
            {
                if (cmd.Undo())
                    _undoStack.Undo();
                OnOpenItem();
            }
        }

        private void AssignLevel(int lvl)
        {
            TestUtil.IsTrue(lvl > 0);
            var attachments = _tweetsWorking.tweets[_curIndex].attachments;
            var dict = new Dictionary<string, bool>();
            foreach(var attach in attachments)
            {
                // check for duplicate images !! no point in having both small and large of the same image !!
                var newname = attach.Replace("_large.", ".").Replace("_medium.", ".").Replace("_small.", ".");
                if (dict.ContainsKey(newname))
                {
                    MessageBox.Show("Cannot continue -- duplicate image " + newname);
                    return;
                }
                else
                    dict[newname] = true;
            }

            var cmd = new AssignCategoryCommand();
            cmd._source = 0;
            cmd._dest = lvl;
            cmd._arr = _arrCollections;
            cmd.Perform(_curIndex);
            _undoStack.Add(cmd);
            if (_curIndex >= _tweetsWorking.tweets.Count)
                FormFromTwitter_KeyUp(null, new KeyEventArgs(Keys.Left));
            OnOpenItem();
        }

        interface IUndoableCommand
        {
            bool Undo();
        }

        class AlterAttachedImageCommand : IUndoableCommand
        {
            public TweetInfo _tweetInfoObj;
            public List<string> _attachmentsWas;
            public void Perform(List<string> newAttachments)
            {
                TestUtil.IsTrue((object)_attachmentsWas != (object)newAttachments);
                _tweetInfoObj.attachments = newAttachments;
            }
            public bool Undo()
            {
                if (!Utils.AskToConfirm("undo image change?"))
                    return false;
                _tweetInfoObj.attachments = _attachmentsWas;
                return true;
            }
        }

        class AssignCategoryCommand : IUndoableCommand
        {
            public int _source; // 0 is working
            public int _dest; // 0 is working
            public TweetInfoCollection[] _arr;
            public void Perform(int curIndex)
            {
                if (_source == _dest) return;
                MoveItem(_arr, _source, _dest, curIndex, false);
            }
            public bool Undo()
            {
                if (_source == _dest) return true;
                // we know that the result has been added to the bottom of the file. move it to the top of source
                var index = _arr[_dest].tweets.Count - 1;
                if (!Utils.AskToConfirm("undo moving " + _arr[_dest].tweets[index].text + " ?"))
                    return false;
                MoveItem(_arr, _dest, _source, index, true);
                return true;
            }
            static void MoveItem(TweetInfoCollection[] arr, int source, int dest, int sourceIndex, bool destBeginOrEnd)
            {
                TweetInfo itemToMove = arr[source].tweets[sourceIndex];
                if (destBeginOrEnd)
                    arr[dest].tweets.Insert(0, itemToMove);
                else
                    arr[dest].tweets.Add(itemToMove);
                TweetInfoCollection.ToFile(arr[dest].filepath, arr[dest]);
                arr[source].tweets.RemoveAt(sourceIndex);
                TweetInfoCollection.ToFile(arr[source].filepath, arr[source]);
            }
        }

        private void FormFromTwitter_KeyDown(object sender, KeyEventArgs e)
        {
            // this successfully prevents the Enter key from being sent to linkLabel1
            if (e.KeyCode == Keys.Enter)
                e.Handled = true;
        }

        private void SetLinkLabelText(string s)
        {
            var sHrefsToAddLater = " ";
            // make sure there are spaces separating links
            s = s.Replace("<span", " <span");
            foreach (var match in Regex.Matches(s, @"href=""[^""]+"""))
                sHrefsToAddLater += " ( " + match.ToString().Replace("\"", "").Replace("href=", "") + " )";
            s = Regex.Replace(s, @"<[^>]*>", ""); // kill html tags and contents
            s = s.Replace("pic.twitter.com", "https://pic.twitter.com");
            s += sHrefsToAddLater;

            this.linkLabel1.Text = s;
            this.linkLabel1.Links.Clear();

            // first add http links
            int nLinksAdded = 0;
            List<int> starts = new List<int>();
            List<int> ends = new List<int>();

            // @"https?://[^""<>]+"
            foreach (var match in Regex.Matches(s, @"https?://\S+"))
            {
                Match match2 = (Match)match;
                linkLabel1.Links.Add(match2.Index, match2.Length, match2.Value);
                starts.Add(match2.Index);
                ends.Add(match2.Index + match2.Length);
                nLinksAdded++;
                if (nLinksAdded > 30) // max of 32 or so links :(
                    break;
            }
        }

        private void linkLabel1_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var data = e.Link.LinkData.ToString();
            if (data.StartsWith("http", StringComparison.Ordinal) && Utils.AskToConfirm("open the link? " + data))
                System.Diagnostics.Process.Start(data);
        }

        private void linkLabel1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Middle &&
                Utils.AskToConfirm("remove caption?"))
            {
                _tweetsWorking.tweets[_curIndex].text = "";
                OnOpenItem();
            }
        }

        private void GetOtherImagesForThis_NoLongerUsedAtAll(List<string> list)
        {
            if (list.Count == 0) { return; }
            List<string> listImages = new List<string>();
            listImages.Sort();
            listImages.Reverse();
            if (listImages.Count == 0) { return; }
            if (listImages.Count > _arPictureBox.Length)
            {
                Utils.MessageErr("too many images found");
                return;
            }

            flowLayoutPanel1.AutoScroll = true;
            flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanel1.WrapContents = false;
            for (int i = 0; i < listImages.Count; i++)
            {
                _arPictureBoxCaptions[i].Text = Path.GetFileName(listImages[i]);
                _arPictureBoxCaptions[i].Text += " " + Utils.FormatFilesize(listImages[i]);
                _arPictureBox[i].ImageLocation = listImages[i];

                flowLayoutPanel1.Controls.Add(_arPictureBoxCaptions[i]);
                flowLayoutPanel1.Controls.Add(_arPictureBox[i]);
            }

            if (this.flowLayoutPanel1.Controls.Count != listImages.Count * 2)
                Utils.MessageErr("expect exact number of controls, we use this metric elsewhere");
        }
    }

    public class TweetInfo
    {
        public string id="";
        public string text = "";
        public string date = "";
        public string origname = "";
        public string origorder = "";
        public string imagefilepath = "";
        public string favs = "";
        public string rts = "";
        public List<string> attachments = new List<string>();
        public static TweetInfo FromStr(string s, string imagefilepath)
        {
            var lines = s.Replace("\r\n", "\n").Split(new char[] { '\n' });
            var ret = new TweetInfo();
            bool isInT = false;
            foreach (var line in lines)
            {
                if (isInT)
                {
                    ret.text += line + "\n";
                }
                else
                {
                    if (line.StartsWith("id="))
                        ret.id = line.Substring("id=".Length);
                    else if (line.StartsWith("date="))
                        ret.date = line.Substring("date=".Length);
                    else if (line.StartsWith("origorder="))
                        ret.origorder = line.Substring("origorder=".Length);
                    else if (line.StartsWith("favs="))
                        ret.favs = line.Substring("favs=".Length);
                    else if (line.StartsWith("rts="))
                        ret.rts = line.Substring("rts=".Length);
                    else if (line.StartsWith("a="))
                        ret.attachments = line.Substring("a=".Length).Split(new char[] { '|' }).ToList();
                    else if (line.TrimStart().StartsWith("origname="))
                        ret.origname = line.TrimStart().Substring("origname=".Length);
                    else if (line.StartsWith("t="))
                        isInT = true;
                    else if (line.Trim().Length > 0)
                        throw new Exception("unknown line " + line);
                }
            }
            TestUtil.IsTrue(ret.id.Length > 0);
            TestUtil.IsTrue(ret.text.Length > 0);
            ret.imagefilepath = imagefilepath;
            turnPicTwitterIntoAttachments(ret);
            return ret;
        }

        static void turnPicTwitterIntoAttachments(TweetInfo obj)
        {
            var s = obj.text;
            s = Regex.Replace(s, "~LNK0`pic.twitter.com/([^ ~]+)~LNK1`", (match) => { return TweetInfo.turnPicTwitterIntoAttachmentsMatch(match, obj); }
            );

            obj.text = s;
        }

        static string turnPicTwitterIntoAttachmentsMatch(Match match, TweetInfo obj)
        {
            var twitterPicId = match.Groups[1];
            var dir = Path.GetDirectoryName(obj.imagefilepath);
            if (!Directory.Exists(dir))
            {
                MessageBox.Show("could not find parent directory, expected at " + dir);
                TestUtil.IsTrue(false);
            }
            TestUtil.IsTrue(twitterPicId.Length>5);

            var found = "";
            for (int i = 0; i < 4; i++)
            {
                var picsdir = dir + "\\" + (i%2==0 ? "twpics" : "twpics_referenced");
                var onlySearchSmall = i <= 1; // first only check "small" ones, then check everything
                if (!Directory.Exists(picsdir))
                {
                    MessageBox.Show("could not find pics directory, expected at " + picsdir);
                    TestUtil.IsTrue(false);
                }
                foreach (var s in Directory.EnumerateFiles(picsdir, "*", SearchOption.AllDirectories))
                {
                    var sshort = Path.GetFileName(s);
                    if (onlySearchSmall && !sshort.Contains("_small")) continue;
                    if (sshort.Contains("_" + twitterPicId + "_"))
                    {
                        found = sshort;
                        break;
                    }
                }
                if (found != "")
                {
                    break;
                }
            }
            if (!string.IsNullOrEmpty(found))
            {
                obj.attachments.Add(found);
                return "";
            }
            else
            {
                return "{" + match.Groups[0] + " not found}";
            }
        }

        public string ToStr()
        {
            string s = "";
            s += "\nid=" + id;
            if (this.origname.Length > 0)
                s += "\n																																												origname=" + origname;
            if (this.origorder.Length > 0)
                s += "\norigorder=" + origorder;
            if (this.date.Length > 0)
                s += "\ndate=" + date;
            if (this.favs.Length > 0)
                s += "\nfavs=" + favs;
            if (this.rts.Length > 0)
                s += "\nrts=" + rts;
            if (this.attachments.Count > 0)
                s += "\na=" + string.Join("|", attachments);
            s += "\nt=\n" + text;
            return s;
        }
    }
    public class TweetInfoCollection
    {
        public string account = "";
        public string namerendered = "";
        public string datecaptured = "";
        public string bio = "";
        public string avatar = "";
        public string filepath = "";
        public List<TweetInfo> tweets = new List<TweetInfo>();
        public static TweetInfoCollection FromFile(string path)
        {
            var txt = File.ReadAllText(path, Encoding.UTF8);
            txt = txt.Replace("\r\n", "\n");
            var ret = new TweetInfoCollection();
            ret.filepath = path;
            var parts = Utils.SplitByString(txt, "\n==================");
            foreach (var line in parts[0].Split(new char[] { '\n' }))
            {
                if (line.StartsWith("account="))
                    ret.account = line.Substring("account=".Length);
                else if (line.StartsWith("namerendered="))
                    ret.namerendered = line.Substring("namerendered=".Length);
                else if (line.StartsWith("datecaptured="))
                    ret.datecaptured = line.Substring("datecaptured=".Length);
                else if (line.StartsWith("bio="))
                    ret.bio = line.Substring("bio=".Length);
                else if (line.StartsWith("avatar="))
                    ret.avatar = line.Substring("avatar=".Length);
                else if (line.Trim().Length > 0)
                    throw new Exception("unknown line " + line);
            }
            TestUtil.IsTrue(ret.account.Length > 0);
            TestUtil.IsTrue(ret.namerendered.Length > 0);
            parts[0] = "";
            foreach (var part in parts)
            {
                if (part.Trim().Length > 0)
                    ret.tweets.Add(TweetInfo.FromStr(part, path));
            }
            return ret;
        }
        public static void ToFile(string path, TweetInfoCollection obj)
        {
            var sb = new StringBuilder();
            sb.Append("\naccount=" + obj.account);
            sb.Append("\nnamerendered=" + obj.namerendered);
            sb.Append("\ndatecaptured=" + obj.datecaptured);
            sb.Append("\nbio=" + obj.bio);
            sb.Append("\navatar=" + obj.avatar);
            sb.Append("\n\n");
            foreach (var item in obj.tweets)
            {
                sb.Append("==================");
                sb.Append(item.ToStr());
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
    }
}
