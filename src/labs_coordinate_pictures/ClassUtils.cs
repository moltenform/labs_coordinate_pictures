using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public static class Utils
    {
        static Random rand = new Random();
        public static void OpenDirInExplorer(string sDir)
        {
            if (!Directory.Exists(sDir))
            {
                sDir = ".";
            }

            RunExeWithArguments("explorer", new string[] { sDir },
                createWindow: true, waitForExit: false, shellEx: true);
        }

        public static string RunExeWithArguments(string exe, string[] args, bool createWindow, bool waitForExit, bool shellEx)
        {
            string sErr = "";
            var startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = !createWindow;
            startInfo.UseShellExecute = shellEx;
            startInfo.FileName = exe;
            startInfo.Arguments = CombineProcessArguments(args);
            if (!shellEx)
            {
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
            }

            var process = Process.Start(startInfo);
            if (!shellEx)
            {
                process.ErrorDataReceived += (sender, errordataargs) => sErr += errordataargs.Data;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            if (waitForExit)
            {
                process.WaitForExit();
            }

            return sErr;
        }

        public static bool AskToConfirm(string s)
        {
            var res = MessageBox.Show(s, "", MessageBoxButtons.YesNo);
            return res == DialogResult.Yes;
        }

        public static string FirstTwoChars(string s)
        {
            return s.Substring(0, Math.Min(2, s.Length));
        }

        public static void SoftDelete(string s)
        {
            var trashdir = Configs.Current.Get(ConfigsPersistedKeys.FilepathTrash);
            if (String.IsNullOrEmpty(trashdir) || 
                !Directory.Exists(trashdir))
            {
                MessageBox.Show("Trash directory not found. Go to the main screen and to the option menu and click Options->Set trash directory...");
                return;
            }

            // as a prefix, the first 2 chars of the parent directory
            var prefix = FirstTwoChars(Path.GetFileName(Path.GetDirectoryName(s))) + "_";
            var newname = Path.Combine(trashdir, prefix + Path.GetFileName(s) + rand.Next());
            SimpleLog.Current.WriteLog("Moving [" + s + "] to [" + newname + "]");
            File.Move(s, newname);
        }

        public static string CombineProcessArguments(string[] args)
        {
            // By Roger Knapp, http://csharptest.net/529/how-to-correctly-escape-command-line-arguments-in-c/
            if (args == null || args.Length == 0)
                return "";

            StringBuilder arguments = new StringBuilder();
            Regex invalidChar = new Regex("[\x00\x0a\x0d]");//  these can not be escaped
            Regex needsQuotes = new Regex(@"\s|""");//          contains whitespace or two quote characters
            Regex escapeQuote = new Regex(@"(\\*)(""|$)");//    one or more '\' followed with a quote or end of string
            for (int carg = 0; carg < args.Length; carg++)
            {
                if (invalidChar.IsMatch(args[carg]))
                {
                    throw new ArgumentOutOfRangeException("invalid character (" + carg + ")");
                }
                if (args[carg] == String.Empty)
                {
                    arguments.Append("\"\"");
                }
                else if (!needsQuotes.IsMatch(args[carg]))
                {
                    arguments.Append(args[carg]);
                }
                else
                {
                    arguments.Append('"');
                    arguments.Append(escapeQuote.Replace(args[carg], m =>
                        m.Groups[1].Value + m.Groups[1].Value +
                        (m.Groups[2].Value == "\"" ? "\\\"" : "")
                        ));
                    arguments.Append('"');
                }
                if (carg + 1 < args.Length)
                {
                    arguments.Append(' ');
                }
            }
            return arguments.ToString();
        }

        public static bool RepeatWhileFileLocked(string sFilename, int timeout)
        {
            int milliseconds = 250;
            for (int i = 0; i < timeout; i += milliseconds)
            {
                if (!IsFileLocked(sFilename))
                    return true;

                Thread.Sleep(milliseconds);
            }
            return false;
        }

        public static bool IsFileLocked(string sFile)
        {
            FileInfo file = new FileInfo(sFile);
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            return false;
        }

        public static void RunPythonScriptOnSeparateThread(string pyScript, string[] listArgs, bool createWindow = false)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                RunPythonScript(pyScript, listArgs, createWindow: createWindow);
            });
        }

        public static string RunPythonScript(string pyScript, string[] listArgs, bool createWindow = false, bool warnIfStdErr = true)
        {
            if (!pyScript.Contains("/") && !pyScript.Contains("\\"))
            {
                pyScript = Path.Combine(Configs.Current.Directory, pyScript);
            }

            if (!File.Exists(pyScript))
            {
                MessageBox.Show("Script not found "+pyScript);
                return "Script not found";
            }

            var python = Configs.Current.Get(ConfigsPersistedKeys.FilepathPython);
            if (String.IsNullOrEmpty(python) || !File.Exists(python))
            {
                MessageBox.Show("Python exe not found. Go to the main screen and to the option menu and click Options->Set python location...");
                return "Python exe not found.";
            }

            var args = new List<string> { pyScript };
            args.AddRange(listArgs);
            string sErr = RunExeWithArguments(python, args.ToArray(), createWindow, waitForExit: true, shellEx: false);
            if (warnIfStdErr && !String.IsNullOrEmpty(sErr))
            {
                MessageBox.Show("warning, error from script: " + sErr);
            }
            return sErr;
        }

        public static void PlayMedia(string path)
        {
            if (path == null)
                path = Path.Combine(Configs.Current.Directory, "silence.flac");

            var player = Configs.Current.Get(ConfigsPersistedKeys.FilepathMediaPlayer);
            if (String.IsNullOrEmpty(player) || !File.Exists(player))
            {
                MessageBox.Show("Media player not found. Go to the main screen and to the option menu and click Options->Set media player location...");
                return;
            }

            var args = player.ToLower().Contains("foobar") ? new string[] { "/playnow", path } :
                new string[] { path };

            RunExeWithArguments(player, args, createWindow: false, waitForExit: false, shellEx: false);
        }

        public static string GetClipboard()
        {
            try
            {
                return Clipboard.GetText() ?? "";
            }
            catch
            {
                return "";
            }
        }

