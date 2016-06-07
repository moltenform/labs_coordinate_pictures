using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    // models what is shown on the FormSortFiles form.
    public class SortFilesSettings
    {
        public SortFilesSettings()
        {
            SkipDirectories = new List<string>();
            SkipFiles = new List<string>();
        }

        public string LeftDirectory { get; set; }
        public string RightDirectory { get; set; }
        public bool AllowFiletimesDifferForFAT { get; set; }
        public bool AllowFiletimesDifferForDST { get; set; }
        public bool Mirror { get; set; }
        public bool PreviewOnly { get; set; }
        public string LogFile { get; set; }
        public List<string> SkipDirectories { get; }
        public List<string> SkipFiles { get; }
    }

    // will be shown in FormSortFilesList UI.
    public class FileComparisonResult : ListViewItem
    {
        public FileComparisonResult(FileInfoForComparison fileLeft,
            FileInfoForComparison fileRight,
            FileComparisonResultType type)
        {
            FileInfoLeft = fileLeft;
            FileInfoRight = fileRight;
            Type = type;
        }

        public FileInfoForComparison FileInfoLeft { get; }
        public FileInfoForComparison FileInfoRight { get; }
        public FileComparisonResultType Type { get; }
    }

    // caches a file's information and content-hash.
    // the FileInfo class, in contrast, doesn't always cache its properties.
    public class FileInfoForComparison
    {
        public FileInfoForComparison(string filename,
            long filesize, DateTime lastModifiedTime,
            string contentHash = null)
        {
            Filename = filename;
            FileSize = filesize;
            LastModifiedTime = lastModifiedTime;
            ContentHash = contentHash;
        }

        // filename is relative to root directory.
        public string Filename { get; }
        public long FileSize { get; }
        public DateTime LastModifiedTime { get; }
        public bool MarkWhenVisited { get; set; }
        public string ContentHash { get; set; }
    }

    // result of comparison. numeric value used as index of an ImageList.
    public enum FileComparisonResultType
    {
        None = 0,
        Changed_File = 1,
        Left_Only = 2,
        Right_Only = 3,
        Same_Contents = 4,
        Moved_File = 5,
    }

    // these are our four provided operations.
    public enum SortFilesAction
    {
        SearchDifferences, // see SortFilesSearchDifferences
        SearchDuplicates, // see SortFilesSearchDuplicates
        SearchDuplicatesInOneDir, // see SortFilesSearchDuplicatesInOneDir
        SyncFiles // see SyncFilesWithRobocopy
    }

    // sync files from one directory to another. (all work done by robocopy.exe)
    public static class SyncFilesWithRobocopy
    {
        // for FAT systems with imprecise last-write-times.
        public const int AllowDifferSeconds = 2;

        public static string GetFullArgs(SortFilesSettings settings)
        {
            var args = Utils.CombineProcessArguments(GetArgs(settings));
            return "robocopy.exe " + args;
        }

        public static string[] GetArgs(SortFilesSettings settings)
        {
            var args = new List<string>();
            args.Add(settings.LeftDirectory);
            args.Add(settings.RightDirectory);
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
            string unused;
            var workingDir = Environment.SystemDirectory;
            int retcode = Utils.Run("robocopy.exe", GetArgs(settings), shellExecute: false,
                waitForExit: true, hideWindow: true, getStdout: false, outStdout: out unused,
                outStderr: out unused, workingDir: workingDir);

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
                    waitForExit: false, hideWindow: false, getStdout: false, outStdout: out unused,
                    outStderr: out unused, workingDir: workingDir);
            }
            else
            {
                Utils.MessageErr("Log file not found.");
            }
        }
    }

    public static class SortFilesSearchDifferences
    {
        // for FAT systems with imprecise last-write-times.
        public const int AllowDifferSeconds = 4;

        public static List<FileComparisonResult> SearchDifferences(SortFilesSettings settings)
        {
            var ret = new List<FileComparisonResult>();
            Dictionary<string, FileInfoForComparison> filesLeft =
                new Dictionary<string, FileInfoForComparison>(StringComparer.OrdinalIgnoreCase);

            // go through files in left
            var diLeft = new DirectoryInfo(settings.LeftDirectory);
            foreach (var info in diLeft.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var filename = info.FullName.Substring(settings.LeftDirectory.Length);
                filesLeft[filename] = new FileInfoForComparison(
                    filename, info.Length, info.LastWriteTimeUtc);
            }

            // go through files in right
            var diRight = new DirectoryInfo(settings.RightDirectory);
            foreach (var info in diRight.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                FileInfoForComparison objLeft;
                if (filesLeft.TryGetValue(info.FullName, out objLeft))
                {
                    objLeft.MarkWhenVisited = true;
                    if (objLeft.FileSize != info.Length ||
                        !AreTimesTheSame(objLeft.LastModifiedTime, info.LastWriteTimeUtc, settings))
                    {
                        // looks like a modified file. same path but different filesize/lmt.
                        var filename = info.FullName.Substring(settings.RightDirectory.Length);
                        var objRight = new FileInfoForComparison(
                            filename, info.Length, info.LastWriteTimeUtc);
                        ret.Add(new FileComparisonResult(
                            objLeft, objRight, FileComparisonResultType.Changed_File));
                    }
                }
                else
                {
                    // looks like a new file
                    var filename = info.FullName.Substring(settings.RightDirectory.Length);
                    var objRight = new FileInfoForComparison(
                        filename, info.Length, info.LastWriteTimeUtc);
                    ret.Add(new FileComparisonResult(
                        null, objRight, FileComparisonResultType.Right_Only));
                }
            }

            // which files did we see in left but not in right?
            foreach (var kvp in filesLeft)
            {
                if (!kvp.Value.MarkWhenVisited)
                {
                    // looks like a deleted file since it didn't show up on the right.
                    ret.Add(new FileComparisonResult(
                        kvp.Value, null, FileComparisonResultType.Left_Only));
                }
            }

            return ret;
        }

        public static bool AreTimesTheSameHelper(DateTime dt1, DateTime dt2, SortFilesSettings settings)
        {
            if (settings.AllowFiletimesDifferForFAT)
            {
                // allow times to differ
                return Math.Abs(dt1.Ticks - dt2.Ticks) <= AllowDifferSeconds * TimeSpan.TicksPerSecond;
            }
            else
            {
                // times must be exact
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

    public static class SortFilesSearchDuplicates
    {
        public static Dictionary<long, List<FileInfoForComparison>> CreateMappingFilesizeToFilename(
            string dirName, IEnumerable<FileInfo> files)
        {
            // there can be many files with the same size, so it needs to map from size to a collection
            // use a List instead of a Dict for these small collections, because
            // maintaining inserted order makes results that are more intuitive to the user.
            var ret = new Dictionary<long, List<FileInfoForComparison>>();
            foreach (var info in files)
            {
                var filename = info.FullName.Substring(dirName.Length);
                var obj = new FileInfoForComparison(
                    filename, info.Length, info.LastWriteTimeUtc);

                List<FileInfoForComparison> list;
                if (!ret.TryGetValue(obj.FileSize, out list))
                {
                    list = ret[obj.FileSize] = new List<FileInfoForComparison>();
                }

                list.Add(obj);
            }

            return ret;
        }

        public static List<FileComparisonResult> SearchDuplicatesAcrossDirectories(
            SortFilesSettings settings, IEnumerable<FileInfo> leftFiles, IEnumerable<FileInfo> rightFiles)
        {
            var ret = new List<FileComparisonResult>();
            var indexLeft = CreateMappingFilesizeToFilename(settings.LeftDirectory, leftFiles);

            foreach (var info in rightFiles)
            {
                List<FileInfoForComparison> list;
                if (indexLeft.TryGetValue(info.Length, out list))
                {
                    var hashRight = Utils.GetSha512(info.FullName);
                    foreach (var listitem in list)
                    {
                        if (listitem.ContentHash == null)
                        {
                            listitem.ContentHash = Utils.GetSha512(Path.Combine(
                                settings.LeftDirectory, listitem.Filename));
                        }

                        if (listitem.ContentHash == hashRight)
                        {
                            var filenameRight = info.FullName.Substring(settings.RightDirectory.Length);
                            var objRight = new FileInfoForComparison(
                                filenameRight, info.Length, info.LastWriteTimeUtc, hashRight);
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
            var di = new DirectoryInfo(settings.LeftDirectory);
            var index = CreateMappingFilesizeToFilename(settings.LeftDirectory,
                di.EnumerateFiles("*", SearchOption.AllDirectories));

            foreach (var list in index.Values)
            {
                if (list.Count > 1)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        list[i].ContentHash = Utils.GetSha512(Path.Combine(
                            settings.LeftDirectory, list[i].Filename));

                        // have we seen this hash before?
                        // this is an n squared loop, but calculating content hashes is far more expensive.
                        for (int j = 0; j < i; j++)
                        {
                            if (list[j].ContentHash == list[i].ContentHash)
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

        public static List<FileComparisonResult> SearchDuplicatesAcrossDirectories(
            SortFilesSettings settings)
        {
            var diLeft = new DirectoryInfo(settings.LeftDirectory);
            var diRight = new DirectoryInfo(settings.LeftDirectory);
            return SearchDuplicatesAcrossDirectories(settings,
                diLeft.EnumerateFiles("*", SearchOption.AllDirectories),
                diRight.EnumerateFiles("*", SearchOption.AllDirectories));
        }
    }

    public static class SortFilesSearchDifferencesAndDetectRenames
    {
        public static IEnumerable<FileInfo> GetFileInfos(string directory,
            List<FileInfoForComparison> list)
        {
            foreach (var obj in list)
            {
                var filename = Path.Combine(directory, obj.Filename);
                yield return new FileInfo(filename);
            }
        }

        public static List<FileComparisonResult> SearchDifferencesAndDetectRenames(SortFilesSettings settings)
        {
            var differences = SortFilesSearchDifferences.SearchDifferences(settings);

            // separate the list based on what type of action.
            var listLeftOnly = new List<FileInfoForComparison>();
            var listRightOnly = new List<FileInfoForComparison>();
            foreach (var obj in differences)
            {
                if (obj.Type == FileComparisonResultType.Left_Only)
                {
                    listLeftOnly.Add(obj.FileInfoLeft);
                }
                else if (obj.Type == FileComparisonResultType.Right_Only)
                {
                    listRightOnly.Add(obj.FileInfoRight);
                }
            }

            // look for any duplicates between the left-only files and the right-only files
            var duplicates = SortFilesSearchDuplicates.SearchDuplicatesAcrossDirectories(settings,
                GetFileInfos(settings.LeftDirectory, listLeftOnly),
                GetFileInfos(settings.RightDirectory, listRightOnly));

            // make a dictionary marking everything we saw that was a duplicate
            var dictAlreadySeenLeft = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dictAlreadySeenRight = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in duplicates)
            {
                dictAlreadySeenLeft.Add(item.FileInfoLeft.Filename);
                dictAlreadySeenRight.Add(item.FileInfoRight.Filename);
            }

            var results = from item in differences
                          where ((item.FileInfoLeft == null || !dictAlreadySeenLeft.Contains(item.FileInfoLeft.Filename))
                          &&
                          (item.FileInfoRight == null || !dictAlreadySeenRight.Contains(item.FileInfoRight.Filename)))
                          select item;

            return results.Concat(duplicates).ToList();
        }
    }


}
