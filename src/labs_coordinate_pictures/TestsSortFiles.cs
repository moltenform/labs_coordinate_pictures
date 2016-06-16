using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    [Serializable]
    public class CoordinatePicturesTestException : Exception
    {
        public CoordinatePicturesTestException(string message, Exception e)
            : base("CoordinatePicturesTestException " + message, e)
        {
        }

        public CoordinatePicturesTestException(string message)
            : this(message, null)
        {
        }

        public CoordinatePicturesTestException()
            : this("", null)
        {
        }

        protected CoordinatePicturesTestException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }

    public static class TestUtil
    {
        // used to represent null, not accessible outside this class.
        static object nullToken = new object();

        public static void IsEq(object expected, object actual)
        {
            // use a token to make sure that IsEq(null, null) works.
            expected = expected ?? nullToken;
            actual = actual ?? nullToken;

            if (!expected.Equals(actual))
            {
                throw new CoordinatePicturesTestException(
                    "Assertion failure, expected " + expected + " but got " + actual);
            }
        }

        public static void IsTrue(bool actual)
        {
            IsEq(true, actual);
        }

        public static void IsStringArrayEq(string expected, IList<string> actual)
        {
            if (expected == null)
            {
                IsTrue(actual == null || actual.Count == 0);
            }
            else
            {
                var expectedSplit = expected.Split(new char[] { '|' });
                IsEq(expectedSplit.Length, actual.Count);
                for (int i = 0; i < expectedSplit.Length; i++)
                {
                    IsEq(expectedSplit[i], actual[i]);
                }
            }
        }

        // expect an exception to occur when running the action,
        // the exception should have the string in its message.
        public static void AssertExceptionMessage(Action fn, string expectExceptionMessage)
        {
            string exceptionMessage = null;
            try
            {
                fn();
            }
            catch (Exception exc)
            {
                exceptionMessage = exc.ToString();
            }

            if (exceptionMessage == null || !exceptionMessage.Contains(expectExceptionMessage))
            {
                throw new CoordinatePicturesTestException(
                    "Testing.AssertExceptionMessageIncludes expected " +
                    expectExceptionMessage + " but got " + exceptionMessage + ".");
            }
        }

        // use reflection to call all methods that start with TestMethod_
        public static void CallAllTestMethods(Type type, object[] arParams)
        {
            MethodInfo[] methodInfos = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
            var sortedMethods = methodInfos.OrderBy(item => item.Name);
            foreach (MethodInfo methodInfo in sortedMethods)
            {
                if (methodInfo.Name.StartsWith("TestMethod_", StringComparison.InvariantCulture))
                {
                    TestUtil.IsTrue(methodInfo.GetParameters().Length == 0);
                    methodInfo.Invoke(null, arParams);
                }
            }
        }

        public static string GetTestWriteDirectory()
        {
            string directory = Path.Combine(Path.GetTempPath(), "test_labs_coordinate_pictures");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return directory;
        }

        public static string GetTestSubDirectory(string name)
        {
            string directory = Path.Combine(GetTestWriteDirectory(), name);
            Directory.CreateDirectory(directory);
            return directory;
        }

        public static void RunTests()
        {
            if (Utils.GetSoftDeleteDestination("abc" + Utils.PathSep + "abc") == null)
            {
                Utils.MessageBox("Skipping tests until a trash directory is chosen.");
                return;
            }

            string dir = TestUtil.GetTestWriteDirectory();
            Directory.Delete(dir, true);
            Configs.Current.SuppressDialogs = true;
            try
            {
                TestUtil.CallAllTestMethods(typeof(CoordinatePicturesTests), null);
                TestUtil.CallAllTestMethods(typeof(CoordinateFilesTests), null);
            }
            finally
            {
                Configs.Current.SuppressDialogs = false;
            }
        }
    }

    public static class CreateFileCombinations
    {
        enum ModifiedTimes {
            SmTime,
            MTime
        }

        enum Content {
            SmText,
            AltText,
            AddText
        }

        enum Filename {
            SmName,
            MName
        }

        enum ExtraCopies {
            None,
            OneOnLeft,
            OneOnRight
        }

        public static int Go(string dirLeft, string dirRight)
        {
            int filesCreated = 0;
            var baseTime = DateTime.Now;
            foreach (ModifiedTimes m in Enum.GetValues(typeof(ModifiedTimes)))
            {
                foreach (Content c in Enum.GetValues(typeof(Content)))
                {
                    foreach (Filename f in Enum.GetValues(typeof(Filename)))
                    {
                        foreach (ExtraCopies copies in Enum.GetValues(typeof(ExtraCopies)))
                        {
                            filesCreated += Go(dirLeft, dirRight, baseTime, m, c, f, copies);
                        }
                    }
                }
            }

            return filesCreated;
        }

        public static int CountPossibleModifiedTimes()
        {
            return Enum.GetValues(typeof(ModifiedTimes)).Length;
        }

        public static int CountPossibleContents()
        {
            return Enum.GetValues(typeof(Content)).Length;
        }

        public static int CountPossibleFilenames()
        {
            return Enum.GetValues(typeof(Filename)).Length;
        }

        static int Go(string dirLeft, string dirRight, DateTime baseTime,
            ModifiedTimes m, Content c, Filename f, ExtraCopies copies)
        {
            var baseName = m.ToString() + c.ToString() + f.ToString() + copies.ToString();
            var fileLeft = Path.Combine(
                dirLeft, baseName + ".a");
            var fileRight = Path.Combine(
                dirRight, baseName + (f == Filename.MName ? ".z" : ".a"));

            WriteFiles(fileLeft, fileRight, baseTime, m, c);
            switch (copies)
            {
                case ExtraCopies.OneOnLeft:
                    File.Copy(fileLeft, fileLeft + "_1");
                    break;
                case ExtraCopies.OneOnRight:
                    File.Copy(fileRight, fileRight + "_1");
                    break;
            }

            return copies == ExtraCopies.None ? 2 : 3;
        }

        static void WriteFiles(string fileLeft, string fileRight,
            DateTime baseTime, ModifiedTimes m, Content c)
        {
            var addTime = (m == ModifiedTimes.MTime) ? 1 : 0;
            var contentsLeft = fileLeft;
            var contentsRight = contentsLeft;
            if (c == Content.AltText)
            {
                // replace first char with *
                contentsRight = "*" + contentsRight.Substring(1);
            }
            else if (c == Content.AddText)
            {
                contentsRight += "***";
            }

            TestUtil.IsTrue(!File.Exists(fileLeft) && !File.Exists(fileRight));
            File.WriteAllText(fileLeft, contentsLeft);
            File.SetLastWriteTimeUtc(fileLeft, baseTime);
            File.WriteAllText(fileRight, contentsRight);
            File.SetLastWriteTimeUtc(fileRight, baseTime.AddHours(addTime));
        }
    }

    public static class CoordinateFilesTests
    {
        static void TestMethod_ValidateGoodFilesSettings()
        {
            var dirFirst = TestUtil.GetTestSubDirectory("first");
            var dirSecond = TestUtil.GetTestSubDirectory("second");
            var settings = FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                dirFirst, dirSecond, true, true, false, false);

            TestUtil.IsEq(true, settings.AllowFiletimesDifferForFAT);
            TestUtil.IsEq(true, settings.AllowFiletimesDifferForDST);
            TestUtil.IsEq(dirSecond, settings.RightDirectory);
            TestUtil.IsTrue(Directory.Exists(Path.GetDirectoryName(settings.LogFile)));
            TestUtil.IsEq(false, settings.Mirror);
            TestUtil.IsEq(false, settings.PreviewOnly);
            TestUtil.IsStringArrayEq(null, settings.SkipDirectories);
            TestUtil.IsStringArrayEq(null, settings.SkipFiles);
            TestUtil.IsEq(dirFirst, settings.LeftDirectory);

            settings = FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "a", "a\nb b\n\nc\n\n ",
                dirSecond, dirFirst, false, false, true, true);

            TestUtil.IsEq(false, settings.AllowFiletimesDifferForFAT);
            TestUtil.IsEq(false, settings.AllowFiletimesDifferForDST);
            TestUtil.IsEq(dirFirst, settings.RightDirectory);
            TestUtil.IsTrue(Directory.Exists(Path.GetDirectoryName(settings.LogFile)));
            TestUtil.IsEq(true, settings.Mirror);
            TestUtil.IsEq(true, settings.PreviewOnly);
            TestUtil.IsStringArrayEq("a", settings.SkipDirectories);
            TestUtil.IsStringArrayEq("a|b b|c", settings.SkipFiles);
            TestUtil.IsEq(dirSecond, settings.LeftDirectory);
        }

        static void TestMethod_RejectBadFilesSettings()
        {
            var dirFirst = TestUtil.GetTestSubDirectory("first");
            var dirSecond = TestUtil.GetTestSubDirectory("second");

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                Path.Combine(dirFirst, "notexist"), dirSecond, true, true, true, true));

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                dirFirst, Path.Combine(dirSecond, "notexist"), true, true, true, true));

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                dirFirst + Utils.PathSep, dirSecond, true, true, true, true));

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                dirFirst, dirSecond + Utils.PathSep, true, true, true, true));

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                dirFirst, dirFirst, true, true, true, true));

            Directory.CreateDirectory(Path.Combine(dirFirst, "sub"));
            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                dirFirst, Path.Combine(dirFirst, "sub"), true, true, true, true));

            // valid for dest to be empty if action is FindDupeFilesInOneDir
            TestUtil.IsTrue(FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicatesInOneDir, "", "",
                dirFirst, "", true, false, true, true) != null);
        }

        static void TestMethod_AreTimesEqual()
        {
            var time = DateTime.Now;
            var timePlus3s = time.AddSeconds(3);
            var timePlus1hr = time.AddHours(1);

            // strict compare
            var settings = new SortFilesSettings();
            settings.AllowFiletimesDifferForDST = false;
            settings.AllowFiletimesDifferForFAT = false;
            TestUtil.IsEq(true, SortFilesSearchDifferences.AreTimesEqual(time, time, settings));
            TestUtil.IsEq(false, SortFilesSearchDifferences.AreTimesEqual(time, timePlus3s, settings));
            TestUtil.IsEq(false, SortFilesSearchDifferences.AreTimesEqual(timePlus3s, time, settings));
            TestUtil.IsEq(false, SortFilesSearchDifferences.AreTimesEqual(time, timePlus1hr, settings));
            TestUtil.IsEq(false, SortFilesSearchDifferences.AreTimesEqual(timePlus1hr, time, settings));

            // allow DST
            settings.AllowFiletimesDifferForDST = true;
            TestUtil.IsEq(true, SortFilesSearchDifferences.AreTimesEqual(time, time, settings));
            TestUtil.IsEq(false, SortFilesSearchDifferences.AreTimesEqual(time, timePlus3s, settings));
            TestUtil.IsEq(false, SortFilesSearchDifferences.AreTimesEqual(timePlus3s, time, settings));
            TestUtil.IsEq(true, SortFilesSearchDifferences.AreTimesEqual(time, timePlus1hr, settings));
            TestUtil.IsEq(true, SortFilesSearchDifferences.AreTimesEqual(timePlus1hr, time, settings));

            // allow close
            settings.AllowFiletimesDifferForFAT = true;
            TestUtil.IsEq(true, SortFilesSearchDifferences.AreTimesEqual(time, time, settings));
            TestUtil.IsEq(true, SortFilesSearchDifferences.AreTimesEqual(time, timePlus3s, settings));
            TestUtil.IsEq(true, SortFilesSearchDifferences.AreTimesEqual(timePlus3s, time, settings));
            TestUtil.IsEq(true, SortFilesSearchDifferences.AreTimesEqual(time, timePlus1hr, settings));
            TestUtil.IsEq(true, SortFilesSearchDifferences.AreTimesEqual(timePlus1hr, time, settings));

            // disallow DST
            settings.AllowFiletimesDifferForDST = false;
            TestUtil.IsEq(true, SortFilesSearchDifferences.AreTimesEqual(time, time, settings));
            TestUtil.IsEq(true, SortFilesSearchDifferences.AreTimesEqual(time, timePlus3s, settings));
            TestUtil.IsEq(true, SortFilesSearchDifferences.AreTimesEqual(timePlus3s, time, settings));
            TestUtil.IsEq(false, SortFilesSearchDifferences.AreTimesEqual(time, timePlus1hr, settings));
            TestUtil.IsEq(false, SortFilesSearchDifferences.AreTimesEqual(timePlus1hr, time, settings));
        }

        static void TestMethod_MapFilesizesToFilenames()
        {
            var dirTest = TestUtil.GetTestSubDirectory("testMapFilesizesToFilenames");
            File.WriteAllText(Path.Combine(dirTest, "a.txt"), "abcd");
            File.WriteAllText(Path.Combine(dirTest, "b.txt"), "abcde");
            File.WriteAllText(Path.Combine(dirTest, "c.txt"), "1234");

            // adjust the lmt of c.txt
            File.SetLastWriteTimeUtc(Path.Combine(dirTest, "c.txt"), DateTime.Now.AddDays(1));

            var map = SortFilesSearchDuplicates.MapFilesizesToFilenames(dirTest,
                new DirectoryInfo(dirTest).EnumerateFiles("*"));
            var mapSorted = (from item in map[4] orderby item.Filename select item).ToArray();
            TestUtil.IsEq(2, map.Count);
            TestUtil.IsEq(2, map[4].Count);
            TestUtil.IsEq(1, map[5].Count);

            // test that FileInfoForComparison was set correctly
            TestUtil.IsEq(Utils.PathSep + "a.txt", mapSorted[0].Filename);
            TestUtil.IsEq(null, mapSorted[0].ContentHash);
            TestUtil.IsEq(4L, mapSorted[0].FileSize);
            TestUtil.IsEq(File.GetLastWriteTimeUtc(Path.Combine(dirTest, "a.txt")),
                mapSorted[0].LastModifiedTime);

            TestUtil.IsEq(Utils.PathSep + "b.txt", map[5][0].Filename);
            TestUtil.IsEq(null, map[5][0].ContentHash);
            TestUtil.IsEq(5L, map[5][0].FileSize);
            TestUtil.IsEq(File.GetLastWriteTimeUtc(Path.Combine(dirTest, "b.txt")),
                map[5][0].LastModifiedTime);

            TestUtil.IsEq(Utils.PathSep + "c.txt", mapSorted[1].Filename);
            TestUtil.IsEq(null, mapSorted[1].ContentHash);
            TestUtil.IsEq(4L, mapSorted[1].FileSize);
            TestUtil.IsEq(File.GetLastWriteTimeUtc(Path.Combine(dirTest, "c.txt")),
                mapSorted[1].LastModifiedTime);
        }

        static string ResultsToString(IEnumerable<FileComparisonResult> list)
        {
            var lines = from item in list
                        orderby item.FileInfoLeft?.Filename,
                        item.FileInfoRight?.Filename
                        select
                        (item.FileInfoLeft?.Filename ?? "") + "|" +
                        (item.FileInfoRight?.Filename ?? "") + "|" +
                        item.Type.ToString();
            return string.Join(Utils.NL, lines);
        }

        static void CompareResultsToString(List<FileComparisonResult> list, string expected)
        {
            var received = ResultsToString(list).Replace("\\", "").Replace(
                "_File", "").Replace("_Only", "").Replace("\r\n", "\n");
            expected = expected.Replace("\r\n", "\n");
            TestUtil.IsEq(expected, received);
        }

        static int CountFilenames(string s)
        {
            // valid if each filename has 1 and only 1 file extension.
            return (from c in s where c == '.' select c).Count();
        }

        static void TestMethod_TestSortFilesOperations()
        {
            // run the methods on actual files. first create combinations of modified/not modified.
            var settings = new SortFilesSettings();
            settings.LeftDirectory = TestUtil.GetTestSubDirectory("left_fndmved");
            settings.RightDirectory = TestUtil.GetTestSubDirectory("right_fndmved");
            var filesCreated = CreateFileCombinations.Go(settings.LeftDirectory, settings.RightDirectory);
            TestUtil.IsEq(
                CreateFileCombinations.CountPossibleModifiedTimes() *
                CreateFileCombinations.CountPossibleContents() *
                CreateFileCombinations.CountPossibleFilenames() *
                ((1 * 2) + (3 * 2)), // ExtraCopies.None -> 2 files, the rest -> 3 files
                filesCreated);

            // search for duplicates in one directory. The only ones it will find are the 'extra copy on left.'
            var results = SortFilesSearchDuplicatesInOneDir.Go(settings);
            TestUtil.IsEq(
                CreateFileCombinations.CountPossibleModifiedTimes() *
                CreateFileCombinations.CountPossibleContents() *
                CreateFileCombinations.CountPossibleFilenames(), results.Count);

            // verify sort order. for each pair, the left side should be the one that sorts first alphabetically
            var expectedDuplicates =
@"MTimeAddTextMNameOneOnLeft.a|MTimeAddTextMNameOneOnLeft.a_1|Same_Contents
MTimeAddTextSmNameOneOnLeft.a|MTimeAddTextSmNameOneOnLeft.a_1|Same_Contents
MTimeAltTextMNameOneOnLeft.a|MTimeAltTextMNameOneOnLeft.a_1|Same_Contents
MTimeAltTextSmNameOneOnLeft.a|MTimeAltTextSmNameOneOnLeft.a_1|Same_Contents
MTimeSmTextMNameOneOnLeft.a|MTimeSmTextMNameOneOnLeft.a_1|Same_Contents
MTimeSmTextSmNameOneOnLeft.a|MTimeSmTextSmNameOneOnLeft.a_1|Same_Contents
SmTimeAddTextMNameOneOnLeft.a|SmTimeAddTextMNameOneOnLeft.a_1|Same_Contents
SmTimeAddTextSmNameOneOnLeft.a|SmTimeAddTextSmNameOneOnLeft.a_1|Same_Contents
SmTimeAltTextMNameOneOnLeft.a|SmTimeAltTextMNameOneOnLeft.a_1|Same_Contents
SmTimeAltTextSmNameOneOnLeft.a|SmTimeAltTextSmNameOneOnLeft.a_1|Same_Contents
SmTimeSmTextMNameOneOnLeft.a|SmTimeSmTextMNameOneOnLeft.a_1|Same_Contents
SmTimeSmTextSmNameOneOnLeft.a|SmTimeSmTextSmNameOneOnLeft.a_1|Same_Contents";
            CompareResultsToString(results, expectedDuplicates);

            // search for duplicates across directories
            // should find all files on the right marked 'SmText'.
            results = SortFilesSearchDuplicates.Go(settings);
            var countExpectedDuplicates = (from filename in Directory.EnumerateFiles(settings.RightDirectory)
                                           where filename.Contains("SmText")
                                           select filename).Count();
            TestUtil.IsEq(countExpectedDuplicates, results.Count);

            // verify sort order
            expectedDuplicates =
@"MTimeSmTextMNameNone.a|MTimeSmTextMNameNone.z|Same_Contents
MTimeSmTextMNameOneOnLeft.a|MTimeSmTextMNameOneOnLeft.z|Same_Contents
MTimeSmTextMNameOneOnRight.a|MTimeSmTextMNameOneOnRight.z|Same_Contents
MTimeSmTextMNameOneOnRight.a|MTimeSmTextMNameOneOnRight.z_1|Same_Contents
MTimeSmTextSmNameNone.a|MTimeSmTextSmNameNone.a|Same_Contents
MTimeSmTextSmNameOneOnLeft.a|MTimeSmTextSmNameOneOnLeft.a|Same_Contents
MTimeSmTextSmNameOneOnRight.a|MTimeSmTextSmNameOneOnRight.a|Same_Contents
MTimeSmTextSmNameOneOnRight.a|MTimeSmTextSmNameOneOnRight.a_1|Same_Contents
SmTimeSmTextMNameNone.a|SmTimeSmTextMNameNone.z|Same_Contents
SmTimeSmTextMNameOneOnLeft.a|SmTimeSmTextMNameOneOnLeft.z|Same_Contents
SmTimeSmTextMNameOneOnRight.a|SmTimeSmTextMNameOneOnRight.z|Same_Contents
SmTimeSmTextMNameOneOnRight.a|SmTimeSmTextMNameOneOnRight.z_1|Same_Contents
SmTimeSmTextSmNameNone.a|SmTimeSmTextSmNameNone.a|Same_Contents
SmTimeSmTextSmNameOneOnLeft.a|SmTimeSmTextSmNameOneOnLeft.a|Same_Contents
SmTimeSmTextSmNameOneOnRight.a|SmTimeSmTextSmNameOneOnRight.a|Same_Contents
SmTimeSmTextSmNameOneOnRight.a|SmTimeSmTextSmNameOneOnRight.a_1|Same_Contents";
            CompareResultsToString(results, expectedDuplicates);

            // search for differences in similar directories.
            results = SortFilesSearchDifferences.Go(settings);
            var expectedDifferences =
@"|MTimeAddTextMNameNone.z|Right
|MTimeAddTextMNameOneOnLeft.z|Right
|MTimeAddTextMNameOneOnRight.z|Right
|MTimeAddTextMNameOneOnRight.z_1|Right
|MTimeAddTextSmNameOneOnRight.a_1|Right
|MTimeAltTextMNameNone.z|Right
|MTimeAltTextMNameOneOnLeft.z|Right
|MTimeAltTextMNameOneOnRight.z|Right
|MTimeAltTextMNameOneOnRight.z_1|Right
|MTimeAltTextSmNameOneOnRight.a_1|Right
|MTimeSmTextMNameNone.z|Right
|MTimeSmTextMNameOneOnLeft.z|Right
|MTimeSmTextMNameOneOnRight.z|Right
|MTimeSmTextMNameOneOnRight.z_1|Right
|MTimeSmTextSmNameOneOnRight.a_1|Right
|SmTimeAddTextMNameNone.z|Right
|SmTimeAddTextMNameOneOnLeft.z|Right
|SmTimeAddTextMNameOneOnRight.z|Right
|SmTimeAddTextMNameOneOnRight.z_1|Right
|SmTimeAddTextSmNameOneOnRight.a_1|Right
|SmTimeAltTextMNameNone.z|Right
|SmTimeAltTextMNameOneOnLeft.z|Right
|SmTimeAltTextMNameOneOnRight.z|Right
|SmTimeAltTextMNameOneOnRight.z_1|Right
|SmTimeAltTextSmNameOneOnRight.a_1|Right
|SmTimeSmTextMNameNone.z|Right
|SmTimeSmTextMNameOneOnLeft.z|Right
|SmTimeSmTextMNameOneOnRight.z|Right
|SmTimeSmTextMNameOneOnRight.z_1|Right
|SmTimeSmTextSmNameOneOnRight.a_1|Right
MTimeAddTextMNameNone.a||Left
MTimeAddTextMNameOneOnLeft.a||Left
MTimeAddTextMNameOneOnLeft.a_1||Left
MTimeAddTextMNameOneOnRight.a||Left
MTimeAddTextSmNameNone.a|MTimeAddTextSmNameNone.a|Changed
MTimeAddTextSmNameOneOnLeft.a|MTimeAddTextSmNameOneOnLeft.a|Changed
MTimeAddTextSmNameOneOnLeft.a_1||Left
MTimeAddTextSmNameOneOnRight.a|MTimeAddTextSmNameOneOnRight.a|Changed
MTimeAltTextMNameNone.a||Left
MTimeAltTextMNameOneOnLeft.a||Left
MTimeAltTextMNameOneOnLeft.a_1||Left
MTimeAltTextMNameOneOnRight.a||Left
MTimeAltTextSmNameNone.a|MTimeAltTextSmNameNone.a|Changed
MTimeAltTextSmNameOneOnLeft.a|MTimeAltTextSmNameOneOnLeft.a|Changed
MTimeAltTextSmNameOneOnLeft.a_1||Left
MTimeAltTextSmNameOneOnRight.a|MTimeAltTextSmNameOneOnRight.a|Changed
MTimeSmTextMNameNone.a||Left
MTimeSmTextMNameOneOnLeft.a||Left
MTimeSmTextMNameOneOnLeft.a_1||Left
MTimeSmTextMNameOneOnRight.a||Left
MTimeSmTextSmNameNone.a|MTimeSmTextSmNameNone.a|Changed
MTimeSmTextSmNameOneOnLeft.a|MTimeSmTextSmNameOneOnLeft.a|Changed
MTimeSmTextSmNameOneOnLeft.a_1||Left
MTimeSmTextSmNameOneOnRight.a|MTimeSmTextSmNameOneOnRight.a|Changed
SmTimeAddTextMNameNone.a||Left
SmTimeAddTextMNameOneOnLeft.a||Left
SmTimeAddTextMNameOneOnLeft.a_1||Left
SmTimeAddTextMNameOneOnRight.a||Left
SmTimeAddTextSmNameNone.a|SmTimeAddTextSmNameNone.a|Changed
SmTimeAddTextSmNameOneOnLeft.a|SmTimeAddTextSmNameOneOnLeft.a|Changed
SmTimeAddTextSmNameOneOnLeft.a_1||Left
SmTimeAddTextSmNameOneOnRight.a|SmTimeAddTextSmNameOneOnRight.a|Changed
SmTimeAltTextMNameNone.a||Left
SmTimeAltTextMNameOneOnLeft.a||Left
SmTimeAltTextMNameOneOnLeft.a_1||Left
SmTimeAltTextMNameOneOnRight.a||Left
SmTimeAltTextSmNameOneOnLeft.a_1||Left
SmTimeSmTextMNameNone.a||Left
SmTimeSmTextMNameOneOnLeft.a||Left
SmTimeSmTextMNameOneOnLeft.a_1||Left
SmTimeSmTextMNameOneOnRight.a||Left
SmTimeSmTextSmNameOneOnLeft.a_1||Left";
            CompareResultsToString(results, expectedDifferences);

            // account for all 96 files.
            // (SortFilesSearchDifferences doesn't check hashes, so although it knows
            // AddText are different because filesize changes,
            // it won't detect AltText unless filesize or lmt are also different.)
            var expectedSame =
@"SmTimeAltTextSmNameNone.a|SmTimeAltTextSmNameNone.a
SmTimeAltTextSmNameOneOnLeft.a|SmTimeAltTextSmNameOneOnLeft.a
SmTimeAltTextSmNameOneOnRight.a|SmTimeAltTextSmNameOneOnRight.a
SmTimeSmTextSmNameNone.a|SmTimeSmTextSmNameNone.a
SmTimeSmTextSmNameOneOnLeft.a|SmTimeSmTextSmNameOneOnLeft.a
SmTimeSmTextSmNameOneOnRight.a|SmTimeSmTextSmNameOneOnRight.a";
            TestUtil.IsEq(filesCreated,
                CountFilenames(expectedDifferences) + CountFilenames(expectedSame));
        }

        static void SelectFirstItems(List<FileComparisonResult> selectedItems,
            ListView listView, int itemsToSelect)
        {
            selectedItems.Clear();
            for (int i = 0; i < itemsToSelect; i++)
            {
                selectedItems.Add((FileComparisonResult)listView.Items[i]);
            }
        }

        static void TestMethod_TestSortFilesListOperations()
        {
            // create settings object
            var settings = new SortFilesSettings();
            var left = TestUtil.GetTestSubDirectory("left_sortfileslist");
            var right = TestUtil.GetTestSubDirectory("right_sortfileslist");
            settings.LeftDirectory = left;
            settings.RightDirectory = right;

            // first, set up test files
            File.WriteAllText(left + Utils.PathSep + "onlyleft.txt", "onlyl");
            File.WriteAllText(left + Utils.PathSep + "changed1.txt", "a");
            File.WriteAllText(left + Utils.PathSep + "changed2.txt", "123");
            File.WriteAllText(left + Utils.PathSep + "same.txt", "s");
            File.WriteAllText(right + Utils.PathSep + "onlyright.txt", "onlyr");
            File.WriteAllText(right + Utils.PathSep + "changed1.txt", "abc");
            File.WriteAllText(right + Utils.PathSep + "changed2.txt", "124");
            File.WriteAllText(right + Utils.PathSep + "same.txt", "s");
            Action checkFileContents = () =>
            {
                TestUtil.IsEq("onlyl", File.ReadAllText(Path.Combine(left, "onlyleft.txt")));
                TestUtil.IsEq("a", File.ReadAllText(Path.Combine(left, "changed1.txt")));
                TestUtil.IsEq("123", File.ReadAllText(Path.Combine(left, "changed2.txt")));
                TestUtil.IsEq("onlyr", File.ReadAllText(Path.Combine(right, "onlyright.txt")));
                TestUtil.IsEq("abc", File.ReadAllText(Path.Combine(right, "changed1.txt")));
                TestUtil.IsEq("124", File.ReadAllText(Path.Combine(right, "changed2.txt")));
            };

            // tweak last write times to ensure files on right look different
            File.SetLastWriteTime(right + Utils.PathSep + "changed1.txt",
                File.GetLastWriteTime(right + Utils.PathSep + "changed1.txt").AddDays(1));
            File.SetLastWriteTime(right + Utils.PathSep + "changed2.txt",
                File.GetLastWriteTime(right + Utils.PathSep + "changed2.txt").AddDays(1));
            File.SetLastWriteTime(right + Utils.PathSep + "same.txt",
                File.GetLastWriteTime(left + Utils.PathSep + "same.txt"));

            // create form and run searchdifferences
            var form = new FormSortFilesList(
                SortFilesAction.SearchDifferences, settings, "", allActionsSynchronous: true);
            ListView listView;
            List<FileComparisonResult> mockSelection;
            UndoStack<List<FileMove>> undoStack;
            form.GetTestHooks(out listView, out mockSelection, out undoStack);
            form.RunSortFilesAction();

            // simulate column-header click to sort by path
            form.listView_ColumnClick(null, new ColumnClickEventArgs(2));

            // verify listview contents
            var items = listView.Items.Cast<FileComparisonResult>().ToArray();
            TestUtil.IsEq(4, items.Length);
            TestUtil.IsEq(Utils.PathSep + "changed1.txt", items[0].FileInfoLeft.Filename);
            TestUtil.IsEq(null, items[0].FileInfoLeft.ContentHash);
            TestUtil.IsEq(1L, items[0].FileInfoLeft.FileSize);
            TestUtil.IsEq(Utils.PathSep + "changed1.txt", items[0].FileInfoRight.Filename);
            TestUtil.IsEq(null, items[0].FileInfoRight.ContentHash);
            TestUtil.IsEq(3L, items[0].FileInfoRight.FileSize);
            TestUtil.IsEq(Utils.PathSep + "changed2.txt", items[1].FileInfoLeft.Filename);
            TestUtil.IsEq(null, items[1].FileInfoLeft.ContentHash);
            TestUtil.IsEq(3L, items[1].FileInfoLeft.FileSize);
            TestUtil.IsEq(Utils.PathSep + "changed2.txt", items[1].FileInfoRight.Filename);
            TestUtil.IsEq(null, items[1].FileInfoRight.ContentHash);
            TestUtil.IsEq(3L, items[1].FileInfoRight.FileSize);
            TestUtil.IsEq(Utils.PathSep + "onlyleft.txt", items[2].FileInfoLeft.Filename);
            TestUtil.IsEq(null, items[2].FileInfoLeft.ContentHash);
            TestUtil.IsEq(5L, items[2].FileInfoLeft.FileSize);
            TestUtil.IsEq(null, items[2].FileInfoRight);
            TestUtil.IsEq(Utils.PathSep + "onlyright.txt", items[3].FileInfoRight.Filename);
            TestUtil.IsEq(null, items[3].FileInfoRight.ContentHash);
            TestUtil.IsEq(5L, items[3].FileInfoRight.FileSize);
            TestUtil.IsEq(null, items[3].FileInfoLeft);

            // test CheckSelectedItemsSameType
            mockSelection.Clear();
            TestUtil.IsEq(false, form.CheckSelectedItemsSameType());
            mockSelection.Add((FileComparisonResult)listView.Items[0]);
            TestUtil.IsEq(true, form.CheckSelectedItemsSameType());
            mockSelection.Add((FileComparisonResult)listView.Items[1]);
            TestUtil.IsEq(true, form.CheckSelectedItemsSameType());
            mockSelection.Add((FileComparisonResult)listView.Items[2]);
            TestUtil.IsEq(false, form.CheckSelectedItemsSameType());
            mockSelection.Clear();
            TestUtil.IsEq(false, form.CheckSelectedItemsSameType());
            mockSelection.Add((FileComparisonResult)listView.Items[1]);
            TestUtil.IsEq(true, form.CheckSelectedItemsSameType());
            mockSelection.Add((FileComparisonResult)listView.Items[2]);
            TestUtil.IsEq(false, form.CheckSelectedItemsSameType());

            // delete all on left
            checkFileContents();
            SelectFirstItems(mockSelection, listView, items.Length);
            form.OnClickDeleteFile(left: true, needConfirm: false);

            // see if undo was set as expected
            var lastUndo = undoStack.PeekUndo();
            TestUtil.IsEq(3, lastUndo.Count);
            TestUtil.IsEq(Path.Combine(left, "changed1.txt"), lastUndo[0].Source);
            TestUtil.IsEq(Path.Combine(left, "changed2.txt"), lastUndo[1].Source);
            TestUtil.IsEq(Path.Combine(left, "onlyleft.txt"), lastUndo[2].Source);

            // test file presence
            TestUtil.IsTrue(!File.Exists(Path.Combine(left, "changed1.txt")));
            TestUtil.IsTrue(!File.Exists(Path.Combine(left, "changed2.txt")));
            TestUtil.IsTrue(!File.Exists(Path.Combine(left, "onlyleft.txt")));
            TestUtil.IsTrue(File.Exists(Path.Combine(right, "changed1.txt")));
            TestUtil.IsTrue(File.Exists(Path.Combine(right, "changed2.txt")));
            TestUtil.IsTrue(File.Exists(Path.Combine(right, "onlyright.txt")));

            // run undo
            form.OnUndoClick(needConfirm: false);
            TestUtil.IsEq(null, undoStack.PeekUndo());
            checkFileContents();

            // delete all on right
            SelectFirstItems(mockSelection, listView, items.Length);
            form.OnClickDeleteFile(left: false, needConfirm: false);

            // see if undo was set as expected
            lastUndo = undoStack.PeekUndo();
            TestUtil.IsEq(3, lastUndo.Count);
            TestUtil.IsEq(Path.Combine(right, "changed1.txt"), lastUndo[0].Source);
            TestUtil.IsEq(Path.Combine(right, "changed2.txt"), lastUndo[1].Source);
            TestUtil.IsEq(Path.Combine(right, "onlyright.txt"), lastUndo[2].Source);

            // test file presence
            TestUtil.IsTrue(File.Exists(Path.Combine(left, "changed1.txt")));
            TestUtil.IsTrue(File.Exists(Path.Combine(left, "changed2.txt")));
            TestUtil.IsTrue(File.Exists(Path.Combine(left, "onlyleft.txt")));
            TestUtil.IsTrue(!File.Exists(Path.Combine(right, "changed1.txt")));
            TestUtil.IsTrue(!File.Exists(Path.Combine(right, "changed2.txt")));
            TestUtil.IsTrue(!File.Exists(Path.Combine(right, "onlyright.txt")));

            // run undo
            form.OnUndoClick(needConfirm: false);
            TestUtil.IsEq(null, undoStack.PeekUndo());
            checkFileContents();

            // copy all left to right
            SelectFirstItems(mockSelection, listView, items.Length);
            form.OnClickCopyFile(left: true, needConfirm: false);

            // see if undo was set as expected
            lastUndo = undoStack.PeekUndo();
            TestUtil.IsEq(5, lastUndo.Count);
            TestUtil.IsEq(true, lastUndo[0].MoveOrCopy);
            TestUtil.IsEq(Path.Combine(right, "changed1.txt"), lastUndo[0].Source);
            TestUtil.IsEq(false, lastUndo[1].MoveOrCopy);
            TestUtil.IsEq(Path.Combine(left, "changed1.txt"), lastUndo[1].Source);
            TestUtil.IsEq(Path.Combine(right, "changed1.txt"), lastUndo[1].Dest);
            TestUtil.IsEq(true, lastUndo[2].MoveOrCopy);
            TestUtil.IsEq(Path.Combine(right, "changed2.txt"), lastUndo[2].Source);
            TestUtil.IsEq(false, lastUndo[3].MoveOrCopy);
            TestUtil.IsEq(Path.Combine(left, "changed2.txt"), lastUndo[3].Source);
            TestUtil.IsEq(Path.Combine(right, "changed2.txt"), lastUndo[3].Dest);
            TestUtil.IsEq(false, lastUndo[4].MoveOrCopy);
            TestUtil.IsEq(Path.Combine(left, "onlyleft.txt"), lastUndo[4].Source);
            TestUtil.IsEq(Path.Combine(right, "onlyleft.txt"), lastUndo[4].Dest);

            // test file contents
            TestUtil.IsEq("onlyl", File.ReadAllText(Path.Combine(left, "onlyleft.txt")));
            TestUtil.IsEq("a", File.ReadAllText(Path.Combine(left, "changed1.txt")));
            TestUtil.IsEq("123", File.ReadAllText(Path.Combine(left, "changed2.txt")));
            TestUtil.IsEq("onlyl", File.ReadAllText(Path.Combine(right, "onlyleft.txt")));
            TestUtil.IsEq("onlyr", File.ReadAllText(Path.Combine(right, "onlyright.txt")));
            TestUtil.IsEq("a", File.ReadAllText(Path.Combine(right, "changed1.txt")));
            TestUtil.IsEq("123", File.ReadAllText(Path.Combine(right, "changed2.txt")));

            // run undo
            form.OnUndoClick(needConfirm: false);
            TestUtil.IsEq(null, undoStack.PeekUndo());
            TestUtil.IsTrue(!File.Exists(Path.Combine(right, "onlyleft.txt")));
            checkFileContents();

            // copy all right to left
            SelectFirstItems(mockSelection, listView, items.Length);
            form.OnClickCopyFile(left: false, needConfirm: false);

            // see if undo was set as expected
            lastUndo = undoStack.PeekUndo();
            TestUtil.IsEq(5, lastUndo.Count);
            TestUtil.IsEq(true, lastUndo[0].MoveOrCopy);
            TestUtil.IsEq(Path.Combine(left, "changed1.txt"), lastUndo[0].Source);
            TestUtil.IsEq(false, lastUndo[1].MoveOrCopy);
            TestUtil.IsEq(Path.Combine(right, "changed1.txt"), lastUndo[1].Source);
            TestUtil.IsEq(Path.Combine(left, "changed1.txt"), lastUndo[1].Dest);
            TestUtil.IsEq(true, lastUndo[2].MoveOrCopy);
            TestUtil.IsEq(Path.Combine(left, "changed2.txt"), lastUndo[2].Source);
            TestUtil.IsEq(false, lastUndo[3].MoveOrCopy);
            TestUtil.IsEq(Path.Combine(right, "changed2.txt"), lastUndo[3].Source);
            TestUtil.IsEq(Path.Combine(left, "changed2.txt"), lastUndo[3].Dest);
            TestUtil.IsEq(false, lastUndo[4].MoveOrCopy);
            TestUtil.IsEq(Path.Combine(right, "onlyright.txt"), lastUndo[4].Source);
            TestUtil.IsEq(Path.Combine(left, "onlyright.txt"), lastUndo[4].Dest);

            // test file contents
            TestUtil.IsEq("onlyl", File.ReadAllText(Path.Combine(left, "onlyleft.txt")));
            TestUtil.IsEq("onlyr", File.ReadAllText(Path.Combine(left, "onlyright.txt")));
            TestUtil.IsEq("abc", File.ReadAllText(Path.Combine(left, "changed1.txt")));
            TestUtil.IsEq("124", File.ReadAllText(Path.Combine(left, "changed2.txt")));
            TestUtil.IsEq("onlyr", File.ReadAllText(Path.Combine(right, "onlyright.txt")));
            TestUtil.IsEq("abc", File.ReadAllText(Path.Combine(right, "changed1.txt")));
            TestUtil.IsEq("124", File.ReadAllText(Path.Combine(right, "changed2.txt")));

            // run undo
            form.OnUndoClick(needConfirm: false);
            TestUtil.IsEq(null, undoStack.PeekUndo());
            TestUtil.IsTrue(!File.Exists(Path.Combine(left, "onlyright.txt")));
            checkFileContents();
            form.Dispose();
        }
    }
}