#if DEBUG
        public static readonly bool Debug = true;
#else
        public static readonly bool Debug = false;
#endif
    }

    public class FileListAutoUpdated
    {
        bool _dirty = true;
        string[] _list = new string[] { };
        FileSystemWatcher m_watcher;
        string _root;
        bool _recurse;
        public FileListAutoUpdated(string root, bool recurse)
        {
            _root = root;
            _recurse = recurse;
            m_watcher = new FileSystemWatcher(root);
            m_watcher.IncludeSubdirectories = recurse;
            m_watcher.Created += m_watcher_Created;
            m_watcher.Renamed += m_watcher_Renamed;
            m_watcher.Deleted += m_watcher_Deleted;
            m_watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            m_watcher.EnableRaisingEvents = true;
        }
        private void m_watcher_Created(object sender, FileSystemEventArgs e)
        {
            _dirty = true;
        }
        private void m_watcher_Renamed(object sender, RenamedEventArgs e)
        {
            _dirty = true;
        }
        private void m_watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            _dirty = true;
        }
        public string[] GetList()
        {
            if (_dirty)
            {
                // for a 900 file directory, DirectoryInfo takes about 13ms, Directory.EnumerateFiles takes about 12ms
                var enumerator = Directory.EnumerateFiles(_root, "*", _recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                var listtemp = enumerator.ToList();
                listtemp.Sort(StringComparer.Ordinal);
                _list = listtemp.ToArray();
                _dirty = false;
            }

            return _list;
        }
    }

    public static class FilenameUtils
    {
        public static string MarkerString = "__MARKAS__";
        public static string AddMarkToFilename(string path, string category)
        {
            if (path.Contains(MarkerString)) {
                MessageBox.Show("Path " + path + " already contains marker.");
                return path;
            }

            var ext = Path.GetExtension(path);
            var before = Path.GetFileNameWithoutExtension(path);
            return Path.Combine(Path.GetDirectoryName(path), before) + MarkerString + category + ext;
        }
        public static void GetMarkFromFilename(string pathAndCatgory, out string path, out string category)
        {
            // check nothing in path has mark
            if (Path.GetDirectoryName(pathAndCatgory).Contains(MarkerString))
                throw new CoordinatePicturesException("Directories should not have magic");

            var parts = Regex.Split(pathAndCatgory, Regex.Escape(MarkerString));
            if (parts.Length != 2)
            {
                MessageBox.Show("Path " + pathAndCatgory + " does not contain marker.");
                path = pathAndCatgory; category = ""; return;
            }
            var partsmore = parts[1].Split(new char[] { '.' });
            Debug.Assert(partsmore.Length == 2);
            category = partsmore[0];
            path = parts[0] + "." + partsmore[1];
        }
        public static bool LooksLikeImage(string s)
        {
            return IsExtensionInList(s, new string[] { ".jpg", ".png", ".gif", ".bmp", ".webp", ".emf", ".wmf", ".jpeg" });
        }
        public static bool LooksLikeEditableAudio(string s)
        {
            return IsExtensionInList(s, new string[] { ".wav", ".flac", ".mp3" });
        }
        public static bool IsExtensionInList(string s, string[] sExts)
        {
            var sLower = s.ToLowerInvariant();
            foreach (var item in sExts)
            {
                if (sLower.EndsWith(item))
                    return true;
            }
            return false;
        }
    }

    public class SimpleLog
    {
        private static SimpleLog _instance;
        private SimpleLog(string path) { _path = path; }
        string _path;

        public static void Init(string path)
        {
            _instance = new SimpleLog(path);
        }
        public static SimpleLog Current
        {
            get
            {
                return _instance;
            }
        }
        public void WriteLog(string s)
        {
            try
            {
                File.AppendAllText(_path, Environment.NewLine + s);
            }
            catch (Exception)
            {
                if (!Utils.AskToConfirm("Could not write to " + _path +
                    "; labs_coordinate_pictures.exe currently needs to be in writable directory. Continue?"))
                    Environment.Exit(1);
            }
        }
        public void WriteWarning(string s)
        {
            WriteLog("[warning] " + s);
        }
        public void WriteError(string s)
        {
            WriteLog("[error] " + s);
        }
        public void WriteVerbose(string s)
        {
            if (Configs.Current.GetBool(ConfigsPersistedKeys.EnableVerboseLogging))
            {
                WriteLog("[vb] " + s);
            }
        }
    }

    public class CoordinatePicturesException : ApplicationException
    {
        public CoordinatePicturesException(string message) : base("CoordinatePictures " + message) { }
    }
}
