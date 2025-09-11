// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

#pragma warning disable SA1513
#pragma warning disable SA1201 //method should not follow class
#pragma warning disable SA1034 //do not nest type

namespace labs_coordinate_pictures
{
    // note: contains GetNextDirectory() and MoveDirToStaging()
    public partial class FormPersonalMusic : Form
    {
        string _genre = "";
        string _dir = null;
        string _staging = null;
        string _songsToMoveToStaging = "";
        string _probableArtistAlbum = "";

        public FormPersonalMusic()
        {
            InitializeComponent();
            AddGenreMenuItems();
            _staging = Configs.Current.Get(ConfigKey.FilepathSortMusicToLibraryStagingDir);
            if (string.IsNullOrWhiteSpace(_staging) || !Directory.Exists(_staging))
            {
                _staging = InputBoxForm.GetStrInput("Enter staging directory:", @"C:\data\data_3\getmusic\process\stage_wavcut", mustBeDirectory: true);
                if (string.IsNullOrEmpty(_staging) || !Directory.Exists(_staging))
                {
                    MessageBox.Show("does not exist");
                    Environment.Exit(1);
                    return;
                }

                Configs.Current.Set(ConfigKey.FilepathSortMusicToLibraryStagingDir, _staging);
            }

            mnuRefresh_Click(null, null);
        }

        private void mnuRefresh_Click(object sender, EventArgs e)
        {
            _songsToMoveToStaging = "";
            if (_dir == null)
            {
                return;
            }
            else
            {
                string sFirst = null;
                string sFirstAlbum = null;
                List<string> songs = new List<string>();
                foreach (var s in Directory.EnumerateFiles(_dir, "*.m4a", SearchOption.TopDirectoryOnly))
                {
                    if (sFirst == null)
                    {
                        sFirst = s;
                        sFirstAlbum = albumArtistTrackTitleFromFilename(s)[0];
                        songs.Add(s);
                    }
                    else if (sFirstAlbum == albumArtistTrackTitleFromFilename(s)[0])
                    {
                        songs.Add(s);
                    }
                    else
                    {
                        break;
                    }
                }

                if (sFirst == null)
                {
                    _songsToMoveToStaging = "(Done)";
                    return;
                }

                _songsToMoveToStaging = string.Join("\r\n", songs);
            }
        }

        void AddGenreMenuItems()
        {
            var root = @"c:\music";
            var dictTaken = new Dictionary<char, bool>();
            foreach (var subdir1 in Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly))
            {
                if (subdir1.ToUpperInvariant() == @"C:\MUSIC\LIB")
                    continue;

                var nameparts = Path.GetFileName(subdir1).Split(new char[] { ' ' });
                int nWhich = -1;
                for (int i = nameparts.Length - 1; i >= 0; i--)
                {
                    if (!dictTaken.ContainsKey(nameparts[i][0]))
                    {
                        dictTaken[nameparts[i][0]] = true;
                        nWhich = i;
                        break;
                    }
                }
                if (nWhich >= 0)
                    nameparts[nWhich] = "&" + nameparts[nWhich];

                var newMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                newMenuItem.Tag = Path.GetFileName(subdir1);
                newMenuItem.Size = new System.Drawing.Size(203, 22);
                newMenuItem.Text = string.Join(" ", nameparts);
                newMenuItem.Click += genreMenuItem_Click;
                genresToolStripMenuItem.DropDownItems.Add(newMenuItem);
            }
        }

        private void genreMenuItem_Click(object sender, EventArgs e)
        {
            if (sender == null || (sender as ToolStripMenuItem == null) || string.IsNullOrEmpty((sender as ToolStripMenuItem).Tag as string))
            {
                lblGenre.Text = "Try all";
                _genre = "";
                return;
            }

            lblGenre.Text = _genre;
            _genre = (sender as ToolStripMenuItem).Tag as string;
        }

