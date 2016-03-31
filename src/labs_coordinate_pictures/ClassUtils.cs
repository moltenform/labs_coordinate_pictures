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
            Process.Start("explorer.exe", "\"" + sDir + "\"");
        }

        public static void SelectFileInExplorer(string path)
        {
            Process.Start("explorer.exe", "/select,\"" + path+"\"");
        }

        public static void Run(string exe, string[] args, bool shell, bool waitForExit, bool hideWindow)
        {
            string stdout, stderr;
            Run(exe, args, shell, waitForExit, hideWindow, false, out stdout, out stderr);
        }

        public static void Run(string exe, string[] args, bool shell, bool waitForExit, bool hideWindow, out string stdout, out string stderr)
        {
            Run(exe, args, shell, waitForExit, hideWindow, true, out stdout, out stderr);
        }

        static void Run(string exe, string[] args, bool shell, bool waitForExit, bool hideWindow, bool getStdout, out string outStdout, out string outStderr, string workingDir = null)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = hideWindow;
            startInfo.UseShellExecute = shell;
            startInfo.FileName = exe;
            startInfo.Arguments = CombineProcessArguments(args);
            startInfo.WorkingDirectory = workingDir;
            if (getStdout)
            {
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
            }

            string serr = "", sout = "";
            var process = Process.Start(startInfo);
            if (getStdout)
            {
                process.OutputDataReceived += (sender, errordataargs) => sout += errordataargs.Data;
                process.ErrorDataReceived += (sender, errordataargs) => serr += errordataargs.Data;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            if (waitForExit || getStdout)
            {
                process.WaitForExit();
            }

            outStdout = sout;
            outStderr = serr;
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

        public static string GetSoftDeleteDestination(string s)
        {
            var trashdir = Configs.Current.Get(ConfigKey.FilepathTrash);
            if (String.IsNullOrEmpty(trashdir) ||
                !Directory.Exists(trashdir))
            {
                MessageBox.Show("Trash directory not found. Go to the main screen and to the option menu and click Options->Set trash directory...");
                return null;
            }

            // as a prefix, the first 2 chars of the parent directory
            var prefix = FirstTwoChars(Path.GetFileName(Path.GetDirectoryName(s))) + "_";
            return Path.Combine(trashdir, prefix + Path.GetFileName(s) + rand.Next());
        }

        public static void SoftDelete(string s)
        {
            var newname = GetSoftDeleteDestination(s);
            if (newname != null)
            {
                SimpleLog.Current.WriteLog("Moving [" + s + "] to [" + newname + "]");
                File.Move(s, newname);
            }
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

        public static string FormatFilesize(string path)
        {
            if (!File.Exists(path))
                return " file not found";

            var len = new FileInfo(path).Length;
            return (len > 1024 * 1024) ?
                String.Format(" ({0:0.00}mb)", len / (1024.0 * 1024.0)) :
                String.Format(" ({0}k)", (len / 1024));
        }

        public static void CloseOtherProcessesByName(string name)
        {
            var thisId = Process.GetCurrentProcess().Id;
            foreach (var process in Process.GetProcessesByName(name))
            {
                if (process.Id != thisId)
                    process.Kill();
            }
        }

        public static void RunPythonScriptOnSeparateThread(string pyScript, string[] listArgs, bool createWindow = false)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                RunPythonScript(pyScript, listArgs, createWindow: createWindow);
            });
        }

        public static string RunPythonScript(string pyScript, string[] listArgs, bool createWindow = false, bool warnIfStdErr = true, string workingDir = null)
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

            var python = Configs.Current.Get(ConfigKey.FilepathPython);
            if (String.IsNullOrEmpty(python) || !File.Exists(python))
            {
                MessageBox.Show("Python exe not found. Go to the main screen and to the option menu and click Options->Set python location...");
                return "Python exe not found.";
            }

            var args = new List<string> { pyScript };
            args.AddRange(listArgs);
            string stdout, stderr;
            Run(python, args.ToArray(), false, true, !createWindow, true, out stdout, out stderr, workingDir: workingDir);
            if (warnIfStdErr && !String.IsNullOrEmpty(stderr))
            {
                MessageBox.Show("warning, error from script: " + stderr);
            }
            return stderr;
        }

        public static bool RunImageConversion(string pathin, string pathout, string resizeSpec, int jpgQuality)
        {
            // send a good working directory for the script so that it can find options.ini
            var scriptcurdir = Path.Combine(Configs.Current.Directory, "ben_python_img");
            var script = Path.Combine(Configs.Current.Directory, "ben_python_img", "main.py");
            var args = new string[] { "convert_resize", pathin, pathout, resizeSpec, jpgQuality.ToString() };
            var stderr = RunPythonScript(script, args, createWindow: false, warnIfStdErr: false, workingDir: scriptcurdir);
            if (!String.IsNullOrEmpty(stderr) || !File.Exists(pathout))
            {
                MessageBox.Show("RunImageConversion failed, stderr = " + stderr);
                return false;
            }
            else
            {
                return true;
            }
        }

        public static void PlayMedia(string path)
        {
            if (path == null)
                path = Path.Combine(Configs.Current.Directory, "silence.flac");

            var player = Configs.Current.Get(ConfigKey.FilepathMediaPlayer);
            if (String.IsNullOrEmpty(player) || !File.Exists(player))
            {
                MessageBox.Show("Media player not found. Go to the main screen and to the option menu and click Options->Set media player location...");
                return;
            }

            var args = player.ToLower().Contains("foobar") ? new string[] { "/playnow", path } :
                new string[] { path };

            Run(player, args, hideWindow: true, waitForExit: false, shell: false);
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

        public static string GetFirstHttpLink(string s)
        {
            foreach (var match in Regex.Matches(s, @"https?://\S+"))
            {
                return ((Match)match).ToString();
            }
            return null;
        }

        public static void LaunchUrl(string s)
        {
            s = GetFirstHttpLink(s);
            if (s != null && s.StartsWith("http"))
                Process.Start(s);
        }

        public static T ArrayAt<T>(T[] arr, int index)
        {
            if (index < 0)
                return arr[0];
            if (index >= arr.Length - 1)
                return arr[arr.Length - 1];

            return arr[index];
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
        public bool Recurse {get; private set;}
        public FileListAutoUpdated(string root, bool recurse)
        {
            _root = root;
            Recurse = recurse;
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
        public void Dirty()
        {
            _dirty = true;
        }
        public string[] GetList(bool forceRefresh=false)
        {
            if (_dirty || forceRefresh)
            {
                // DirectoryInfo takes about 13ms, Directory.EnumerateFiles takes about 12ms, for a 900 file directory
                var enumerator = Directory.EnumerateFiles(_root, "*", Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                _list = enumerator.ToArray();
                Array.Sort<string>(_list, StringComparer.OrdinalIgnoreCase);
                _dirty = false;
            }

            return _list;
        }
    }

    public class FileListNavigation
    {
        string[] _extensionsAllowed;
        bool _excludeMarked;
        FileListAutoUpdated _list;
        public string Current { get; private set; }
        public string BaseDirectory { get; private set; }
        public FileListNavigation(string basedir, string[] extensionsAllowed, bool fRecurse, bool excludeMarked = true, string sCurrent = "")
        {
            BaseDirectory = basedir;
            _extensionsAllowed = extensionsAllowed;
            _list = new FileListAutoUpdated(basedir, fRecurse);
            _excludeMarked = excludeMarked;
            TrySetPath(sCurrent);
        }

        public void Refresh(bool justPokeUpdates = false)
        {
            _list = new FileListAutoUpdated(BaseDirectory, _list.Recurse);
            TrySetPath("");
        }

        public void NotifyFileChanges()
        {
            _list.Dirty();
        }

        void TryAgainIfFileIsMissing(Func<string[], string> fn)
        {
            var list = GetList();
            if (list.Length == 0)
            {
                Current = null;
                return;
            }

            string firstTry = fn(list);
            if (firstTry != null && !File.Exists(firstTry))
            {
                list = GetList(true /*refresh the list*/);
                if (list.Length == 0)
                {
                    Current = null;
                    return;
                }

                Current = fn(list);
            }
            else
            {
                Current = firstTry;
            }
        }

        static int GetLessThanOrEqual(string[] list, string search)
        {
            var index = Array.BinarySearch<string>(list, search);
            if (index < 0)
                index = ~index - 1;
            return index;
        }

        public void GoNextOrPrev(bool isNext, List<string> neighbors=null, int retrieveNeighbors=0)
        {
            TryAgainIfFileIsMissing((list) =>
            {
                var index = GetLessThanOrEqual(list, Current ?? "");
                if (isNext)
                {
                    for (int i = 0; i < retrieveNeighbors; i++)
                    {
                        neighbors[i] = Utils.ArrayAt(list, index + i + 2);
                    }

                    return Utils.ArrayAt(list, index + 1);
                }
                else
                {
                    // index is LessThanOrEqual, but we want just LessThan, so move prev if equal.
                    if (index > 0 && Current == list[index])
                        index--;

                    for (int i = 0; i < retrieveNeighbors; i++)
                    {
                        neighbors[i] = Utils.ArrayAt(list, index - i - 1);
                    }

                    return Utils.ArrayAt(list, index);
                }
            });
        }

        public void GoFirst()
        {
            TryAgainIfFileIsMissing((list) =>
            {
                return list[0];
            });
        }
        public void GoLast()
        {
            TryAgainIfFileIsMissing((list) =>
            {
                return list[list.Length - 1];
            });
        }

        public void TrySetPath(string sCurrent, bool verify = true)
        {
            Current = sCurrent;
            if (verify)
            {
                TryAgainIfFileIsMissing((list) =>
                {
                    var index = GetLessThanOrEqual(list, Current ?? "");
                    return Utils.ArrayAt(list, index);
                });
            }
        }

        public string[] GetList(bool forceRefresh = false, bool includeMarked = false)
        {
            Func<string, bool> includeFile = (path) =>
            {
                if ((includeMarked || !_excludeMarked) && path.Contains(FilenameUtils.MarkerString))
                    return false;
                if (!FilenameUtils.IsExtensionInList(path, _extensionsAllowed))
                    return false;
                return true;
            };

            return _list.GetList().Where(includeFile).ToArray();
        }
    }

    public static class FilenameUtils
    {
        public static bool LooksLikeImage(string s)
        {
            return IsExtensionInList(s, new string[] { ".jpg", ".png", ".gif", ".bmp", ".webp", ".emf", ".wmf", ".jpeg" });
        }
        public static bool LooksLikeEditableAudio(string s)
        {
            return IsExtensionInList(s, new string[] { ".wav", ".flac", ".mp3", ".m4a", ".mp4" });
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
        public static bool IsExt(string s, string ext)
        {
            return s.ToLowerInvariant().EndsWith("."+ ext);
        }
        public static string AddNumberedPrefix(string path, int n)
        {
            var nameOnly = Path.GetFileName(path);
            if (nameOnly != GetFileNameWithoutNumberedPrefix(path))
            {
                // already has one
                return path;
            }
            else
            {
                // add a trailing zero, just lets the user change the order more easily.
                return Path.GetDirectoryName(path) +
                    "\\([" + n.ToString("D3") + "0])" + nameOnly;
            }
        }
        public static string GetFileNameWithoutNumberedPrefix(string path)
        {
            var nameOnly = Path.GetFileName(path);
            if (nameOnly.Length > 8 && nameOnly.StartsWith("([") && nameOnly.Substring(6, 2) == "])")
                return nameOnly.Substring(8);
            else
                return nameOnly;
        }

        public static string MarkerString = "__MARKAS__";
        public static string AddMarkToFilename(string path, string category)
        {
            if (path.Contains(MarkerString))
            {
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
        
        public static bool SameExceptExtension(string s1, string s2, string[] allowedTypes)
        {
            var rootNoExtension1 = Path.Combine(Path.GetDirectoryName(s1), Path.GetFileNameWithoutExtension(s1));
            var rootNoExtension2 = Path.Combine(Path.GetDirectoryName(s2), Path.GetFileNameWithoutExtension(s2));
            return rootNoExtension1.ToLowerInvariant() == rootNoExtension2.ToLowerInvariant() &&
                Array.IndexOf(allowedTypes, "." + Path.GetExtension(s2)) != -1;
        }
        public static bool IsPathRooted(string s)
        {
            try
            {
                return Path.IsPathRooted(s);
            }
            catch (ArgumentException)
            {
                return false;
            }
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
            if (Configs.Current.GetBool(ConfigKey.EnableVerboseLogging))
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
