using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace labs_coordinate_pictures
{
    public class SortFilesSettings
    {
        public string SourceDirectory { get; set; }
        public string DestDirectory { get; set; }
        public bool AllowFiletimesDifferForFAT { get; set; }
        public int ShiftFiletimeHours { get; set; }
        public string[] SkipDirectories { get; set; }
        public string[] SkipFiles { get; set; }
        public bool Mirror { get; set; }
        public string LogFile { get; set; }
    }

    public static class RobocopySyncFiles
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

            if (settings.ShiftFiletimeHours == 1)
            {
                args.Add("/DST");
            }

            if (settings.Mirror)
            {
                args.Add("/MIR");
            }

            foreach (var s in settings.SkipDirectories)
            {
                args.Add("/XD");
                args.Add(s);
            }

            foreach (var s in settings.SkipFiles)
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
            if (!Utils.AskToConfirm("Sync files?"))
            {
                return;
            }

            var workingDir = Environment.SystemDirectory;
            string unused1, unused2;
            int retcode = Utils.Run("robocopy.exe", GetArgs(settings), shellExecute: false, waitForExit: true,
                hideWindow: true, getStdout: false, outStdout: out unused1, outStderr: out unused2, workingDir: workingDir);
            if (retcode != 0)
            {
                Utils.MessageErr("Warning: non zero return code, " + retcode);
            }
            else
            {
                Utils.MessageBox("Complete.");
            }

            Utils.Run("notepad.exe", new string[] { settings.LogFile }, shellExecute: false, waitForExit: false,
                hideWindow: false, getStdout: false, outStdout: out unused1, outStderr: out unused2, workingDir: workingDir);
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
                var entryLeft = FileEntry.EntryFromFileInfo(fileInfo, fileInfo.FullName,
                    settings.SourceDirectory, settings.AllowFiletimesDifferForFAT, settings.ShiftFiletimeHours);
                indexFilesLeft[entryLeft.Filepath] = entryLeft;
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
                    results.Add(new FilePathsListViewItem("", fileInfo.FullName, FilePathsListViewItemType.Right_Only,
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
                        "", FilePathsListViewItemType.Left_Only,
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
                if (item.Status == FilePathsListViewItemType.Left_Only)
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
                if (item.Status == FilePathsListViewItemType.Right_Only)
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

                    if (entry.Checksum == checksumThis && !movedAlready.ContainsKey(entry.Filepath))
                    {
                        return entry;
                    }
                }
            }

            return null;
        }
    }
}