        public static void AddToLog(string s)
        {
            string strLogFile = AppDomain.CurrentDomain.BaseDirectory + "\\log.txt";
            File.AppendAllText(strLogFile, s + "\r\n");
        }
        private static string[] albumArtistTrackTitleFromFilename(string s)
        {
            s = Path.GetFileName(s);
            var parts = s.Split(new char[] { '$' });
            if (parts.Length != 5)
            {
                Utils.MessageErr("error for file " + s + " incorrect number of $");
                return new string[] { "", "", "", "", "" };
            }
            parts[0] = parts[0].Split(new char[] { ' ' }, 2)[1];
            parts[4] = "spotify:track:" + parts[4].Split(new char[] { '.' })[0];
            return parts;
        }

        private void mnuGenreTryAll_Click(object sender, EventArgs e)
        {
            (sender as ToolStripMenuItem).Tag = "";
            genreMenuItem_Click(sender, e);
        }

        public static bool looksLikeFile(string s)
        {
            var slower = s.ToUpperInvariant();
            return slower.EndsWith(".M4A", StringComparison.Ordinal) ||
                slower.EndsWith(".MP3", StringComparison.Ordinal) ||
                slower.EndsWith(".FLAC", StringComparison.Ordinal)
                || slower.EndsWith(".URL", StringComparison.Ordinal);
        }

