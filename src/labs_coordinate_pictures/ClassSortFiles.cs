using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace labs_coordinate_pictures
{
    public enum SortFilesAction
    {
        SearchDifferences,
        SearchDupes,
        SearchDupesInOneDir,
        SyncFiles
    }

    public enum FilePathsListViewItemType
    {
        // numeric value used as index of an ImageList.
        None = 0,
        Changed_File = 1,
        Src_Only = 2,
        Dest_Only = 3,
        Same_Contents = 4,
        Moved_File = 5,
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

    public static class SyncFilesWithRobocopy
    {
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

    public class FileEntry
    {
        public FileEntry(long lastModifiedTime, long filesize, string filepath)
        {
            LastModifiedTime = lastModifiedTime;
            Filesize = filesize;
            Filepath = filepath;
        }

        public long LastModifiedTime { get; set; }
        public long Filesize { get; set; }
        public string Filepath { get; set; }
        public string Checksum { get; set; }
        public bool Visited { get; set; }

        public static FileEntry EntryFromFileInfo(FileInfo fileInfo, string fullpath, string stripPrefix,
            bool roundLastModifiedTime, int adjustTimeHours)
        {
            var dt = fileInfo.LastWriteTimeUtc;
            dt = dt.AddHours(adjustTimeHours);
            long ticks = dt.Ticks;

            if (roundLastModifiedTime)
            {
                ticks >>= 2;
            }

            var path = fullpath.Substring(stripPrefix.Length);
            return new FileEntry(ticks, fileInfo.Length, path);
        }
    }

    public static class FindMovedFiles
    {
        // finds 'quick' differences. won't see cases where contents differ, but lmt and filesize are the same.
        // returns (only on left, only on right, modified)
        public static List<FilePathsListViewItem> FindQuickDifferencesByModifiedTimeAndFilesize(SortFilesSettings settings)
        {
            var results = new List<FilePathsListViewItem>();

            // make an index of files on the left
            var indexFilesLeft = new Dictionary<string, FileEntry>();
            var di = new DirectoryInfo(settings.SourceDirectory);
            foreach (var fileInfo in di.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                //var entryLeft = FileEntry.EntryFromFileInfo(fileInfo, fileInfo.FullName,
                //    settings.SourceDirectory, settings.AllowFiletimesDifferForFAT, settings.);
                //indexFilesLeft[entryLeft.Filepath] = entryLeft;
            }

            // walk through files on the right
            di = new DirectoryInfo(settings.DestDirectory);
            foreach (var fileInfo in di.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var entryRight = FileEntry.EntryFromFileInfo(fileInfo, fileInfo.FullName,
                    settings.SourceDirectory, settings.AllowFiletimesDifferForFAT, adjustTimeHours: 0);
                FileEntry entryLeft;
                indexFilesLeft.TryGetValue(entryRight.Filepath, out entryLeft);

                if (entryLeft == null)
                {
                    results.Add(new FilePathsListViewItem("", fileInfo.FullName, FilePathsListViewItemType.Dest_Only,
                        -1, -1, entryRight.Filesize, entryRight.LastModifiedTime));
                }
                else
                {
                    entryLeft.Visited = true;
                    if (entryLeft.Filesize != entryRight.Filesize || entryLeft.LastModifiedTime != entryRight.LastModifiedTime)
                    {
                        results.Add(new FilePathsListViewItem(
                            settings.SourceDirectory + Path.DirectorySeparatorChar + entryLeft.Filepath,
                            settings.DestDirectory + Path.DirectorySeparatorChar + entryRight.Filepath,
                            FilePathsListViewItemType.Changed_File,
                            entryLeft.Filesize, entryLeft.LastModifiedTime,
                            entryRight.Filesize, entryRight.LastModifiedTime));
                    }
                }
            }

            // find files that weren't seen on the right
            foreach (var entryLeft in indexFilesLeft)
            {
                if (!entryLeft.Value.Visited)
                {
                    results.Add(new FilePathsListViewItem(
                        settings.SourceDirectory + Path.DirectorySeparatorChar + entryLeft.Value.Filepath,
                        "", FilePathsListViewItemType.Src_Only,
                        entryLeft.Value.Filesize, entryLeft.Value.LastModifiedTime, -1, -1));
                }
            }

            return results;
        }

        public static List<FilePathsListViewItem> DifferencesToFindDupes(FilePathsListViewItem[] listAll)
        {
            // make an index of files on the left
            // map of (filesize, lmt) to FileEntry
            var indexLeft = new Dictionary<Tuple<long, long>, List<FileEntry>>();
            foreach (var item in listAll)
            {
                if (item.Status == FilePathsListViewItemType.Src_Only)
                {
                    List<FileEntry> shortList;
                    var tp = Tuple.Create(item.FirstLastModifiedTime, item.FirstFileLength);
                    indexLeft.TryGetValue(tp, out shortList);
                    if (shortList == null)
                    {
                        shortList = new List<FileEntry>();
                        indexLeft[tp] = shortList;
                    }

                    var entry = new FileEntry(item.FirstLastModifiedTime, item.FirstFileLength, item.FirstPath);
                    shortList.Add(entry);
                }
            }

            var moved = new Dictionary<string, bool>();
            var results = new List<FilePathsListViewItem>();

            // walk through files on the right
            foreach (var item in listAll)
            {
                if (item.Status == FilePathsListViewItemType.Dest_Only)
                {
                    List<FileEntry> shortList;
                    var tp = Tuple.Create(item.SecondLastModifiedTime, item.SecondFileLength);
                    indexLeft.TryGetValue(tp, out shortList);
                    var entryFound = IsPresentInList(shortList, moved, item.SecondLastModifiedTime, item.SecondFileLength, item.SecondPath);

                    if (entryFound != null)
                    {
                        moved[entryFound.Filepath] = true;
                        moved[item.SecondPath] = true;
                        results.Add(new FilePathsListViewItem(entryFound.Filepath, item.SecondPath, FilePathsListViewItemType.Moved_File,
                            entryFound.Filesize, entryFound.Filesize, item.SecondFileLength, item.SecondLastModifiedTime));
                    }
                }
            }

            // add the remaining ones
            foreach (var item in listAll)
            {
                if (!moved.ContainsKey(item.FirstPath) && !moved.ContainsKey(item.SecondPath))
                {
                    results.Add(item);
                }
            }

            return results;
        }

        static FileEntry IsPresentInList(List<FileEntry> list, Dictionary<string, bool> movedAlready, long lastModifiedTime, long filesize, string path)
        {
            if (list != null)
            {
                string checksumThis = Utils.GetSha512(path);
                foreach (var entry in list)
                {
                    if (entry.Checksum == null)
                    {
                        entry.Checksum = Utils.GetSha512(path);
                    }

                    if (entry.Checksum == checksumThis &&
                        entry.LastModifiedTime == lastModifiedTime &&
                        entry.Filesize == filesize &&
                        !movedAlready.ContainsKey(entry.Filepath))
                    {
                        return entry;
                    }
                }
            }

            return null;
        }
    }

    public static class SortFilesSearchDifferences
    {
        public class FileInformation
        {
            public string _filename;
            public long _filesize;
            public DateTime _lastModifiedTime;
            public bool _wasSeenInDest;
        }

        public class FileInformationForUI
        {
            public FileInformation _fileInfoLeft;
            public FileInformation _fileInfoRight;
            public FilePathsListViewItemType _type;

            public FileInformationForUI(FileInformation left, FileInformation right,
                FilePathsListViewItemType type)
            {
                _fileInfoLeft = left;
                _fileInfoRight = right;
                _type = type;
            }
        }

        const int AllowDifferSeconds = 4;

        public static List<FileInformationForUI> SearchDifferences(SortFilesSettings settings)
        {
            var ret = new List<FileInformationForUI>();
            Dictionary<string, FileInformation> filesSrc =
                new Dictionary<string, FileInformation>(StringComparer.OrdinalIgnoreCase);

            // go through files in source
            var diSrc = new DirectoryInfo(settings.SourceDirectory);
            foreach (var fileInfo in diSrc.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var obj = new FileInformation();
                obj._filename = fileInfo.FullName;
                obj._filesize = fileInfo.Length;
                obj._lastModifiedTime = fileInfo.LastWriteTimeUtc;
                obj._wasSeenInDest = false;
                filesSrc[obj._filename] = obj;
            }

            // go through files in dest
            var diDest = new DirectoryInfo(settings.DestDirectory);
            foreach (var fileInfo in diSrc.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                FileInformation objSrc;
                if (filesSrc.TryGetValue(fileInfo.FullName, out objSrc))
                {
                    objSrc._wasSeenInDest = true;
                    if (objSrc._filesize != fileInfo.Length ||
                        !AreTimesTheSame(objSrc._lastModifiedTime, fileInfo.LastWriteTimeUtc, settings))
                    {
                        var objDest = new FileInformation();
                        objDest._filename = fileInfo.FullName;
                        objDest._filesize = fileInfo.Length;
                        objDest._lastModifiedTime = fileInfo.LastWriteTimeUtc;
                        objDest._wasSeenInDest = true;
                        ret.Add(new FileInformationForUI(objSrc, objDest, FilePathsListViewItemType.Changed_File));
                    }
                }
                else
                {
                    // looks like a new file
                    var objDest = new FileInformation();
                    objDest._filename = fileInfo.FullName;
                    objDest._filesize = fileInfo.Length;
                    objDest._lastModifiedTime = fileInfo.LastWriteTimeUtc;
                    objDest._wasSeenInDest = true;
                    ret.Add(new FileInformationForUI(null, objDest, FilePathsListViewItemType.Dest_Only));
                }
            }

            // which files did we see in src but not in dest?
            foreach (var kvo in filesSrc)
            {
                if (!kvo.Value._wasSeenInDest)
                {
                    ret.Add(new FileInformationForUI(
                        kvo.Value, null, FilePathsListViewItemType.Src_Only));
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
