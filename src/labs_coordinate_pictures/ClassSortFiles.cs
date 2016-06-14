using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    // these are our four provided operations.
    public enum SortFilesAction
    {
        SearchDifferences, // see SortFilesSearchDifferences
        SearchDuplicates, // see SortFilesSearchDuplicates
        SearchDuplicatesInOneDir, // see SortFilesSearchDuplicatesInOneDir
        SyncFiles // see SyncFilesWithRobocopy
    }

    // result of comparison, also used as index into an ImageList.
    public enum FileComparisonResultType
    {
        None = 0,
        Changed_File = 1,
        Left_Only = 2,
        Right_Only = 3,
        Same_Contents = 4,
        Moved_File = 5,
    }

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

    // sync files from one directory to another. (all work done by robocopy.exe)
    public static class SyncFilesWithRobocopy
    {
        // for filesystems like FAT that have imprecise last-write-times.
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

    public static class SortFilesSearchDifferencesAndDetectRenames
    {
        public static List<FileComparisonResult> Go(SortFilesSettings settings)
        {
            // renamed files appear as a left-only file and a right-only file with same contents
            // this also helpfully detects files with different lmt but contents are the same
            var differences = SortFilesSearchDifferences.Go(settings);
            var left = from item in differences
                               //where item.Type == FileComparisonResultType.Left_Only
                           where item.FileInfoLeft != null
                           select item.FileInfoLeft;
            var right = from item in differences
                            //where item.Type == FileComparisonResultType.Right_Only
                           where item.FileInfoRight != null
                            select item.FileInfoRight;

            // look for any duplicates between the left-only files and the right-only files
            var duplicates = SortFilesSearchDuplicates.Go(settings,
                FileInfoFromComparison(settings.LeftDirectory, left),
                FileInfoFromComparison(settings.RightDirectory, right));

            // we now want to filter out the duplicates.
            // 1) make a set of filenames already seen.
            var alreadySeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in duplicates)
            {
                alreadySeen.Add(settings.LeftDirectory + item.FileInfoLeft.Filename);
                alreadySeen.Add(settings.RightDirectory + item.FileInfoRight.Filename);
            }

            // 2) filter out any entries referencing those filenames
            // but: what if a changed file is also a duplicate.
            // file A is changed to A', which happens to have the same contents has B.
            // should we show this as a duplicate, or a change, or both?
            // hard to solve this. maybe this feature can't be done elegantly.
            var results = from item in differences
                          where (item.FileInfoLeft == null ||
                            !alreadySeen.Contains(settings.LeftDirectory + item.FileInfoLeft.Filename)) &&
                          (item.FileInfoRight == null ||
                            !alreadySeen.Contains(settings.RightDirectory + item.FileInfoRight.Filename))
                          select item;

            return results.Concat(duplicates).ToList();
        }

        public static IEnumerable<FileInfo> FileInfoFromComparison(string directory,
            IEnumerable<FileInfoForComparison> list)
        {
            foreach (var obj in list)
            {
                var filename = directory + obj.Filename;
                yield return new FileInfo(filename);
            }
        }
    }

    public static class SortFilesSearchDifferences
    {
        // for filesystems like FAT that have imprecise last-write-times.
        public const int AllowDifferSeconds = 4;

        public static List<FileComparisonResult> Go(SortFilesSettings settings,
            bool treatAllFilesAsModified = false)
        {
            var results = new List<FileComparisonResult>();
            var filesInLeft = new Dictionary<string, FileInfoForComparison>(
                StringComparer.OrdinalIgnoreCase);

            // go through files in left
            var diLeft = new DirectoryInfo(settings.LeftDirectory);
            foreach (var info in diLeft.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var filename = info.FullName.Substring(settings.LeftDirectory.Length);
                filesInLeft[filename] = new FileInfoForComparison(
                    filename, info.Length, info.LastWriteTimeUtc);
            }

            // go through files in right
            int branchHit = 0;
            var diRight = new DirectoryInfo(settings.RightDirectory);
            foreach (var info in diRight.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var filenameRight = info.FullName.Substring(settings.RightDirectory.Length);
                FileInfoForComparison objLeft;
                if (filesInLeft.TryGetValue(filenameRight, out objLeft))
                {
                    objLeft.MarkWhenVisited = true;
                    if (objLeft.FileSize != info.Length ||
                        !AreTimesEqual(objLeft.LastModifiedTime, info.LastWriteTimeUtc, settings) ||
                        treatAllFilesAsModified)
                    {
                        // looks like a modified file. same path but different filesize/lmt.
                        var filename = info.FullName.Substring(settings.RightDirectory.Length);
                        var objRight = new FileInfoForComparison(
                            filename, info.Length, info.LastWriteTimeUtc);
                        results.Add(new FileComparisonResult(
                            objLeft, objRight, FileComparisonResultType.Changed_File));
                    }
                    else
                    {
                        branchHit++;
                    }
                }
                else
                {
                    // looks like a new file
                    var objRight = new FileInfoForComparison(
                        filenameRight, info.Length, info.LastWriteTimeUtc);
                    results.Add(new FileComparisonResult(
                        null, objRight, FileComparisonResultType.Right_Only));
                }
            }

            // which files did we see in left but not in right?
            foreach (var kvp in filesInLeft)
            {
                if (!kvp.Value.MarkWhenVisited)
                {
                    // looks like a deleted file since it didn't show up on the right.
                    results.Add(new FileComparisonResult(
                        kvp.Value, null, FileComparisonResultType.Left_Only));
                }
                else
                {
                    branchHit++;
                }
            }

            return results;
        }

        static bool AreTimesEqualHelper(DateTime dt1, DateTime dt2, SortFilesSettings settings)
        {
            int branchHit = 0;
            if (settings.AllowFiletimesDifferForFAT)
            {
                // allow times to differ
                return Math.Abs(dt1.Ticks - dt2.Ticks) <= AllowDifferSeconds * TimeSpan.TicksPerSecond;
            }
            else
            {
                // times must be exact
                var ret = dt1.Ticks == dt2.Ticks;
                if (ret)
                    branchHit++;
                else
                    branchHit--;
                return ret;
            }
        }

        public static bool AreTimesEqual(DateTime dt1, DateTime dt2, SortFilesSettings settings)
        {
            if (settings.AllowFiletimesDifferForDST)
            {
                return AreTimesEqualHelper(dt1, dt2, settings)
                    || AreTimesEqualHelper(dt1, dt2.AddHours(1), settings)
                    || AreTimesEqualHelper(dt1, dt2.AddHours(-1), settings);
            }
            else
            {
                return AreTimesEqualHelper(dt1, dt2, settings);
            }
        }
    }

    public static class SortFilesSearchDuplicates
    {
        public static List<FileComparisonResult> Go(SortFilesSettings settings,
            IEnumerable<FileInfo> filesInLeft,
            IEnumerable<FileInfo> filesInRight)
        {
            // first, just make an index that simply maps filesizes to filenames.
            // we don't need to compute any content-hashes yet, because if there
            // is only one file with that filesize, we know it's not a duplicate.
            var results = new List<FileComparisonResult>();
            var indexLeft = MapFilesizesToFilenames(settings.LeftDirectory, filesInLeft);
            int branchHit = 0;

            // go through files on the right
            foreach (var infoRight in filesInRight)
            {
                List<FileInfoForComparison> list;
                if (indexLeft.TryGetValue(infoRight.Length, out list))
                {
                    // we found another file(s) with the same filesize, so
                    // let's compare hashes of the content to see if they're the same.
                    var hashRight = Utils.GetSha512(infoRight.FullName);
                    foreach (var objLeft in list)
                    {
                        // compute the hash if it hasn't been computed already
                        if (objLeft.ContentHash == null)
                        {
                            objLeft.ContentHash = Utils.GetSha512(
                                settings.LeftDirectory + objLeft.Filename);
                        }
                        else
                        {
                            branchHit++;
                        }

                        if (objLeft.ContentHash == hashRight)
                        {
                            // these are duplicates, they have the same hash and filesize.
                            var filenameRight = infoRight.FullName.Substring(
                                settings.RightDirectory.Length);
                            var objRight = new FileInfoForComparison(
                                filenameRight, infoRight.Length, infoRight.LastWriteTimeUtc, hashRight);
                            results.Add(new FileComparisonResult(
                                objLeft, objRight, FileComparisonResultType.Same_Contents));
                            break;
                        }
                        else
                        {
                            branchHit++;
                        }
                    }
                }
            }

            return results;
        }

        public static List<FileComparisonResult> Go(
            SortFilesSettings settings)
        {
            var diLeft = new DirectoryInfo(settings.LeftDirectory);
            var diRight = new DirectoryInfo(settings.RightDirectory);
            return Go(settings,
                diLeft.EnumerateFiles("*", SearchOption.AllDirectories),
                diRight.EnumerateFiles("*", SearchOption.AllDirectories));
        }

        public static Dictionary<long, List<FileInfoForComparison>> MapFilesizesToFilenames(
            string dirName, IEnumerable<FileInfo> files)
        {
            // map filesize to List<FileInfoForComparison> or HashSet<FileInfoForComparison>?
            // chose List<>; maintaining inserted order makes results that look nicer to the user.
            var map = new Dictionary<long, List<FileInfoForComparison>>();
            int branchHit = 0;
            foreach (var info in files)
            {
                var filename = info.FullName.Substring(dirName.Length);
                var obj = new FileInfoForComparison(
                    filename, info.Length, info.LastWriteTimeUtc);

                List<FileInfoForComparison> list;
                if (!map.TryGetValue(obj.FileSize, out list))
                {
                    list = map[obj.FileSize] = new List<FileInfoForComparison>();
                }
                else
                {
                    branchHit++;
                }

                list.Add(obj);
            }

            return map;
        }
    }

    public static class SortFilesSearchDuplicatesInOneDir
    {
        public static List<FileComparisonResult> Go(SortFilesSettings settings)
        {
            // first, just make an index that simply maps filesizes to filenames.
            // we don't need to compute any content-hashes yet, because if there
            // is only one file with that filesize, we know it's not a duplicate.
            var results = new List<FileComparisonResult>();
            var di = new DirectoryInfo(settings.LeftDirectory);
            int branchHit = 0;
            var index = SortFilesSearchDuplicates.MapFilesizesToFilenames(
                settings.LeftDirectory,
                di.EnumerateFiles("*", SearchOption.AllDirectories));

            foreach (var list in index.Values)
            {
                if (list.Count > 1)
                {
                    // if there's more than one file with the same filesize,
                    // compute hashes of contents to look for duplicates.
                    for (int i = 0; i < list.Count; i++)
                    {
                        list[i].ContentHash = Utils.GetSha512(
                            settings.LeftDirectory + list[i].Filename);

                        // have we seen this hash before? this is an n-squared loop, but
                        // basically amortized by the cost of computing hashes.
                        bool found = false;
                        for (int j = 0; j < i; j++)
                        {
                            if (list[j].ContentHash == list[i].ContentHash)
                            {
                                // consistently put the first-appearing file on the 'left' side
                                // so that the user can conveniently safely delete all on 'right'.
                                results.Add(new FileComparisonResult(
                                    list[j], list[i], FileComparisonResultType.Same_Contents));
                                found = true;
                                break;
                            }
                            else
                            {
                                branchHit++;
                            }
                        }

                        if (i == 0)
                        {
                            branchHit++;
                        }

                        if (found)
                        {
                            branchHit++;
                        }
                        else
                        {
                            branchHit++;
                        }
                    }
                }
                else
                {
                    branchHit++;
                }
            }

            return results;
        }
    }
}