        private void moveToStagingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_dir == null)
                return;

            if (_songsToMoveToStaging.StartsWith("(", StringComparison.Ordinal) ||
                string.IsNullOrEmpty(_songsToMoveToStaging))
            {
                Utils.MessageBox("No files to move");
                return;
            }

            var songs = _songsToMoveToStaging.Replace("\r\n", "\n").Split(new char[] { '\n' });
            foreach (var song in songs)
            {
                var newPath = _staging + "\\" + Path.GetFileName(song);
                File.Move(song, newPath);
            }

            int nSongs = Directory.EnumerateFiles(_staging, "*.m4a", SearchOption.TopDirectoryOnly).Count();
            mnuRefresh_Click(null, null);

            // autosearch based on artist
            searchArtistToolStripMenuItem_Click(null, null);

            // if there's just one, auto-set to artist - title, user can undo if needed.
            MakeFakeDir();
            _probableArtistAlbum = GetProbableArtistAlbum(_staging);
            if (nSongs == 1)
            {
                mnuRename2_Click(null, null);
            }
            else
            {
                mnuRename3_Click(null, null);
            }
        }

        static string GetProbableArtistAlbum(string directory)
        {
            var songfound = Directory.EnumerateFiles(directory, "*.m4a", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (songfound == null)
            {
                return "";
            }
            else
            {
                var parts = Path.GetFileName(songfound).Split(new char[] { '$' });
                var albumpts = parts[0].Split(new char[] { ' ' }, 2, StringSplitOptions.None);
                var album = albumpts.Length > 1 ? albumpts[1] : albumpts[0];
                return parts[1] + " - " + album;
            }
        }

        private void MakeFakeDir()
        {
            var ddir = @"C:\data\data_3\getmusic\process\stage_dirs";
            if (!Directory.Exists(ddir))
                return;
            foreach (var dir in Directory.EnumerateDirectories(ddir))
                Directory.Delete(dir);

            var first = getFirstFilename();
            if (first == null)
            {
                Utils.MessageBox("no files");
                return;
            }
            var parts = albumArtistTrackTitleFromFilename(first);
            var fakename = "0000, " + parts[0];
            Directory.CreateDirectory(ddir + "\\" + fakename);
        }

        List<string> _lastRenames = new List<string>();
        void RenameHelper(Func<string[], string> fn)
        {
            if (_dir == null)
                return;
            _lastRenames = null;
            Dictionary<string, bool> names = new Dictionary<string, bool>();
            foreach (var s in Directory.EnumerateFiles(_staging, "*.m4a", SearchOption.TopDirectoryOnly))
            {
                names[Path.GetFileName(s).ToLowerInvariant()] = true;
            }

            List<string> lastRenames = new List<string>();
            foreach (var s in Directory.EnumerateFiles(_staging, "*.m4a", SearchOption.TopDirectoryOnly))
            {
                var parts = albumArtistTrackTitleFromFilename(s);
                var newname = fn(parts);
                if (names.ContainsKey(newname.ToLowerInvariant()))
                {
                    Utils.MessageErr("Name conflict: " + newname);
                    return;
                }
                names[newname.ToLowerInvariant()] = true;
                lastRenames.Add(s + "|" + _staging + "\\" + newname);
            }

            foreach (var s in lastRenames)
            {
                var parts = s.Split(new char[] { '|' }, 2);
                AddToLog("moving " + parts[0] + " to " + parts[1]);
                File.Move(parts[0], parts[1]);
            }

            _lastRenames = lastRenames;
        }

        private void undoRenameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_dir == null)
                return;
            if (Utils.AskToConfirm("Undo, doing the following:\r\n" + string.Join("\r\n", _lastRenames)))
            {
                foreach (var s in _lastRenames)
                {
                    var parts = s.Split(new char[] { '|' }, 2);
                    AddToLog("moving " + parts[1] + " back to " + parts[0]);
                    File.Move(parts[1], parts[0]);
                }

                _lastRenames = null;
            }
        }

        private void mnuRename4_Click(object sender, EventArgs e)
        {
            RenameHelper((parts) => parts[3] + ".m4a");
        }

        private void mnuRename3_Click(object sender, EventArgs e)
        {
            RenameHelper((parts) => parts[2] + " " + parts[3] + ".m4a");
        }

        private void mnuRename2_Click(object sender, EventArgs e)
        {
            RenameHelper((parts) => parts[1] + " - " + parts[3] + ".m4a");
        }

        private void mnuRenameShift2_Click(object sender, EventArgs e)
        {
            RenameHelper((parts) => parts[2] + " " + parts[1] + " - " + parts[3] + ".m4a");
        }

        private void newAlbumDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_dir == null)
                return;
            List<string> lastRenames = new List<string>();
            foreach (var s in Directory.EnumerateFiles(_staging, "*.m4a", SearchOption.TopDirectoryOnly))
            {
                var parts = albumArtistTrackTitleFromFilename(s);
                var newdir = _staging + "\\9999, " + parts[0];
                Directory.CreateDirectory(newdir);

                var newname = newdir + "\\" + Path.GetFileName(s);
                lastRenames.Add(s + "|" + newname);
            }

            foreach (var s in lastRenames)
            {
                var parts = s.Split(new char[] { '|' }, 2);
                File.Move(parts[0], parts[1]);
            }

            _lastRenames = lastRenames;
        }

        string _lastOpened = null;
        private void opencreateArtistDirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.MessageBox("not implemented");
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null && !string.IsNullOrEmpty(listBox1.SelectedItem.ToString()))
            {
                var str = listBox1.SelectedItem.ToString();
                if (str.StartsWith("(", StringComparison.Ordinal))
                    return;
                if (!Directory.Exists(str) && !File.Exists(str))
                    return;
                if (looksLikeFile(str))
                {
                    _lastOpened = Path.GetDirectoryName(str);
                    Utils.OpenDirInExplorer(Path.GetDirectoryName(str));

                    // Process.Start("\"" + str + "\"");
                }
                else
                {
                    _lastOpened = str;
                    Utils.OpenDirInExplorer(str);
                }
            }
        }

        private void openGoodToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_genre))
            {
                Utils.MessageBox("no genre chosen");
                return;
            }

            _lastOpened = "c:\\music\\" + _genre + "\\( ) Good";
            Utils.OpenDirInExplorer(_lastOpened);
        }

        private void openLessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_genre))
            {
                Utils.MessageBox("no genre chosen");
                return;
            }

            _lastOpened = "c:\\music\\" + _genre + "\\( ) Less";
            Utils.OpenDirInExplorer(_lastOpened);
        }

        private void moveIntoOpenedDirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.MessageBox("not implemented");
        }

        SearchEngineClass _search = new SearchEngineClass(@"C:\b\pydev\trees\music.txt");

        private void GoQuery(string sQuery, bool fRegex)
        {
            var inst = _search.GetInstance();
            var listDirSameGenre = new List<string>();
            var listFileSameGenre = new List<string>();
            var listDirDiffGenre = new List<string>();
            var listFileDiffGenre = new List<string>();
            var matches = new List<string>();
            var genreLower = string.IsNullOrEmpty(_genre) ? "" : _genre.ToLowerInvariant();
            genreLower = "c:\\music\\" + genreLower;
            int nHits = 0, nMaxHits = 2000;
            if (fRegex)
            {
                var reg = new Regex(sQuery);
                foreach (var s in inst)
                {
                    if (reg.IsMatch(s))
                    {
                        if (nHits++ >= nMaxHits)
                            break;
                        matches.Add(inst.GetFullPath());
                    }
                }
            }
            else
            {
                var sQueryLower = sQuery.ToLowerInvariant();
                foreach (var s in inst)
                {
                    if (s.ToLowerInvariant().Contains(sQueryLower))
                    {
                        if (nHits++ >= nMaxHits)
                            break;
                        matches.Add(inst.GetFullPath());
                    }
                }
            }

            foreach (var fullPath in matches)
            {
                if (looksLikeFile(fullPath))
                {
                    if (fullPath.ToLowerInvariant().StartsWith(genreLower, StringComparison.Ordinal))
                        listFileSameGenre.Add(fullPath);
                    else
                        listFileDiffGenre.Add(fullPath);
                }
                else
                {
                    if (fullPath.ToLowerInvariant().StartsWith(genreLower, StringComparison.Ordinal))
                        listDirSameGenre.Add(fullPath);
                    else
                        listDirDiffGenre.Add(fullPath);
                }
            }

            listBox1.BeginUpdate();
            listBox1.Items.Clear();
            listBox1.Items.Add("(Dirs SameGenre)");
            foreach (var s in listDirSameGenre)
                listBox1.Items.Add(s);
            listBox1.Items.Add("(Files SameGenre)");
            foreach (var s in listFileSameGenre)
                listBox1.Items.Add(s);
            listBox1.Items.Add("(Dirs DiffGenre)");
            foreach (var s in listDirDiffGenre)
                listBox1.Items.Add(s);
            listBox1.Items.Add("(Files DiffGenre)");
            foreach (var s in listFileDiffGenre)
                listBox1.Items.Add(s);
            if (nHits >= nMaxHits)
                listBox1.Items.Add("(Results truncated)");
            listBox1.EndUpdate();
        }

        private void mnuSearch_Click(object sender, EventArgs e)
        {
            var sQuery = InputBoxForm.GetStrInput("Search for string in music library:");
            if (!string.IsNullOrEmpty(sQuery))
                GoQuery(sQuery, false);
        }

        private void searchRegexToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var sQuery = InputBoxForm.GetStrInput("Search for Regex string in music library:");
            if (!string.IsNullOrEmpty(sQuery))
                GoQuery(sQuery, true);
        }

        string getFirstFilename()
        {
            string first = null;
            foreach (var s in Directory.EnumerateFiles(_staging, "*.m4a", SearchOption.TopDirectoryOnly))
            {
                first = s;
                break;
            }

            return first;
        }

        private void searchThisPartOfFilename(int nWhichPart)
        {
            _lastOpened = null;
            var first = getFirstFilename();
            if (first == null)
            {
                Utils.MessageBox("no files");
                return;
            }
            var parts = albumArtistTrackTitleFromFilename(first);
            GoQuery(parts[nWhichPart], false /*regex*/);
        }

        private void searchArtistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            searchThisPartOfFilename(1);
        }

        private void searchAlbumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            searchThisPartOfFilename(0);
        }

        List<string> _dirListingUnindexed = null;
        private void searchWithoutIndexToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var sQuery = InputBoxForm.GetStrInput("Search for string in music library:");
            if (string.IsNullOrEmpty(sQuery))
                return;
            if (_dirListingUnindexed == null)
            {
                _dirListingUnindexed = new List<string>();
                foreach (var s in Directory.EnumerateDirectories(@"c:\music", "*", SearchOption.AllDirectories))
                {
                    var sLower = s.ToLowerInvariant();
                    if (!sLower.StartsWith("c:\\music\\lib\\", StringComparison.Ordinal))
                        _dirListingUnindexed.Add(sLower);
                }
            }

            sQuery = sQuery.ToLowerInvariant();
            int nHits = 0, nMaxHits = 2000;
            var matches = new List<string>();
            foreach (var s in _dirListingUnindexed)
            {
                if (s.Contains(sQuery))
                {
                    matches.Add(s);
                    if (nHits++ >= nMaxHits)
                        break;
                }
            }

            listBox1.BeginUpdate();
            listBox1.Items.Clear();
            listBox1.Items.Add("(Dirs)");
            foreach (var s in matches)
                listBox1.Items.Add(s);
            if (nHits >= nMaxHits)
                listBox1.Items.Add("(Results truncated)");
            listBox1.EndUpdate();
        }

        private void copySongArtistTitleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var first = getFirstFilename();
            if (first == null)
            {
                Utils.MessageBox("no files");
                return;
            }
            if (first.Contains("$"))
            {
                var parts = albumArtistTrackTitleFromFilename(first);
                Clipboard.SetText(parts[1] + " - " + parts[3]);
            }
            else
            {
                Clipboard.SetText(Path.GetFileNameWithoutExtension(first));
            }
        }

        private void lookForAllUrlDirsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var root = @"C:\music";
            var res = new List<string>();
            foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
            {
                bool sawDir = false;

                // skip over a directory that has a subdirectory
                if (Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly).FirstOrDefault() != null)
                {
                    sawDir = true;
                    break;
                }
                if (!sawDir)
                {
                    var tempres = new List<string>();
                    int nCountUrl = 0;
                    foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                    {
                        tempres.Add(file.EndsWith(".url", StringComparison.Ordinal) ? file : "()" + file);
                        if (file.EndsWith(".url", StringComparison.Ordinal))
                            nCountUrl++;
                    }

                    if (nCountUrl > 5)
                    {
                        res.Add("  ");
                        res.Add("  ");
                        res.Add("  ");
                        foreach (var s in tempres)
                            res.Add(s);
                    }
                }
            }

            listBox1.BeginUpdate();
            listBox1.Items.Clear();
            foreach (var s in res)
                listBox1.Items.Add(s);
            listBox1.EndUpdate();
        }

        public static bool IsAudio(string s)
        {
            var exts = new string[] { ".m4a", ".mp4", ".flac", ".wav", ".mp3", ".ogg", ".wma", ".aac", ".wma" };
            return Array.IndexOf(exts, "." + Path.GetExtension(s)) != -1;
        }

        public static string GetNextDirectory()
        {
            var startdir = @"C:\data\local\getmusic\process_processmock\ready1";
            if (!Directory.Exists(startdir))
                return null;
            foreach (var dir in Directory.EnumerateDirectories(startdir, "*", SearchOption.AllDirectories))
            {
                foreach (var fl in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    if (IsAudio(fl))
                        return dir;
                }
            }

            return null;
        }

        public static void MoveDirToStaging()
        {
            var startdir = @"C:\data\local\getmusic\process_processmock\ready1";
            string dest = null;
            if (!Directory.Exists(startdir))
                return;
            foreach (var dir in Directory.EnumerateDirectories(startdir, "*", SearchOption.AllDirectories))
            {
                bool isAppropriate = true;
                bool sawAtLeastOne = false;
                foreach (var fl in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    if (IsAudio(fl))
                    {
                        sawAtLeastOne = true;
                        if (!fl.Contains(FilenameUtils.MarkerString))
                        {
                            isAppropriate = false;
                            break;
                        }
                    }
                    else if (!Directory.Exists(fl))
                    {
                        if (Utils.AskToConfirm("delete file with bad extension " + fl + "?"))
                        {
                            Utils.SoftDelete(fl);
                        }

                        if (File.Exists(fl))
                            isAppropriate = false;
                    }
                }

                if (sawAtLeastOne && isAppropriate)
                {
                    if (Directory.EnumerateDirectories(dir).Count() > 0)
                    {
                        Utils.MessageErr("Can't move the dir " + dir + " since it has a subdir?");
                        continue;
                    }

                    if (dest == null)
                    {
                        dest = InputBoxForm.GetStrInput("Enter dest directory:", @"C:\data\local\getmusic\process_processmock\ready2");
                        if (!string.IsNullOrEmpty(dest) && Directory.Exists(dest))
                            return;
                    }
                    if (!dir.StartsWith(startdir, StringComparison.Ordinal))
                    {
                        Utils.MessageErr("dir " + dir + " doesn't start with " + startdir + "??");
                        continue;
                    }
                    var partafterdir = dir.Substring(startdir.Length);
                    var newdir = dest + partafterdir;
                    if (!Directory.Exists(newdir))
                    {
                        Utils.MessageErr("dir " + newdir + " already exists");
                        continue;
                    }
                    if (Utils.AskToConfirm("Looks like the directory " + dir + " contains nothing but marked mp3s, move it to " + newdir + "?"))
                    {
                        try
                        {
                            // make the parent dir
                            if (!Directory.Exists(Path.GetDirectoryName(newdir)))
                                Directory.CreateDirectory(Path.GetDirectoryName(newdir));
                            Directory.Move(dir, newdir);
                        }
                        catch
                        {
                            Utils.MessageErr("Could not move directory " + dir);
                        }
                    }
                }
            }
        }

        private void searchArtist_Click(object sender, EventArgs e)
        {
            var q = Utils.SplitByString(_probableArtistAlbum, " - ")[0];
            Utils.LaunchUrl("http://google.com/search?q=" + q);
        }

        private void searchAlbum_Click(object sender, EventArgs e)
        {
            Utils.LaunchUrl("http://google.com/search?q=" + _probableArtistAlbum);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            var dir = InputBoxForm.GetStrInput("Open what directory?", @"C:\data\l5\getmusic_wavs\a", mustBeDirectory: true);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                return;
            }

            // check that id3 tags are set
            var sArgs = new string[] { "checkformetadatatags", dir };
            var sPyscript = Configs.Current.Get(ConfigKey.FilepathCoordMusicMainPy);
            string sStdErrFromScript = Utils.RunPythonScript(sPyscript, sArgs, warnIfStdErr: false);
            if (!Utils.AskToConfirm("checking for id3 tags, script says " +
                Utils.FormatPythonError(sStdErrFromScript) + ". continue?"))
                return;

            _dir = dir;
            mnuRefresh_Click(null, null);
            MessageBox.Show("Now, we'll open the staging directory in explorer.");
            Utils.OpenDirInExplorer(_staging);
            MessageBox.Show("Now, hit Move (Ctrl+M) to move the first files.\r\n\r\nThen, for example, to rename to titles only, hit Ctrl+Z, Ctrl+4.");
        }

        public static void OnDragDropFile(string path)
        {
            if (path.ToLowerInvariant().EndsWith(".url", StringComparison.Ordinal))
            {
                Utils.Run(path, null, hideWindow: true, waitForExit: false, shellExecute: true);
                return;
            }

            if (FilenameUtils.LooksLikeImage(path))
            {
                var scr = @"C:\b\pydev\devhiatus\pytools_pictures\openwebp\open-webp.py";
                Utils.RunPythonScriptOnSeparateThread(scr,
                     new string[] { path }, createWindow: true, autoWorkingDir: true);
                return;
            }

            if (FilenameUtils.LooksLikeAudio(path))
            {
                var scr = Configs.Current.Get(ConfigKey.FilepathCoordMusicMainPy);

                if (!File.Exists(scr))
                {
                    Utils.MessageErr("could not find " + scr + ".locate it by " +
                        "choosing from the menu Options->Set coordmusic location...");
                }
                else
                {
                    Utils.RunPythonScriptOnSeparateThread(scr,
                        new string[] { "startspotify", path }, createWindow: true, autoWorkingDir: true);
                }

                return;
            }
        }

        public static void OnDragDropFiles(string[] paths)
        {
            if (paths != null)
            {
                foreach (var f in paths)
                {
                    OnDragDropFile(f);
                }
            }
        }
    }

    // Generate an index with the dos command 'tree c:\foo /f /a > mytree.txt'
    // There is apparently a bug where files ending in ... are not represented correctly, and unicode doesn't work too.
    public sealed class SearchEngineClass
    {
        string[] _dirListingAllLines = null;
        int[] _dirListingAllLinesLevels = null;
        string _indexFile;
        public SearchEngineClass(string indexFile)
        {
            _indexFile = indexFile;
        }
        public SearchEngineInstance GetInstance(bool verify = true)
        {
            if (_dirListingAllLines == null)
            {
                if (!File.Exists(_indexFile))
                {
                    Utils.MessageErr("Could not find index at " + _indexFile);
                    return null;
                }

                MessageBox.Show("Creating index... this might take a few seconds.");
                var listOut = new List<string>();
                var levelsOut = new List<int>();
                var regBefore = new Regex(@"^((    *)|[|]|(\+\-\-\-)|(\\\-\-\-))*");
                foreach (var line in File.ReadAllLines(_indexFile))
                {
                    var parts = regBefore.Match(line);
                    var matchLength = parts.ToString().Length;
                    if (matchLength % 4 != 0)
                    {
                        Utils.MessageErr("match length not %4 on line " + line);
                        return null;
                    }
                    var contentString = line.Substring(matchLength);
                    if (contentString.Length > 0)
                    {
                        listOut.Add(contentString.ToLowerInvariant());
                        levelsOut.Add(matchLength / 4);
                    }
                }
                _dirListingAllLines = listOut.ToArray();
                _dirListingAllLinesLevels = levelsOut.ToArray();
                if (verify)
                    TestSearchEngineClass(_indexFile);
            }

            return new SearchEngineInstance(this);
        }

        public sealed class SearchEngineInstance : IEnumerable<string>
        {
            string[] _pathParts = new string[] { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
            int _curLevel = 0;
            SearchEngineClass _outer;
            public SearchEngineInstance(SearchEngineClass outer)
            {
                _outer = outer;
            }
            public IEnumerator<string> GetEnumerator()
            {
                for (int i = 0; i < _outer._dirListingAllLines.Length; i++)
                {
                    var nextLevel = _outer._dirListingAllLinesLevels[i];
                    _pathParts[nextLevel] = _outer._dirListingAllLines[i];
                    if (nextLevel < _curLevel)
                    {
                        for (int j = nextLevel + 1; j < _pathParts.Length; j++)
                            _pathParts[j] = "";
                    }
                    _curLevel = nextLevel;
                    if (_pathParts[1] != "lib" && _curLevel > 0)
                        yield return _outer._dirListingAllLines[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public string GetFullPath()
            {
                var fullPath = "";
                for (int j = 0; j < _curLevel; j++)
                    fullPath += _pathParts[j] + "\\";
                fullPath += _pathParts[_curLevel];
                return fullPath;
            }
        }

        public static bool TreeIndexIsOkForThisString(string fullPath)
        {
            if (fullPath.Contains(".."))
                return false;
            bool isAllAnsi = true;
            for (int ii = 0; ii < fullPath.Length; ii++)
            {
                if (fullPath[ii] > 128 || fullPath[ii] == '?' || fullPath[ii] == '"' || fullPath[ii] == '\'')
                {
                    isAllAnsi = false;
                    break;
                }
            }

            return isAllAnsi;
        }
        static void TestSearchEngineClass(string indexname)
        {
#if DEBUG
            var engine = new SearchEngineClass(indexname);
            var instance = engine.GetInstance(false);
            var problems = new List<string>();
            bool fSeenComptine = false;
            int nCountSeen = 0;
            foreach (var s in instance)
            {
                System.Diagnostics.Debug.WriteLineIf(s == "unlikely", "unlikely");
                nCountSeen++;
                var fullPath = instance.GetFullPath();
                TestUtil.IsTrue(!fullPath.StartsWith("c:\\music\\lib", StringComparison.Ordinal));

                if (TreeIndexIsOkForThisString(fullPath) &&
                        fullPath != "c:\\music\\00s hip hop\\shabazz palaces\\2011, black up\\03 are you. can you. were you (felt).m4a" &&
                        fullPath != "c:\\music\\00s hip hop\\shabazz palaces\\2014, lese majesty\\10 .down 155th in the mcm snorkel.m4a" &&
                        fullPath != "c:\\music\\00s indie electronic\\baths\\2010, cerulean\\04 .m4a" &&
                        !fullPath.Contains("c:\\music\\00s indie electronic\\justice\\2007, cross +") &&
                        !fullPath.Contains("c:\\music\\brazil\\") &&
                        !fullPath.Contains("schoenberg"))
                {
                    if (labs_coordinate_pictures.FormPersonalMusic.looksLikeFile(fullPath) && !File.Exists(fullPath))
                        problems.Add(fullPath);
                    else if (!Directory.Exists(fullPath) && !File.Exists(fullPath))
                        problems.Add(fullPath);
                }

                if (fullPath.Contains("comptine"))
                    fSeenComptine = true;
            }

            TestUtil.IsTrue(nCountSeen > 10 * 1000);

            // Debug.Assert(problems.Count < 30);
            TestUtil.IsTrue(fSeenComptine);
#endif
        }
    }

    public static partial class Utils
    {
        public static string OverrideGetSoftDeleteDir(string s)
        {
            var etrash = "E:\\working\\trash";
            var itrash = "I:\\working\\trash";
            var dtrash = @"D:\local\less_important\trash";
            var ctrash = @"C:\data\e5\less_important\trash";
            if (!Directory.Exists(itrash))
                return ctrash;
            s = s.Replace("/", "\\");
            if (s.ToLower().StartsWith("i:\\", StringComparison.InvariantCulture))
                return itrash;
            else if (s.ToLower().StartsWith("e:\\", StringComparison.InvariantCulture))
                return etrash;
            else if (s.ToLower().StartsWith("d:\\", StringComparison.InvariantCulture))
                return dtrash;
            else
                return ctrash;

            // if (s.ToLower().StartsWith("c:\\b\\"))
            //     return itrash;
            // if (s.ToLower().StartsWith("c:\\data\\data_1\\"))
            //     return itrash;
            // if (s.ToLower().StartsWith("c:\\data\\data_1_pieces\\"))
            //     return itrash;
            // if (s.ToLower().StartsWith("c:\\data\\data_2\\"))
            //     return itrash;
            // if (s.ToLower().StartsWith("c:\\music\\"))
            //     return itrash;
            // if (s.ToLower().StartsWith("c:\\users\\mf\\documents\\fisherapps\\"))
            //     return itrash;a
            // else
            //     return ctrash;
        }
    }

#pragma warning restore SA1513
#pragma warning restore SA1201
#pragma warning restore SA1034 //do not nest type



}
