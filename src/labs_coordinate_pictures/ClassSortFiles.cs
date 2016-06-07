using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public enum SortFilesAction
    {
        SearchDifferences,
        SearchDupes,
        SearchDupesInOneDir,
        SyncFiles
    }

    public class SortFilesSettings
    {
        string[] _skipDirectories;
        string[] _skipFiles;

        public string SourceDirectory { get; set; }
        public string DestDirectory { get; set; }
        public bool AllowFiletimesDifferForFAT { get; set; }
        public bool AllowFiletimesDifferForDST { get; set; }
        public bool Mirror { get; set; }
        public bool PreviewOnly { get; set; }
        public string LogFile { get; set; }

        public void SetSkipDirectories(string[] skipDirs)
        {
            _skipDirectories = skipDirs;
        }

        public void SetSkipFiles(string[] skipFiles)
        {
            _skipFiles = skipFiles;
        }

        public string[] GetSkipDirectories()
        {
            return _skipDirectories;
        }

        public string[] GetSkipFiles()
        {
            return _skipFiles;
        }
    }

    public class FileComparisonResult : ListViewItem
    {
        public FileInfoForComparison _fileInfoLeft;
        public FileInfoForComparison _fileInfoRight;
        public FileComparisonResultType _type;

        public FileComparisonResult(FileInfoForComparison left, FileInfoForComparison right,
            FileComparisonResultType type)
        {
            _fileInfoLeft = left;
            _fileInfoRight = right;
            _type = type;
        }
    }

    public class FileInfoForComparison
    {
        public string _filename;
        public long _filesize;
        public DateTime _lastModifiedTime;
        public bool _wasSeenInDest;
        public string _contentHash;
        public FileInfoForComparison(string filename, long filesize, DateTime lastModifiedTime,
            string contentHash = null, bool wasSeenInDest = false)
        {
            _filename = filename;
            _filesize = filesize;
            _lastModifiedTime = lastModifiedTime;
            _wasSeenInDest = wasSeenInDest;
            _contentHash = contentHash;
        }
    }

    public enum FileComparisonResultType
    {
        // numeric value used as index of an ImageList.
        None = 0,
        Changed_File = 1,
        Src_Only = 2,
        Dest_Only = 3,
        Same_Contents = 4,
        Moved_File = 5,
    }

    public static class SyncFilesWithRobocopy
    {
        public const int AllowDifferSeconds = 2;

        public static string GetFullArgs(SortFilesSettings settings)
        {
            var args = Utils.CombineProcessArguments(GetArgs(settings));
            return "robocopy.exe " + args;
        }

        public static string[] GetArgs(SortFilesSettings settings)
        {
            var args = new List<string>();
            args.Add(settings.SourceDirectory);
            args.Add(settings.DestDirectory);
            args.Add("/S"); // include subdirectories
            args.Add("/E"); // include empty folders

            if (settings.AllowFiletimesDifferForFAT)
            {
                args.Add("/FFT");
            }

            if (settings.AllowFiletimesDifferForDST)
            {
                args.Add("/DST");
            }

            if (settings.Mirror)
            {
                args.Add("/MIR");
            }

            if (settings.PreviewOnly)
            {
                args.Add("/L");
            }

            foreach (var s in settings.GetSkipDirectories())
            {
                args.Add("/XD");
                args.Add(s);
            }

            foreach (var s in settings.GetSkipFiles())
            {
                args.Add("/XF");
                args.Add(s);
            }

            args.Add("/NS"); // no sizes in output
            args.Add("/FP"); // full paths in logs
            args.Add("/NP"); // don't show progress
            args.Add("/UNILOG:" + settings.LogFile);
            return args.ToArray();
        }

        public static void Run(SortFilesSettings settings)
        {
            string unused1, unused2;
            var workingDir = Environment.SystemDirectory;
            int retcode = Utils.Run("robocopy.exe", GetArgs(settings), shellExecute: false,
                waitForExit: true, hideWindow: true, getStdout: false, outStdout: out unused1,
                outStderr: out unused2, workingDir: workingDir);

            if (retcode != 0)
            {
                Utils.MessageErr("Warning: non zero return code, " + retcode);
            }
            else
            {
                Utils.MessageBox("Complete.");
            }

            // show log results in notepad
            if (File.Exists(settings.LogFile))
            {
                Utils.Run("notepad.exe", new string[] { settings.LogFile }, shellExecute: false,
                    waitForExit: false, hideWindow: false, getStdout: false, outStdout: out unused1,
                    outStderr: out unused2, workingDir: workingDir);
            }
            else
            {
                Utils.MessageErr("Log file not found.");
            }
        }
    }

    public static class SortFilesSearchDuplicates
    {
        public static Dictionary<long, List<FileInfoForComparison>> GetIndexByFilesize(string dirName)
        {
            // using a list better than subdictionaries, because for SearchDuplicatesInOneDirectory we want
            // to track the order it was seen in, for consistency.
            var ret = new Dictionary<long, List<FileInfoForComparison>>();

            var di = new DirectoryInfo(dirName);
            foreach (var info in di.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var filename = info.FullName.Substring(dirName.Length);
                var obj = new FileInfoForComparison(
                    filename, info.Length, info.LastWriteTimeUtc);

                List<FileInfoForComparison> list;
                var key = obj._filesize;
                if (!ret.TryGetValue(key, out list))
                {
                    list = ret[key] = new List<FileInfoForComparison>();
                }

                list.Add(obj);
            }

            return ret;
        }

        public static List<FileComparisonResult> SearchDuplicatesAcrossDirectories(SortFilesSettings settings)
        {
            var ret = new List<FileComparisonResult>();
            var indexSrc = GetIndexByFilesize(settings.SourceDirectory);

            var diDest = new DirectoryInfo(settings.DestDirectory);
            foreach (var info in diDest.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                List<FileInfoForComparison> list;
                if (indexSrc.TryGetValue(info.Length, out list))
                {
                    var hashDest = Utils.GetSha512(info.FullName);
                    foreach (var listitem in list)
                    {
                        if (listitem._contentHash == null)
                        {
                            listitem._contentHash = Utils.GetSha512(Path.Combine(settings.SourceDirectory, listitem._filename));
                        }

                        if (listitem._contentHash == hashDest)
                        {
                            var filenameRight = info.FullName.Substring(settings.DestDirectory.Length);
                            var objRight = new FileInfoForComparison(
                                filenameRight, info.Length, info.LastWriteTimeUtc, hashDest);
                            ret.Add(new FileComparisonResult(
                                listitem, objRight, FileComparisonResultType.Same_Contents));
                            break;
                        }
                    }
                }
            }

            return ret;
        }

        public static List<FileComparisonResult> SearchDuplicatesInOneDirectory(SortFilesSettings settings)
        {
            var ret = new List<FileComparisonResult>();
            var index = GetIndexByFilesize(settings.SourceDirectory);
            foreach (var list in index.Values)
            {
                if (list.Count > 1)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        list[i]._contentHash = Utils.GetSha512(Path.Combine(
                            settings.SourceDirectory, list[i]._filename));

                        // have we seen this hash before?
                        // this is an n squared loop, but calculating content hashes is far more expensive.
                        for (int j = 0; j < i; j++)
                        {
                            if (list[j]._contentHash == list[i]._contentHash)
                            {
                                ret.Add(new FileComparisonResult(
                                    list[j], list[i], FileComparisonResultType.Same_Contents));
                                break;
                            }
                        }
                    }
                }
            }

            return ret;
        }
    }


    public static class SortFilesSearchDifferences
    {

        public const int AllowDifferSeconds = 4;

        public static List<FileComparisonResult> SearchDifferences(SortFilesSettings settings)
        {
            var ret = new List<FileComparisonResult>();
            Dictionary<string, FileInfoForComparison> filesSrc =
                new Dictionary<string, FileInfoForComparison>(StringComparer.OrdinalIgnoreCase);

            // go through files in source
            var diSrc = new DirectoryInfo(settings.SourceDirectory);
            foreach (var info in diSrc.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var filename = info.FullName.Substring(settings.SourceDirectory.Length);
                filesSrc[filename] = new FileInfoForComparison(
                    filename, info.Length, info.LastWriteTimeUtc);
            }

            // go through files in dest
            var diDest = new DirectoryInfo(settings.DestDirectory);
            foreach (var info in diDest.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                FileInfoForComparison objSrc;
                if (filesSrc.TryGetValue(info.FullName, out objSrc))
                {
                    objSrc._wasSeenInDest = true;
                    if (objSrc._filesize != info.Length ||
                        !AreTimesTheSame(objSrc._lastModifiedTime, info.LastWriteTimeUtc, settings))
                    {
                        // looks like a modified file. same path but different filesize/lmt.
                        var filename = info.FullName.Substring(settings.DestDirectory.Length);
                        var objDest = new FileInfoForComparison(
                            filename, info.Length, info.LastWriteTimeUtc);
                        ret.Add(new FileComparisonResult(
                            objSrc, objDest, FileComparisonResultType.Changed_File));
                    }
                }
                else
                {
                    // looks like a new file
                    var filename = info.FullName.Substring(settings.DestDirectory.Length);
                    var objDest = new FileInfoForComparison(
                        filename, info.Length, info.LastWriteTimeUtc);
                    ret.Add(new FileComparisonResult(
                        null, objDest, FileComparisonResultType.Dest_Only));
                }
            }

            // which files did we see in src but not in dest?
            foreach (var kvp in filesSrc)
            {
                if (!kvp.Value._wasSeenInDest)
                {
                    // looks like a deleted file since it didn't show up in the destination.
                    ret.Add(new FileComparisonResult(
                        kvp.Value, null, FileComparisonResultType.Src_Only));
                }
            }

            return ret;
        }

        public static bool AreTimesTheSameHelper(DateTime dt1, DateTime dt2, SortFilesSettings settings)
        {
            if (settings.AllowFiletimesDifferForFAT)
            {
                return Math.Abs(dt1.Ticks - dt2.Ticks) <= AllowDifferSeconds * TimeSpan.TicksPerSecond;
            }
            else
            {
                return dt1.Ticks == dt2.Ticks;
            }
        }

        public static bool AreTimesTheSame(DateTime dt1, DateTime dt2, SortFilesSettings settings)
        {
            if (settings.AllowFiletimesDifferForDST)
            {
                return AreTimesTheSameHelper(dt1, dt2, settings)
                    || AreTimesTheSameHelper(dt1, dt2.AddHours(1), settings)
                    || AreTimesTheSameHelper(dt1, dt2.AddHours(-1), settings);
            }
            else
            {
                return AreTimesTheSameHelper(dt1, dt2, settings);
            }
        }
    }
}
