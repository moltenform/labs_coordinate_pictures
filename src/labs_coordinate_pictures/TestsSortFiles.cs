using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

    enum ModifiedTimes { SmTime, MTime }
    enum Content { SmText, AltText, AddText }
    enum Filename { SmName, MName }
    enum ExtraCopies { None, OneOnLeft, OneOnRight }
    public static class CreateFileCombinations
    {
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
                dirFirst + Path.DirectorySeparatorChar, dirSecond, true, true, true, true));

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                dirFirst, dirSecond + Path.DirectorySeparatorChar, true, true, true, true));

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                dirFirst, dirFirst, true, true, true, true));

            Directory.CreateDirectory(Path.Combine(dirFirst, "sub"));
            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                dirFirst, Path.Combine(dirFirst, "sub"), true, true, true, true));

           // valid for dest to be empty if action is FindDupeFilesInOneDir
            TestUtil.IsTrue(FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicatesInOneDir, "", "",
                dirFirst, "", true, false, true, true) != null);
        }

        static void WriteTextFile(string dir, string path, string contents, DateTime time)
        {
            File.WriteAllText(Path.Combine(dir, path), contents);
            File.SetLastWriteTimeUtc(Path.Combine(dir, path), time);
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
            return string.Join(Environment.NewLine, lines);
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

        static void TestMethod_HighLevel()
        {
            // run the methods on actual files. first create combinations of modified/not modified.
            var settings = new SortFilesSettings();
            settings.LeftDirectory = TestUtil.GetTestSubDirectory("left_fndmved");
            settings.RightDirectory = TestUtil.GetTestSubDirectory("right_fndmved");
            var filesCreated = CreateFileCombinations.Go(settings.LeftDirectory, settings.RightDirectory);
            TestUtil.IsEq(
                Enum.GetValues(typeof(ModifiedTimes)).Length *
                Enum.GetValues(typeof(Content)).Length *
                Enum.GetValues(typeof(Filename)).Length *
                ((1 * 2) + (3 * 2)), // ExtraCopies.None -> 2 files, the rest -> 3 files
                filesCreated);

            // search for duplicates in one directory. The only ones it will find are the 'extra copy on left.'
            var results = SortFilesSearchDuplicatesInOneDir.Go(settings);
            TestUtil.IsEq(
                Enum.GetValues(typeof(ModifiedTimes)).Length *
                Enum.GetValues(typeof(Content)).Length *
                Enum.GetValues(typeof(Filename)).Length, results.Count);

            // verify sort order. for each pair, the left side should be the one that sorts first alphabetically
            var expectedDuplicates = @"MTimeAddTextMNameOneOnLeft.a|MTimeAddTextMNameOneOnLeft.a_1|Same_Contents
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
            expectedDuplicates = @"MTimeSmTextMNameNone.a|MTimeSmTextMNameNone.z|Same_Contents
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
            var expectedDifferences = @"|MTimeAddTextMNameNone.z|Right
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
            // (SortFilesSearchDifferences doesn't check hashes, so although it knows AddText are different because filesize changes,
            // it won't detect AltText unless filesize or lmt are also different.)
            var expectedSame = @"SmTimeAltTextSmNameNone.a|SmTimeAltTextSmNameNone.a
SmTimeAltTextSmNameOneOnLeft.a|SmTimeAltTextSmNameOneOnLeft.a
SmTimeAltTextSmNameOneOnRight.a|SmTimeAltTextSmNameOneOnRight.a
SmTimeSmTextSmNameNone.a|SmTimeSmTextSmNameNone.a
SmTimeSmTextSmNameOneOnLeft.a|SmTimeSmTextSmNameOneOnLeft.a
SmTimeSmTextSmNameOneOnRight.a|SmTimeSmTextSmNameOneOnRight.a";
            TestUtil.IsEq(filesCreated,
                CountFilenames(expectedDifferences) + CountFilenames(expectedSame));

            //results = SortFilesSearchDifferencesAndDetectRenames.Go(settings);
            //expectedDifferences = @"";
            //CompareResultsToString(results, expectedDifferences);
        }
    }
}
