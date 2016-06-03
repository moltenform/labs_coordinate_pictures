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
        public long LastModifiedTime { get; set; }
        public long Filesize { get; set; }
        public string Filepath { get; set; }
        public string Checksum { get; set; }
        public bool Flag { get; set; }

        public static FileEntry EntryFromFileInfo(FileInfo fileInfo, string fullpath, string stripPrefix,
            bool roundLastModifiedTime, int adjustTimeHours)
        {
            FileEntry ret = new FileEntry();

            // nb: for a FAT system, o.s. will convert to utc.
            var dt = fileInfo.LastWriteTimeUtc;
            dt = dt.AddHours(adjustTimeHours);
            ret.LastModifiedTime = dt.Ticks;

            if (roundLastModifiedTime)
            {
                ret.LastModifiedTime >>= 2;
            }

            ret.Filepath = fullpath.Substring(stripPrefix.Length);
            ret.Filesize = fileInfo.Length;
            return ret;
        }
    }

    public static class FindMovedFiles
    {
        // finds 'quick' differences. won't see cases where contents differ, but lmt and filesize are the same.
        // returns (only on left, only on right, modified)
        public static Tuple<List<FileEntry>, List<FileEntry>, List<FileEntry>> FindQuickDifferencesByModifiedTimeAndFilesize(SortFilesSettings settings)
        {
            if (settings.SourceDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                throw new CoordinatePicturesException("directory should not end with slash.");
            }

            var onlyOnLeft = new List<FileEntry>();
            var onlyOnRight = new List<FileEntry>();
            var modified = new List<FileEntry>();

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
                    settings.SourceDirectory, settings.AllowFiletimesDifferForFAT, settings.ShiftFiletimeHours);
                FileEntry entryLeft;
                indexFilesLeft.TryGetValue(entryRight.Filepath, out entryLeft);

                if (entryLeft == null)
                {
                    onlyOnRight.Add(entryRight);
                }
                else
                {
                    entryLeft.Flag = true;
                    if (entryLeft.Filesize != entryRight.Filesize || entryLeft.LastModifiedTime != entryRight.LastModifiedTime)
                    {
                        modified.Add(entryLeft);
                    }
                }
            }

            // find files that weren't seen on the right
            foreach (var entryLeft in indexFilesLeft)
            {
                if (!entryLeft.Value.Flag)
                {
                    onlyOnLeft.Add(entryLeft.Value);
                }
            }

            return Tuple.Create(onlyOnLeft, onlyOnRight, modified);
        }
    }

}
