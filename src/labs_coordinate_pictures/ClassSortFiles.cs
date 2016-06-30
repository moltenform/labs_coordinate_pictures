using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    // our four provided operations.
    public enum SortFilesAction
    {
        SearchDifferences,
        SearchDuplicates,
        SearchDuplicatesInOneDir,
        SyncFiles
    }

    // result of comparison, also used as index into an ImageList.
    public enum FileComparisonResultType
    {
        None = 0,
        Changed = 1,
        Left_Only = 2,
        Right_Only = 3,
        Same_Contents = 4
    }

    // command pattern for file operations.
    interface IUndoableFileOp
    {
        string Source { get; }
        string Dest { get; }

        void Do();
        void Undo();
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
        public bool SearchDuplicatesCanUseFiletimes { get; set; }
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

            string showPath;
            if (type == FileComparisonResultType.Same_Contents &&
                FileInfoLeft.Filename != FileInfoRight.Filename)
            {
                showPath = StripInitialSlash(FileInfoLeft.Filename) + "; " +
                     StripInitialSlash(FileInfoRight.Filename);
            }
            else
            {
                showPath = StripInitialSlash(FileInfoLeft?.Filename ??
                    FileInfoRight.Filename);
            }

            ImageIndex = (int)type;
            SubItems.Add(type.ToString().Replace("_", " "));
            SubItems.Add(showPath);

            if (type == FileComparisonResultType.Changed &&
                fileRight.LastModifiedTime > fileLeft.LastModifiedTime)
            {
                // show a red icon if the file on the right is newer.
                ImageIndex = 5;
            }
        }

        public FileInfoForComparison FileInfoLeft { get; }
        public FileInfoForComparison FileInfoRight { get; }
        public FileComparisonResultType Type { get; }

        static string StripInitialSlash(string s)
        {
            return (s.Length > 0 && s[0] == Utils.PathSep[0]) ?
                s.Substring(1) : s;
        }

        public string GetLeft(string baseDir)
        {
            return FileInfoLeft == null ? null :
                Path.Combine(baseDir, StripInitialSlash(FileInfoLeft.Filename));
        }

        public string GetRight(string baseDir)
        {
            return FileInfoRight == null ? null :
                Path.Combine(baseDir, StripInitialSlash(FileInfoRight.Filename));
        }

        public void SetMarkedAsModifiedInUI(bool p)
        {
            SubItems[0].Text = p ? " *" : "";
        }

        public bool GetMarkedAsModifiedInUI()
        {
            return !string.IsNullOrWhiteSpace(SubItems[0].Text);
        }
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
            args.Add("/E"); // subdirectories, including empty directories
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

        public static void Go(SortFilesSettings settings)
        {
            string unused;
            var workingDir = Environment.SystemDirectory;
            int retcode = Utils.Run("robocopy.exe", GetArgs(settings), shellExecute: false,
                waitForExit: true, hideWindow: true, getStdout: false, outStdout: out unused,
                outStderr: out unused, workingDir: workingDir);

            if (((retcode & 0x08) != 0) || ((retcode & 0x10) != 0))
            {
                Utils.MessageErr("Warning: return code indicates action was not completed, "
                    + retcode.ToString("X"));
            }
            else
            {
                Utils.MessageBox("Complete.");
            }

            // show log results in notepad
            if (File.Exists(settings.LogFile))
            {
                if (settings.PreviewOnly)
                {
                    File.AppendAllText(settings.LogFile, "(Preview Only)",
                        System.Text.Encoding.Unicode);
                }

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
        // for filesystems like FAT that have imprecise last-write-times.
        public const int AllowDifferSeconds = 4;

        public static List<FileComparisonResult> Go(SortFilesSettings settings)
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
            var diRight = new DirectoryInfo(settings.RightDirectory);
            foreach (var info in diRight.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var filenameRight = info.FullName.Substring(settings.RightDirectory.Length);
                FileInfoForComparison objLeft;
                if (filesInLeft.TryGetValue(filenameRight, out objLeft))
                {
                    objLeft.MarkWhenVisited = true;
                    if (objLeft.FileSize != info.Length ||
                        !AreTimesEqual(objLeft.LastModifiedTime, info.LastWriteTimeUtc, settings))
                    {
                        // looks like a modified file. same path but different filesize/lmt.
                        var filename = info.FullName.Substring(settings.RightDirectory.Length);
                        var objRight = new FileInfoForComparison(
                            filename, info.Length, info.LastWriteTimeUtc);
                        results.Add(new FileComparisonResult(
                            objLeft, objRight, FileComparisonResultType.Changed));
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
            }

            return results;
        }

        static bool AreTimesEqualHelper(DateTime dt1, DateTime dt2, SortFilesSettings settings)
        {
            if (settings.AllowFiletimesDifferForFAT)
            {
                // allow times to differ
                return Math.Abs(dt1.Ticks - dt2.Ticks) <=
                    AllowDifferSeconds * TimeSpan.TicksPerSecond;
            }
            else
            {
                // times must be exact
                var ret = dt1.Ticks == dt2.Ticks;
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
        public static List<FileComparisonResult> Go(SortFilesSettings settings)
        {
            var filesInLeft = new DirectoryInfo(settings.LeftDirectory).EnumerateFiles(
                "*", SearchOption.AllDirectories);
            var filesInRight = new DirectoryInfo(settings.RightDirectory).EnumerateFiles(
                "*", SearchOption.AllDirectories);

            // first, just make an index that simply maps filesizes to filenames.
            // we don't need to compute any content-hashes yet, because if there
            // is only one file with that filesize, we know it's not a duplicate.
            var results = new List<FileComparisonResult>();
            var indexLeft = MapFilesizesToFilenames(settings.LeftDirectory, filesInLeft);

            // go through files on the right
            foreach (var infoRight in filesInRight)
            {
                var filenameRight = infoRight.FullName.Substring(
                     settings.RightDirectory.Length);
                var objLeft = FindInMap(indexLeft, settings.LeftDirectory, infoRight.FullName,
                    infoRight.Length, settings.SearchDuplicatesCanUseFiletimes,
                    infoRight.LastWriteTimeUtc, filenameRight);

                if (objLeft != null)
                {
                    // these are duplicates, they have the same hash and filesize.
                    var objRight = new FileInfoForComparison(filenameRight,
                        infoRight.Length, infoRight.LastWriteTimeUtc, objLeft.ContentHash);
                    results.Add(new FileComparisonResult(
                        objLeft, objRight, FileComparisonResultType.Same_Contents));
                }
            }

            return results;
        }

        public static Dictionary<long, List<FileInfoForComparison>> MapFilesizesToFilenames(
            string dirName, IEnumerable<FileInfo> files)
        {
            // map filesize to List<FileInfoForComparison> or HashSet<FileInfoForComparison>?
            // chose List<>; maintaining inserted order makes results that look nicer to the user.
            var map = new Dictionary<long, List<FileInfoForComparison>>();
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

                list.Add(obj);
            }

            return map;
        }

        static FileInfoForComparison FindInMap(Dictionary<long, List<FileInfoForComparison>> map,
            string baseDir, string fullnameToFind, long lengthOfFileToFind,
            bool useLmtShortcut, DateTime lmt, string partialFilename)
        {
            if (lengthOfFileToFind == 0)
            {
                // 0-length files compare unequal; often placeholders intentionally created by user
                return null;
            }

            List<FileInfoForComparison> list;
            if (map.TryGetValue(lengthOfFileToFind, out list))
            {
                // look for an entry with the same filename
                FileInfoForComparison hasSameName = null;
                foreach (var obj in list)
                {
                    if (obj.Filename == partialFilename)
                    {
                        hasSameName = obj;
                        break;
                    }
                }

                // if enabled, treat files with the same filesize, lmt, and name as equal
                if (useLmtShortcut && hasSameName != null &&
                    hasSameName.LastModifiedTime == lmt)
                {
                    return hasSameName;
                }

                // change order so that an entry with the same name is checked first. why?
                // 1) results look nicer when paired this way
                // 2) same order of results whether or not useLmtShortcut is on
                // (as long as lmt times are accurate). Consider the case with two duplicates,
                // one with the same name, and one that sorts earlier.
                // We prefer to pick the file with the same name to show as the duplicate.
                if (hasSameName != null)
                {
                    list = new List<FileInfoForComparison>(list);
                    list.Insert(0, hasSameName);
                }

                // we found another file(s) with the same filesize, so
                // let's compare hashes of the content to see if they're the same.
                var hash = Utils.GetSha512(fullnameToFind);
                foreach (var obj in list)
                {
                    // compute the hash if it hasn't been computed already, then cache it
                    if (obj.ContentHash == null)
                    {
                        obj.ContentHash = Utils.GetSha512(
                            baseDir + obj.Filename);
                    }

                    if (obj.ContentHash == hash)
                    {
                        return obj;
                    }
                }
            }

            return null;
        }

        public static List<Tuple<FileComparisonResult, string>> SearchMovedFiles(
            string leftDirectory, string rightDirectory,
            IEnumerable<FileComparisonResult> query)
        {
            // We'll go through every deleted file (exists on the Left
            // but not the Right) and see if it is just the result of a
            // moved or renamed file (a file with same contents already exists on Right)
            var filesInRight = new DirectoryInfo(rightDirectory).EnumerateFiles(
                "*", SearchOption.AllDirectories);
            var map = MapFilesizesToFilenames(rightDirectory, filesInRight);
            var results = new List<Tuple<FileComparisonResult, string>>();

            foreach (var item in query)
            {
                var objSameContents = FindInMap(map, rightDirectory,
                    leftDirectory + item.FileInfoLeft.Filename, item.FileInfoLeft.FileSize,
                    false, DateTime.MinValue, null);
                if (objSameContents != null)
                {
                    // these are duplicates, they have the same hash and filesize.
                    results.Add(Tuple.Create(item, objSameContents.Filename));
                }
            }

            return results;
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
                        for (int j = 0; j < i; j++)
                        {
                            if (list[j].ContentHash == list[i].ContentHash)
                            {
                                // consistently put the first-appearing file on the 'left' side
                                // so that the user can conveniently safely delete all on 'right'.
                                results.Add(new FileComparisonResult(
                                    list[j], list[i], FileComparisonResultType.Same_Contents));
                                break;
                            }
                        }
                    }
                }
            }

            return results;
        }
    }
}
