using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public static class OsHelpers
    {
        // By Roger Knapp, http://csharptest.net/529/how-to-correctly-escape-command-line-arguments-in-c/
        public static string CombineProcessArguments(string[] args)
        {
            StringBuilder arguments = new StringBuilder();
            Regex invalidChar = new Regex("[\x00\x0a\x0d]");//  these can not be escaped
            Regex needsQuotes = new Regex(@"\s|""");//          contains whitespace or two quote characters
            Regex escapeQuote = new Regex(@"(\\*)(""|$)");//    one or more '\' followed with a quote or end of string
            for (int carg = 0; carg < args.Length; carg++)
            {
                if (invalidChar.IsMatch(args[carg]))
                {
                    throw new ArgumentOutOfRangeException("args[" + carg + "]");
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

        public static bool AskToConfirm(string s)
        {
            var res = MessageBox.Show(s, "", MessageBoxButtons.YesNo);
            return res == DialogResult.Yes;
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
        private static readonly SimpleLog instance = new SimpleLog("./log.txt");
        private SimpleLog(string path) { _path = path; }
        string _path;

        public static SimpleLog Current
        {
            get
            {
                return instance;
            }
        }
        public void WriteLog(string s)
        {
            try
            {
                File.AppendAllText(_path, s);
            }
            catch (Exception)
            {
                if (!OsHelpers.AskToConfirm("Could not write to " + _path +
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
            if (ClassConfigs.Current.GetBool(ConfigsPersistedKeys.EnableVerboseLogging))
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
