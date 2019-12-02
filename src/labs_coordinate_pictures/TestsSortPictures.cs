// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public static class CoordinatePicturesTests
    {
        static void TestMethod_Asserts_EqualIntsShouldCompareEqual()
        {
            TestUtil.IsEq(1, 1);
        }

        static void TestMethod_Asserts_EqualStringsShouldCompareEqual()
        {
            TestUtil.IsEq("abcd", "abcd");
        }

        static void TestMethod_Asserts_EqualBoolsShouldCompareEqual()
        {
            TestUtil.IsEq(true, true);
            TestUtil.IsEq(false, false);
        }

        static void TestMethod_Asserts_IsStringArrayEq()
        {
            TestUtil.IsStringArrayEq(null, null);
            TestUtil.IsStringArrayEq(null, new string[] { });
            TestUtil.IsStringArrayEq("", new string[] { "" });
            TestUtil.IsStringArrayEq("|", new string[] { "", "" });
            TestUtil.IsStringArrayEq("||", new string[] { "", "", "" });
            TestUtil.IsStringArrayEq("aa|bb|cc", new string[] { "aa", "bb", "cc" });
        }

        static void TestMethod_Asserts_CheckAssertMessage()
        {
            Action fn = () =>
            {
                throw new CoordinatePicturesTestException("test123");
            };

            TestUtil.AssertExceptionMessage(fn, "test123");
        }

        static void TestMethod_Asserts_NonEqualIntsShouldCompareNonEqual()
        {
            TestUtil.AssertExceptionMessage(() => TestUtil.IsEq(1, 2),
                "expected 1 but got 2");
        }

        static void TestMethod_Asserts_NonEqualStrsShouldCompareNonEqual()
        {
            TestUtil.AssertExceptionMessage(() => TestUtil.IsEq("abcd", "abce"),
                "expected abcd but got abce");
        }

        static void TestMethod_Asserts_NonEqualBoolsShouldCompareNonEqual()
        {
            TestUtil.AssertExceptionMessage(() => TestUtil.IsEq(true, false),
                "expected True but got False");
        }

        static string PathSep(string s)
        {
            // replace slashes with platform appropriate character
            return s.Replace("/", Utils.Sep);
        }

        static void TestMethod_UtilsSameExceptExtension()
        {
            TestUtil.IsTrue(FilenameUtils.SameExceptExtension(
                "test6.jpg", "test6.jpg"));
            TestUtil.IsTrue(FilenameUtils.SameExceptExtension(
                "test6.jpg", "test6.png"));
            TestUtil.IsTrue(FilenameUtils.SameExceptExtension(
                "test6.jpg", "test6.BMP"));
            TestUtil.IsTrue(!FilenameUtils.SameExceptExtension(
                "test6.jpg", "test6.jpg.jpg"));
            TestUtil.IsTrue(!FilenameUtils.SameExceptExtension(
                "test6a.jpg", "test6.jpg"));
            TestUtil.IsTrue(FilenameUtils.SameExceptExtension(
                "aa.jpg.test6.jpg", "aa.jpg.test6.bmp"));
            TestUtil.IsTrue(!FilenameUtils.SameExceptExtension(
                "aa.jpg.test6.jpg", "aa.bmp.test6.jpg"));

            TestUtil.IsTrue(FilenameUtils.SameExceptExtension(
                PathSep("a/test6.jpg"), PathSep("a/test6.jpg")));
            TestUtil.IsTrue(!FilenameUtils.SameExceptExtension(
                PathSep("a/test6.jpg"), PathSep("b/test6.jpg")));
        }

        static void TestMethod_UtilsIsDigits()
        {
            TestUtil.IsTrue(!Utils.IsDigits(null));
            TestUtil.IsTrue(!Utils.IsDigits(""));
            TestUtil.IsTrue(Utils.IsDigits("0"));
            TestUtil.IsTrue(Utils.IsDigits("0123"));
            TestUtil.IsTrue(Utils.IsDigits("456789"));
            TestUtil.IsTrue(!Utils.IsDigits("456789a"));
            TestUtil.IsTrue(!Utils.IsDigits("a456789a"));
        }

        static void TestMethod_UtilsFirstTwoChars()
        {
            TestUtil.IsEq("", Utils.FirstTwoChars(""));
            TestUtil.IsEq("a", Utils.FirstTwoChars("a"));
            TestUtil.IsEq("ab", Utils.FirstTwoChars("ab"));
            TestUtil.IsEq("ab", Utils.FirstTwoChars("abc"));
        }

        static void TestMethod_UtilsArePathsDistinct()
        {
            TestUtil.IsTrue(!Utils.ArePathsDistinct("", ""));
            TestUtil.IsTrue(!Utils.ArePathsDistinct("a", "a"));
            TestUtil.IsTrue(!Utils.ArePathsDistinct(@"C:\A", @"C:\A"));
            TestUtil.IsTrue(!Utils.ArePathsDistinct(@"C:\A", @"C:\a"));
            TestUtil.IsTrue(!Utils.ArePathsDistinct(@"C:\A\subdir", @"C:\A"));
            TestUtil.IsTrue(!Utils.ArePathsDistinct(@"C:\A", @"C:\A\subdir"));
            TestUtil.IsTrue(Utils.ArePathsDistinct(@"C:\A", @"C:\AA"));
            TestUtil.IsTrue(Utils.ArePathsDistinct(@"C:\abc", @"C:\ABCDE"));
        }

        static void TestMethod_UtilsCombineProcessArguments()
        {
            TestUtil.IsEq("", Utils.CombineProcessArguments(new string[] { }));
            TestUtil.IsEq("\"\"", Utils.CombineProcessArguments(new string[] { "" }));
            TestUtil.IsEq("\"\\\"\"", Utils.CombineProcessArguments(new string[] { "\"" }));
            TestUtil.IsEq("\"\\\"\\\"\"", Utils.CombineProcessArguments(new string[] { "\"\"" }));
            TestUtil.IsEq("\"\\\"a\\\"\"", Utils.CombineProcessArguments(new string[] { "\"a\"" }));
            TestUtil.IsEq("\\", Utils.CombineProcessArguments(new string[] { "\\" }));
            TestUtil.IsEq("\"a b\"", Utils.CombineProcessArguments(new string[] { "a b" }));
            TestUtil.IsEq("a \" b\"", Utils.CombineProcessArguments(new string[] { "a", " b" }));
            TestUtil.IsEq("a\\\\b", Utils.CombineProcessArguments(new string[] { "a\\\\b" }));
            TestUtil.IsEq("\" \\\\\"", Utils.CombineProcessArguments(new string[] { " \\" }));
            TestUtil.IsEq("\" \\\\\\\"\"", Utils.CombineProcessArguments(new string[] { " \\\"" }));
            TestUtil.IsEq("\" \\\\\\\\\"", Utils.CombineProcessArguments(new string[] { " \\\\" }));

            TestUtil.IsEq("\"a\\\\b c\"", Utils.CombineProcessArguments(
                new string[] { "a\\\\b c" }));
            TestUtil.IsEq("\"C:\\Program Files\\\\\"", Utils.CombineProcessArguments(
                new string[] { "C:\\Program Files\\" }));
            TestUtil.IsEq("\"dafc\\\"\\\"\\\"a\"", Utils.CombineProcessArguments(
                new string[] { "dafc\"\"\"a" }));
        }

        static void TestMethod_UtilsFormatPythonError()
        {
            TestUtil.IsEq("", Utils.FormatPythonError(""));
            TestUtil.IsEq("NotError", Utils.FormatPythonError("NotError"));
            TestUtil.IsEq("Not Error: Noterror",
                Utils.FormatPythonError("Not Error: Noterror"));
            TestUtil.IsEq("IsError: Details" + Utils.NL + Utils.NL + Utils.NL +
                "Details: text before IsError: Details",
                Utils.FormatPythonError("text before IsError: Details"));
            TestUtil.IsEq("IsError:2 some words" + Utils.NL + Utils.NL + Utils.NL +
                "Details: text before IsError:1 IsError:2 some words",
                Utils.FormatPythonError("text before IsError:1 IsError:2 some words"));

            var sampleStderr = "test failed, stderr = Traceback (most recent call last): File " +
                "reallylong string reallylong string reallylong string reallylong string " +
                ", line 1234, in test.py, raise RuntimeError(errMsg)RuntimeError: the actual msg";
            TestUtil.IsEq("RuntimeError: the actual msg" + Utils.NL + Utils.NL + Utils.NL +
                "Details: " + sampleStderr,
                Utils.FormatPythonError(sampleStderr));
        }

        static void TestMethod_UtilsGetFirstHttpLink()
        {
            TestUtil.IsEq(null, Utils.GetFirstHttpLink(""));
            TestUtil.IsEq(null, Utils.GetFirstHttpLink("no urls present http none"));
            TestUtil.IsEq("http://www.ok.com", Utils.GetFirstHttpLink("http://www.ok.com"));

            TestUtil.IsEq("http://www.ok.com", Utils.GetFirstHttpLink(
                "http://www.ok.com a b c http://www.second.com"));
            TestUtil.IsEq("http://www.ok.com", Utils.GetFirstHttpLink(
                "a b c http://www.ok.com a b c http://www.second.com"));
        }

        static void TestMethod_UtilsFormatFilesize()
        {
            TestUtil.IsEq(" (2.00mb)", Utils.FormatFilesize((2 * 1024 * 1024) + 25));
            TestUtil.IsEq(" (1.02mb)", Utils.FormatFilesize((1024 * 1024) + 20000));
            TestUtil.IsEq(" (345k)", Utils.FormatFilesize((1024 * 345) + 25));
            TestUtil.IsEq(" (1k)", Utils.FormatFilesize(1025));
            TestUtil.IsEq(" (1k)", Utils.FormatFilesize(1024));
            TestUtil.IsEq(" (1k)", Utils.FormatFilesize(1023));
            TestUtil.IsEq(" (1k)", Utils.FormatFilesize(1));
            TestUtil.IsEq(" (0k)", Utils.FormatFilesize(0));
        }

        static void TestMethod_ArrayAt()
        {
            var arr = new int[] { 1, 2, 3 };
            TestUtil.IsEq(1, Utils.ArrayAt(arr, -100));
            TestUtil.IsEq(1, Utils.ArrayAt(arr, -1));
            TestUtil.IsEq(1, Utils.ArrayAt(arr, 0));
            TestUtil.IsEq(2, Utils.ArrayAt(arr, 1));
            TestUtil.IsEq(3, Utils.ArrayAt(arr, 2));
            TestUtil.IsEq(3, Utils.ArrayAt(arr, 3));
            TestUtil.IsEq(3, Utils.ArrayAt(arr, 100));
        }

        static void TestMethod_LooksLikePath()
        {
            TestUtil.IsTrue(!Utils.LooksLikePath(""));
            TestUtil.IsTrue(!Utils.LooksLikePath("/"));
            TestUtil.IsTrue(!Utils.LooksLikePath("\\"));
            TestUtil.IsTrue(!Utils.LooksLikePath("C:"));
            TestUtil.IsTrue(Utils.LooksLikePath("C:\\"));
            TestUtil.IsTrue(Utils.LooksLikePath("C:\\a\\b\\c"));
            TestUtil.IsTrue(Utils.LooksLikePath("\\test"));
            TestUtil.IsTrue(Utils.LooksLikePath("\\test\\a\\b\\c"));
        }

        static void TestMethod_GetFileAttributes()
        {
            var path = Path.Combine(TestUtil.GetTestWriteDirectory(), "testhash.txt");
            File.WriteAllText(path, "12345678");
            TestUtil.IsEq(File.GetAttributes(path), Utils.GetFileAttributesOrNone(path));

            var pathNotExist = Path.Combine(TestUtil.GetTestWriteDirectory(), "testhash2.txt");
            TestUtil.IsEq(false, File.Exists(pathNotExist));
            TestUtil.IsEq(FileAttributes.Normal, Utils.GetFileAttributesOrNone(pathNotExist));
        }

        static void TestMethod_TestSha512()
        {
            var path = Path.Combine(TestUtil.GetTestWriteDirectory(), "testhash.txt");
            File.WriteAllText(path, "12345678");
            TestUtil.IsEq("filenotfound:", Utils.GetSha512(null));
            TestUtil.IsEq("filenotfound:notexist", Utils.GetSha512("notexist"));
            TestUtil.IsEq("+lhdichR3TOKcNz1Naoqkv7ng23Wr/EiZYPojgmWK" +
                "T8WvACcZSgm4PxccGaVoDzdzjcvE57/TROVnabx9dPqvg==", Utils.GetSha512(path));
        }

        static void TestMethod_SplitByString()
        {
            TestUtil.IsStringArrayEq("", Utils.SplitByString("", "delim"));
            TestUtil.IsStringArrayEq("|", Utils.SplitByString("delim", "delim"));
            TestUtil.IsStringArrayEq("||", Utils.SplitByString("delimdelim", "delim"));
            TestUtil.IsStringArrayEq("a||b", Utils.SplitByString("adelimdelimb", "delim"));
            TestUtil.IsStringArrayEq("a|bb|c", Utils.SplitByString("adelimbbdelimc", "delim"));

            // make sure regex special characters are treated as normal chars
            TestUtil.IsStringArrayEq("a|bb|c", Utils.SplitByString("a**bb**c", "**"));
            TestUtil.IsStringArrayEq("a|bb|c", Utils.SplitByString("a?bb?c", "?"));
        }

        static void TestMethod_SoftDeleteDefaultDir()
        {
            var fakeFile = PathSep("C:/dirtest/test.doc");
            var deleteDir = Utils.GetSoftDeleteDirectory(fakeFile);
            var deleteDest = Utils.GetSoftDeleteDestination(fakeFile);
            TestUtil.IsTrue(deleteDest.StartsWith(deleteDir +
                Utils.Sep + "di_test.doc", StringComparison.Ordinal));
        }

        static void TestMethod_IsExtensionInList()
        {
            var exts = new string[] { ".jpg", ".png" };
            TestUtil.IsTrue(!FilenameUtils.IsExtensionInList("", exts));
            TestUtil.IsTrue(!FilenameUtils.IsExtensionInList("png", exts));
            TestUtil.IsTrue(!FilenameUtils.IsExtensionInList("a.bmp", exts));
            TestUtil.IsTrue(FilenameUtils.IsExtensionInList("a.png", exts));
            TestUtil.IsTrue(FilenameUtils.IsExtensionInList("a.PNG", exts));
            TestUtil.IsTrue(FilenameUtils.IsExtensionInList("a.jpg", exts));
            TestUtil.IsTrue(FilenameUtils.IsExtensionInList("a.bmp.jpg", exts));
            TestUtil.IsTrue(FilenameUtils.IsExt("a.png", ".png"));
            TestUtil.IsTrue(FilenameUtils.IsExt("a.PNG", ".png"));
            TestUtil.IsTrue(!FilenameUtils.IsExt("apng", ".png"));
            TestUtil.IsTrue(!FilenameUtils.IsExt("a.png", ".jpg"));
        }

        static void TestMethod_NumberedPrefix()
        {
            TestUtil.IsEq(PathSep("c:/test/([0000])abc.jpg"),
                FilenameUtils.AddNumberedPrefix(PathSep("c:/test/abc.jpg"), 0));
            TestUtil.IsEq(PathSep("c:/test/([0010])abc.jpg"),
                FilenameUtils.AddNumberedPrefix(PathSep("c:/test/abc.jpg"), 1));
            TestUtil.IsEq(PathSep("c:/test/([1230])abc.jpg"),
                FilenameUtils.AddNumberedPrefix(PathSep("c:/test/abc.jpg"), 123));
            TestUtil.IsEq(PathSep("c:/test/([1230])abc.jpg"),
                FilenameUtils.AddNumberedPrefix(PathSep("c:/test/([1230])abc.jpg"), 123));
            TestUtil.IsEq(PathSep("c:/test/([9999])abc.jpg"),
                FilenameUtils.AddNumberedPrefix(PathSep("c:/test/([9999])abc.jpg"), 123));
            TestUtil.IsEq(PathSep("a.jpg"),
                FilenameUtils.GetFileNameWithoutNumberedPrefix(PathSep("a.jpg")));
            TestUtil.IsEq(PathSep("abc.jpg"),
                FilenameUtils.GetFileNameWithoutNumberedPrefix(PathSep("c:/test/([9999])abc.jpg")));
            TestUtil.IsEq(PathSep("abc.jpg"),
                FilenameUtils.GetFileNameWithoutNumberedPrefix(PathSep("c:/test/([0000])abc.jpg")));
            TestUtil.IsEq(PathSep("abc.jpg"),
                FilenameUtils.GetFileNameWithoutNumberedPrefix(PathSep("c:/test/([1230])abc.jpg")));
        }

        static void TestMethod_UtilsGetCategory()
        {
            var testAdd = FilenameUtils.AddCategoryToFilename(PathSep("c:/dir/test/b b.aaa.jpg"), "mk");
            TestUtil.IsEq(PathSep("c:/dir/test/b b.aaa__MARKAS__mk.jpg"), testAdd);
            testAdd = FilenameUtils.AddCategoryToFilename(PathSep("c:/dir/test/b b.aaa.jpg"), "");
            TestUtil.IsEq(PathSep("c:/dir/test/b b.aaa__MARKAS__.jpg"), testAdd);

            Func<string, string> testGetCategory = (input) =>
            {
                FilenameUtils.GetCategoryFromFilename(input, out string pathWithoutCategory, out string category);
                return pathWithoutCategory + "|" + category;
            };

            TestUtil.IsEq(PathSep("C:/dir/test/file.jpg|123"),
                testGetCategory(PathSep("C:/dir/test/file__MARKAS__123.jpg")));
            TestUtil.IsEq(PathSep("C:/dir/test/file.also.jpg|123"),
                testGetCategory(PathSep("C:/dir/test/file.also__MARKAS__123.jpg")));
            TestUtil.IsEq(PathSep("C:/dir/test/file.jpg|"),
                testGetCategory(PathSep("C:/dir/test/file__MARKAS__.jpg")));

            // check that invalid paths cause exception to be thrown.
            TestUtil.AssertExceptionMessage(() => testGetCategory(
                PathSep("C:/dir/test/dirmark__MARKAS__b/file__MARKAS__123.jpg")), "Directories");
            TestUtil.AssertExceptionMessage(() => testGetCategory(
                PathSep("C:/dir/test/dirmark__MARKAS__b/file.jpg")), "Directories");
            TestUtil.AssertExceptionMessage(() => testGetCategory(
                PathSep("C:/dir/test/file__MARKAS__123__MARKAS__123.jpg")), "exactly 1");
            TestUtil.AssertExceptionMessage(() => testGetCategory(
                PathSep("C:/dir/test/file.jpg")), "exactly 1");
            TestUtil.AssertExceptionMessage(() => testGetCategory(
                PathSep("C:/dir/test/file__MARKAS__123.dir.jpg")), "after the marker");
        }

        static void TestMethod_FindSimilarFilenames()
        {
            var mode = new ModeCategorizeAndRename();
            var extensions = mode.GetFileTypes();
            bool nameHasSuffix;
            string pathWithoutSuffix;
            var filepaths = new string[] {
                PathSep("c:/a/a.png"),
                PathSep("c:/a/b.png"),
                PathSep("c:/a/ab.png"),
                PathSep("c:/a/b_out.png"),
                PathSep("c:/a/a_out.png"),
                PathSep("c:/a/a.png60.jpg"),
                PathSep("c:/a/a.png80.jpg"),
                PathSep("c:/b/a.png90.jpg") };

            // alone with no added suffix
            TestUtil.IsStringArrayEq(null, FindSimilarFilenames.FindSimilarNames(
                PathSep("c:/a/b.png"), extensions, filepaths,
                out nameHasSuffix, out pathWithoutSuffix));
            TestUtil.IsTrue(!nameHasSuffix);
            TestUtil.IsEq(null, pathWithoutSuffix);

            // alone with an added suffix
            TestUtil.IsStringArrayEq(null, FindSimilarFilenames.FindSimilarNames(
                PathSep("c:/b/a.png90.jpg"), extensions, filepaths,
                out nameHasSuffix, out pathWithoutSuffix));
            TestUtil.IsTrue(nameHasSuffix);
            TestUtil.IsEq(PathSep("c:/b/a.jpg"), pathWithoutSuffix);

            // has similar names with no added suffix
            TestUtil.IsStringArrayEq(PathSep("c:/a/a.png60.jpg|c:/a/a.png80.jpg"),
                FindSimilarFilenames.FindSimilarNames(
                    PathSep("c:/a/a.png"), extensions, filepaths,
                    out nameHasSuffix, out pathWithoutSuffix));
            TestUtil.IsTrue(!nameHasSuffix);
            TestUtil.IsEq(null, pathWithoutSuffix);

            // has similar names with an added suffix
            TestUtil.IsStringArrayEq(PathSep("c:/a/a.png|c:/a/a.png80.jpg"),
                FindSimilarFilenames.FindSimilarNames(
                    PathSep("c:/a/a.png60.jpg"), extensions, filepaths,
                    out nameHasSuffix, out pathWithoutSuffix));
            TestUtil.IsTrue(nameHasSuffix);
            TestUtil.IsEq(PathSep("c:/a/a.jpg"), pathWithoutSuffix);
        }

        static void TestMethod_Logging()
        {
            var path = Path.Combine(TestUtil.GetTestWriteDirectory(), "testlog.txt");
            var log = new SimpleLog(path, 1024);

            // write simple log entries
            log.WriteError("test e");
            log.WriteWarning("test w");
            log.WriteLog("test l");
            TestUtil.IsEq("\n[error] test e\n[warning] test w\ntest l",
                File.ReadAllText(path).Replace("\r\n", "\n"));

            // add until over the filesize limit
            for (int i = 0; i < 60; i++)
            {
                log.WriteLog("123456789012345");
            }

            // it's over the limit, but hasn't reached the period yet, so still a large file
            TestUtil.IsTrue(new FileInfo(path).Length > 1024);

            // reach the period, file will be reset
            for (int i = 0; i < 10; i++)
            {
                log.WriteLog("123456789012345");
            }

            // now the file will have been reset
            TestUtil.IsTrue(new FileInfo(path).Length < 1024);
        }

        static void TestMethod_ClassConfigsPersistedCommonUsage()
        {
            string path = Path.Combine(TestUtil.GetTestSubDirectory("testcfg"), "test.ini");
            Configs cfg = new Configs(path);
            cfg.LoadPersisted();

            // unset properties should return empty string
            TestUtil.IsEq("", cfg.Get(ConfigKey.EnablePersonalFeatures));
            TestUtil.IsEq("", cfg.Get(ConfigKey.EnableVerboseLogging));
            TestUtil.IsEq("", cfg.Get(ConfigKey.FilepathDeletedFilesDir));

            // from memory
            cfg.Set(ConfigKey.EnablePersonalFeatures, "data=with=equals=");
            cfg.Set(ConfigKey.EnableVerboseLogging, " data\twith\t tabs");
            TestUtil.IsEq("data=with=equals=", cfg.Get(ConfigKey.EnablePersonalFeatures));
            TestUtil.IsEq(" data\twith\t tabs", cfg.Get(ConfigKey.EnableVerboseLogging));
            TestUtil.IsEq("", cfg.Get(ConfigKey.FilepathDeletedFilesDir));

            // from disk
            cfg = new Configs(path);
            cfg.LoadPersisted();
            TestUtil.IsEq("data=with=equals=", cfg.Get(ConfigKey.EnablePersonalFeatures));
            TestUtil.IsEq(" data\twith\t tabs", cfg.Get(ConfigKey.EnableVerboseLogging));
            TestUtil.IsEq("", cfg.Get(ConfigKey.FilepathDeletedFilesDir));
        }

        static void TestMethod_ClassConfigsPersistedBools()
        {
            string path = Path.Combine(TestUtil.GetTestSubDirectory("testcfg"), "testbools.ini");
            Configs cfg = new Configs(path);

            // read and set bools
            TestUtil.IsEq(false, cfg.GetBool(ConfigKey.EnablePersonalFeatures));
            cfg.SetBool(ConfigKey.EnablePersonalFeatures, true);
            TestUtil.IsEq(true, cfg.GetBool(ConfigKey.EnablePersonalFeatures));
            cfg.SetBool(ConfigKey.EnablePersonalFeatures, false);
            TestUtil.IsEq(false, cfg.GetBool(ConfigKey.EnablePersonalFeatures));
        }

        static void TestMethod_ClassConfigsNewlinesShouldNotBeAccepted()
        {
            string path = Path.Combine(TestUtil.GetTestSubDirectory("testcfg"), "test.ini");
            Configs cfg = new Configs(path);
            TestUtil.AssertExceptionMessage(() => cfg.Set(
                ConfigKey.EnableVerboseLogging, "data\rnewline"), "cannot contain newline");
            TestUtil.AssertExceptionMessage(() => cfg.Set(
                ConfigKey.EnableVerboseLogging, "data\nnewline"), "cannot contain newline");
        }

        static void TestMethod_ClassConfigsInputBoxHistoryShouldHaveCorrespondingConfig()
        {
            // each enum value in InputBoxHistory should have a corresponding MRUvalue
            var checkUniqueness = new HashSet<int>();
            var count = 0;
            foreach (var historyKey in Enum.GetValues(
                typeof(InputBoxHistory)).Cast<InputBoxHistory>())
            {
                if (historyKey != InputBoxHistory.None)
                {
                    TestUtil.IsEq(true, Enum.TryParse<ConfigKey>(
                        "MRU" + historyKey.ToString(), out ConfigKey key));

                    TestUtil.IsTrue(key != ConfigKey.None);
                    checkUniqueness.Add((int)key);
                    count++;
                }
            }

            // check that InputBoxHistory keys are mapped to different ConfigKey keys.
            TestUtil.IsEq(count, checkUniqueness.Count);
        }

        static void TestMethod_ClassConfigsEnumsShouldHaveUniqueValues()
        {
            var listOfInts = Enum.GetValues(typeof(InputBoxHistory)).Cast<int>().ToList();
            var set = new HashSet<int>(listOfInts);
            TestUtil.IsEq(listOfInts.Count, set.Count);

            listOfInts = Enum.GetValues(typeof(ConfigKey)).Cast<int>().ToList();
            set = new HashSet<int>(listOfInts);
            TestUtil.IsEq(listOfInts.Count, set.Count);
        }

        static void TestMethod_FileListNavigation()
        {
            // create files
            var dir = TestUtil.GetTestSubDirectory("filelist");
            File.WriteAllText(Path.Combine(dir, "dd.png"), "content");
            File.WriteAllText(Path.Combine(dir, "cc.png"), "content");
            File.WriteAllText(Path.Combine(dir, "bb.png"), "content");
            File.WriteAllText(Path.Combine(dir, "aa.png"), "content");
            List<string> neighbors = new List<string>(new string[4]);

            { // test gonext, gofirst
                var nav = new FileListNavigation(dir, new string[] { ".png" }, true);
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);

                nav.GoNextOrPrev(true, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "bb.png"), nav.Current);
                TestUtil.IsStringArrayEq(
                    "%cc.png|%dd.png|%dd.png|%dd.png".Replace("%", dir + Utils.Sep), neighbors);

                nav.GoNextOrPrev(true, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "cc.png"), nav.Current);
                TestUtil.IsStringArrayEq(
                    "%dd.png|%dd.png|%dd.png|%dd.png".Replace("%", dir + Utils.Sep), neighbors);

                nav.GoNextOrPrev(true, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "dd.png"), nav.Current);
                TestUtil.IsStringArrayEq(
                    "%dd.png|%dd.png|%dd.png|%dd.png".Replace("%", dir + Utils.Sep), neighbors);

                nav.GoNextOrPrev(true, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "dd.png"), nav.Current);
                TestUtil.IsStringArrayEq(
                    "%dd.png|%dd.png|%dd.png|%dd.png".Replace("%", dir + Utils.Sep), neighbors);

                nav.GoFirst();
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);
            }

            { // test golast, goprev
                var nav = new FileListNavigation(dir, new string[] { ".png" }, true);
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);

                nav.GoLast();
                TestUtil.IsEq(Path.Combine(dir, "dd.png"), nav.Current);

                nav.GoNextOrPrev(false, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "cc.png"), nav.Current);
                TestUtil.IsStringArrayEq(
                    "%bb.png|%aa.png|%aa.png|%aa.png".Replace("%", dir + Utils.Sep), neighbors);

                nav.GoNextOrPrev(false, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "bb.png"), nav.Current);
                TestUtil.IsStringArrayEq(
                    "%aa.png|%aa.png|%aa.png|%aa.png".Replace("%", dir + Utils.Sep), neighbors);

                nav.GoNextOrPrev(false, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);
                TestUtil.IsStringArrayEq(
                    "%aa.png|%aa.png|%aa.png|%aa.png".Replace("%", dir + Utils.Sep), neighbors);

                nav.GoNextOrPrev(false, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);
                TestUtil.IsStringArrayEq(
                    "%aa.png|%aa.png|%aa.png|%aa.png".Replace("%", dir + Utils.Sep), neighbors);
            }

            { // test gonext when file is missing
                var nav = new FileListNavigation(dir, new string[] { ".png" }, true);
                nav.GoLast();
                nav.TrySetPath(Path.Combine(dir, "().png"), false);
                nav.GoNextOrPrev(true, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);
                TestUtil.IsStringArrayEq(
                    "%bb.png|%cc.png|%dd.png|%dd.png".Replace("%", dir + Utils.Sep), neighbors);

                nav.GoLast();
                nav.TrySetPath(Path.Combine(dir, "ab.png"), false);
                nav.GoNextOrPrev(true);
                TestUtil.IsEq(Path.Combine(dir, "bb.png"), nav.Current);

                nav.GoLast();
                nav.TrySetPath(Path.Combine(dir, "bc.png"), false);
                nav.GoNextOrPrev(true);
                TestUtil.IsEq(Path.Combine(dir, "cc.png"), nav.Current);

                nav.GoFirst();
                nav.TrySetPath(Path.Combine(dir, "zz.png"), false);
                nav.GoNextOrPrev(true);
                TestUtil.IsEq(Path.Combine(dir, "dd.png"), nav.Current);
            }

            { // test goprev when file is missing
                var nav = new FileListNavigation(dir, new string[] { ".png" }, true);
                nav.GoLast();
                nav.TrySetPath(Path.Combine(dir, "().png"), false);
                nav.GoNextOrPrev(false);
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);

                nav.GoLast();
                nav.TrySetPath(Path.Combine(dir, "bc.png"), false);
                nav.GoNextOrPrev(false);
                TestUtil.IsEq(Path.Combine(dir, "bb.png"), nav.Current);

                nav.GoLast();
                nav.TrySetPath(Path.Combine(dir, "cd.png"), false);
                nav.GoNextOrPrev(false);
                TestUtil.IsEq(Path.Combine(dir, "cc.png"), nav.Current);

                nav.GoFirst();
                nav.TrySetPath(Path.Combine(dir, "zz.png"), false);
                nav.GoNextOrPrev(false, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "dd.png"), nav.Current);
                TestUtil.IsStringArrayEq(
                    "%cc.png|%bb.png|%aa.png|%aa.png".Replace("%", dir + Utils.Sep), neighbors);
            }

            { // gonext and goprev after deleted file
                var nav = new FileListNavigation(dir, new string[] { ".png" }, true);
                nav.GoFirst();
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);
                File.Delete(Path.Combine(dir, "bb.png"));

                // call NotifyFileChanges, the test runs more quickly than event can be received
                nav.NotifyFileChanges();
                nav.GoNextOrPrev(true);
                TestUtil.IsEq(Path.Combine(dir, "cc.png"), nav.Current);
                nav.GoNextOrPrev(true);
                TestUtil.IsEq(Path.Combine(dir, "dd.png"), nav.Current);
                File.Delete(Path.Combine(dir, "cc.png"));
                nav.NotifyFileChanges();
                nav.GoNextOrPrev(false);
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);

                // go down to 1 file
                File.Delete(Path.Combine(dir, "dd.png"));
                nav.NotifyFileChanges();
                nav.GoLast();
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);
                nav.GoNextOrPrev(true);
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);
                nav.GoNextOrPrev(false);
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);

                // go down to no files
                File.Delete(Path.Combine(dir, "aa.png"));
                nav.NotifyFileChanges();
                nav.GoNextOrPrev(true);
                TestUtil.IsEq(null, nav.Current);
                nav.GoNextOrPrev(false);
                TestUtil.IsEq(null, nav.Current);
                nav.GoFirst();
                TestUtil.IsEq(null, nav.Current);
                nav.GoLast();
                TestUtil.IsEq(null, nav.Current);

                // recover from no files
                File.WriteAllText(Path.Combine(dir, "new.png"), "content");
                nav.NotifyFileChanges();
                nav.GoNextOrPrev(true);
                TestUtil.IsEq(Path.Combine(dir, "new.png"), nav.Current);
                nav.GoNextOrPrev(false);
                TestUtil.IsEq(Path.Combine(dir, "new.png"), nav.Current);
            }
        }

        static void TestMethod_ImageCache()
        {
            var dir = TestUtil.GetTestSubDirectory("imcache");
            for (int i = 0; i < 8; i++)
            {
                File.WriteAllText(Path.Combine(dir, "a" + i + ".png"), "fake image");
                File.WriteAllText(Path.Combine(dir, "b" + i + ".png"), "fake image");
            }

            // provide callbacks that record what was removed.
            const int cacheSize = 3;
            List<object> removedFromCache = new List<object>();
            Func<Bitmap, bool> canDisposeBitmap =
                (bmp) =>
                {
                    removedFromCache.Add(bmp);
                    return true;
                };
            Func<Action, bool> callbackOnUiThread =
                (act) =>
                {
                    act();
                    return true;
                };

            // standard lookup
            using (var cache = new ImageCache(20, 20, cacheSize,
                callbackOnUiThread, canDisposeBitmap, null))
            {
                // retrieve from the cache
                var bmp1 = cache.Get(Path.Combine(dir, "a1.png"), out int gotW, out int gotH);
                TestUtil.IsEq(1, gotW);
                TestUtil.IsEq(1, gotH);

                // retrieving same path from the cache should return the exact same image
                var bmp1Same = cache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                TestUtil.IsEq(1, gotW);
                TestUtil.IsEq(1, gotH);
                TestUtil.IsEq((object)bmp1, (object)bmp1Same);

                // however, if lmt has changed, cached copy should be refreshed.
                var wasTime = File.GetLastWriteTime(Path.Combine(dir, "a1.png"));
                File.SetLastWriteTime(
                    Path.Combine(dir, "a1.png"), wasTime - new TimeSpan(0, 0, 10));
                var bmp1Changed = cache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                TestUtil.IsEq(1, gotW);
                TestUtil.IsEq(1, gotH);
                TestUtil.IsTrue((object)bmp1 != (object)bmp1Changed);

                // and further lookups should get this new copy.
                var bmp1ChangedAfter = cache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                TestUtil.IsEq((object)bmp1ChangedAfter, (object)bmp1Changed);
            }

            // add past the limit
            using (var cache = new ImageCache(20, 20, cacheSize,
                callbackOnUiThread, canDisposeBitmap, null))
            {
                // fill up cache
                removedFromCache.Clear();
                int gotW = 0, gotH = 0;
                var bmp1 = cache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                var bmp2 = cache.Get(Path.Combine(dir, "a2.png"), out gotW, out gotH);
                var bmp3 = cache.Get(Path.Combine(dir, "a3.png"), out gotW, out gotH);
                TestUtil.IsEq(0, removedFromCache.Count);

                // add one more
                var bmp4 = cache.Get(Path.Combine(dir, "a4.png"), out gotW, out gotH);
                TestUtil.IsEq(1, removedFromCache.Count);
                TestUtil.IsEq((object)bmp1, (object)removedFromCache[0]);

                // bmp4 should now be in the cache
                var bmp4Again = cache.Get(Path.Combine(dir, "a4.png"), out gotW, out gotH);
                TestUtil.IsEq((object)bmp4Again, (object)bmp4);

                // add one more
                var bmp5 = cache.Get(Path.Combine(dir, "a5.png"), out gotW, out gotH);
                TestUtil.IsEq(2, removedFromCache.Count);
                TestUtil.IsEq((object)bmp2, (object)removedFromCache[1]);

                // add many
                removedFromCache.Clear();
                cache.Add(new string[] { Path.Combine(dir, "b1.png"),
                    Path.Combine(dir, "b2.png"), Path.Combine(dir, "b3.png"),
                    Path.Combine(dir, "b4.png"), Path.Combine(dir, "b5.png") });
                TestUtil.IsEq(5, removedFromCache.Count);
                TestUtil.IsEq((object)bmp3, (object)removedFromCache[4]);
                TestUtil.IsEq((object)bmp4, (object)removedFromCache[3]);
                TestUtil.IsEq((object)bmp5, (object)removedFromCache[2]);
            }
        }

        static void TestMethod_ImageViewExcerptCoordinates()
        {
            using (var excerpt = new ImageViewExcerpt(160, 120))
            using (var bitmapFull = new Bitmap(200, 240))
            {
                excerpt.GetShiftAmount(bitmapFull, clickX: 30, clickY: 40,
                    widthOfResizedImage: 150, heightOfResizedImage: 110,
                    shiftX: out int shiftx, shiftY: out int shifty);

                TestUtil.IsEq(-40, shiftx);
                TestUtil.IsEq(27, shifty);

                excerpt.GetShiftAmount(bitmapFull, clickX: 90, clickY: 20,
                    widthOfResizedImage: 150, heightOfResizedImage: 110,
                    shiftX: out shiftx, shiftY: out shifty);

                TestUtil.IsEq(40, shiftx);
                TestUtil.IsEq(-17, shifty);
            }
        }

        static void TestMethod_CategoriesStringToTuple()
        {
            { // should be valid to have no categories defined
                var tuples = ModeUtils.CategoriesStringToTuple("");
                TestUtil.IsEq(0, tuples.Length);
            }

            { // typical valid categories string
                var tuples = ModeUtils.CategoriesStringToTuple(
                    "A/categoryReadable/categoryId|B/categoryReadable2/categoryId2");

                TestUtil.IsEq(2, tuples.Length);
                TestUtil.IsEq("A", tuples[0].Item1);
                TestUtil.IsEq("categoryReadable", tuples[0].Item2);
                TestUtil.IsEq("categoryId", tuples[0].Item3);
                TestUtil.IsEq("B", tuples[1].Item1);
                TestUtil.IsEq("categoryReadable2", tuples[1].Item2);
                TestUtil.IsEq("categoryId2", tuples[1].Item3);
            }
        }

        static void TestMethod_CategorizeOnCompletionAction()
        {
            // the completion action will create directories and move files
            var mode = new ModeCategorizeAndRename();
            var dir = TestUtil.GetTestSubDirectory("categorize");
            File.WriteAllText(Path.Combine(dir, "t1.png"), "1234");
            File.WriteAllText(Path.Combine(dir, "t2__MARKAS__art.png"), "1234");
            File.WriteAllText(Path.Combine(dir, "t3__MARKAS__art.jpg"), "1234");
            File.WriteAllText(Path.Combine(dir, "t4__MARKAS__other.png"), "1234");
            File.WriteAllText(Path.Combine(dir, "t5__MARKAS__badfiletype.wav"), "1234");
            File.WriteAllText(Path.Combine(dir, "t6__MARKAS__unknowncategory.jpg"), "1234");
            var prevCategories = Configs.Current.Get(ConfigKey.CategoriesModeCategorizeAndRename);
            try
            {
                Configs.Current.Set(ConfigKey.CategoriesModeCategorizeAndRename, mode.GetDefaultCategories());
                using (var form = new FormGallery(mode, dir))
                {
                    form.CallCompletionAction();
                    TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1.png")));
                    TestUtil.IsTrue(File.Exists(Path.Combine(dir, "art", "t2.png")));
                    TestUtil.IsTrue(File.Exists(Path.Combine(dir, "art", "t3.jpg")));
                    TestUtil.IsTrue(File.Exists(Path.Combine(dir, "other", "t4.png")));
                    TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t5__MARKAS__badfiletype.wav")));
                    TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t6__MARKAS__unknowncategory.jpg")));
                }
            }
            finally
            {
                Configs.Current.Set(ConfigKey.CategoriesModeCategorizeAndRename, prevCategories);
            }
        }

        static void TestMethod_UndoStack()
        {
            UndoStack<int?> stack = new UndoStack<int?>();
            TestUtil.IsEq(null, stack.PeekUndo());
            TestUtil.IsEq(null, stack.PeekRedo());
            stack.Add(1);
            TestUtil.IsEq(1, stack.PeekUndo().Value);
            stack.Add(2);
            TestUtil.IsEq(2, stack.PeekUndo().Value);
            stack.Add(3);
            TestUtil.IsEq(3, stack.PeekUndo().Value);
            stack.Add(4);
            TestUtil.IsEq(4, stack.PeekUndo().Value);
            stack.Undo();
            TestUtil.IsEq(3, stack.PeekUndo().Value);
            stack.Undo();
            TestUtil.IsEq(2, stack.PeekUndo().Value);
            stack.Undo();
            TestUtil.IsEq(1, stack.PeekUndo().Value);
            stack.Redo();
            TestUtil.IsEq(2, stack.PeekUndo().Value);
            stack.Redo();
            TestUtil.IsEq(3, stack.PeekUndo().Value);

            // not at the top of stack, will overwrite other values
            stack.Add(40);
            TestUtil.IsEq(40, stack.PeekUndo().Value);

            stack.Add(50);
            TestUtil.IsEq(50, stack.PeekUndo().Value);
            stack.Undo();
            TestUtil.IsEq(40, stack.PeekUndo().Value);
            stack.Undo();
            TestUtil.IsEq(3, stack.PeekUndo().Value);
            stack.Undo();
            TestUtil.IsEq(2, stack.PeekUndo().Value);
            stack.Undo();
            TestUtil.IsEq(1, stack.PeekUndo().Value);
            stack.Undo();
            TestUtil.IsEq(null, stack.PeekUndo());
            stack.Undo();
            TestUtil.IsEq(null, stack.PeekUndo());
        }

        static void TestMethod_FormWrapMoveFile()
        {
            var dir = TestUtil.GetTestSubDirectory("movefile");
            File.WriteAllText(Path.Combine(dir, "t1.bmp"), "1234");
            File.WriteAllText(Path.Combine(dir, "t2.bmp"), "123456789");
            var mode = new ModeCheckFilesizes();
            using (var form = new FormGallery(mode, dir))
            {
                // ok to call undo when undo stack is empty.
                form.UndoOrRedo(true);

                // wrapmovefile should be able to change file case
                TestUtil.IsTrue(form.WrapMoveFile(
                    Path.Combine(dir, "t1.bmp"), Path.Combine(dir, "T1.bmp")));
                TestUtil.IsTrue(form.WrapMoveFile(
                    Path.Combine(dir, "T1.bmp"), Path.Combine(dir, "t1.bmp")));

                // call wrapmovefile
                TestUtil.IsTrue(form.WrapMoveFile(
                    Path.Combine(dir, "t1.bmp"), Path.Combine(dir, "t1m.bmp")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t1.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1m.bmp")));

                // call Undo
                form.UndoOrRedo(true);
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t1m.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1.bmp")));

                // call WrapMoveFile
                TestUtil.IsTrue(form.WrapMoveFile(
                    Path.Combine(dir, "t1.bmp"), Path.Combine(dir, "t1m.bmp")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t1.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1m.bmp")));

                TestUtil.IsTrue(form.WrapMoveFile(
                    Path.Combine(dir, "t2.bmp"), Path.Combine(dir, "t2m.bmp")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t2.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t2m.bmp")));

                // call Undo
                form.UndoOrRedo(true);
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t2m.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t2.bmp")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t1.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1m.bmp")));
                form.UndoOrRedo(true);
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t1m.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1.bmp")));

                // call Redo
                form.UndoOrRedo(false);
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1m.bmp")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t1.bmp")));
                form.UndoOrRedo(true);
            }
        }

        static void TestMethod_AutoAcceptSmallFiles()
        {
            var dir = TestUtil.GetTestSubDirectory("movefile");
            File.WriteAllText(Path.Combine(dir, "t1.bmp"), "1234");
            File.WriteAllText(Path.Combine(dir, "t2.bmp"), "123456789");
            File.WriteAllText(Path.Combine(dir, "a1.jpg"), "1");
            File.WriteAllText(Path.Combine(dir, "a2.jpg"), "1234");
            File.WriteAllText(Path.Combine(dir, "a3.jpg"), "123456789");

            var mode = new ModeCheckFilesizes();
            using (var form = new FormGallery(mode, dir))
            {
                // test AutoAcceptSmallFiles
                var acceptFilesSmallerThanBytes = 6;
                mode.AutoAcceptSmallFiles(form, acceptFilesSmallerThanBytes, acceptFilesSmallerThanBytes);
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t2.bmp")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "a1.jpg")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "a2.jpg")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "a3.jpg")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "a1__MARKAS__size is good.jpg")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "a2__MARKAS__size is good.jpg")));
            }
        }

        static void TestMethod_CheckKeyBindings()
        {
            var bindings = new Dictionary<string, string>
            {
                { "1", "category 1" },
                { "F", "category f" }
            };

            TestUtil.IsEq(null,
                FormGallery.CheckKeyBindingsToAssignCategory(Keys.F1, bindings));
            TestUtil.IsEq(null,
                FormGallery.CheckKeyBindingsToAssignCategory(Keys.Oem1, bindings));
            TestUtil.IsEq(null,
                FormGallery.CheckKeyBindingsToAssignCategory(Keys.G, bindings));
            TestUtil.IsEq("category 1",
                FormGallery.CheckKeyBindingsToAssignCategory(Keys.D1, bindings));
            TestUtil.IsEq("category f",
                FormGallery.CheckKeyBindingsToAssignCategory(Keys.F, bindings));
        }

        static void TestMethod_PersistMostRecentlyUsedList()
        {
            string path = Path.Combine(TestUtil.GetTestSubDirectory("testcfg"), "testmru.ini");
            Configs cfg = new Configs(path);

            // using None should be a no-op
            var mruNone = new PersistMostRecentlyUsedList(InputBoxHistory.None, cfg);
            TestUtil.IsStringArrayEq(null, mruNone.Get());
            mruNone.AddToHistory("abcd");
            TestUtil.IsStringArrayEq(null, mruNone.Get());

            // try to set invalid data
            var mruTestInvalid = new PersistMostRecentlyUsedList(InputBoxHistory.RenameImage, cfg);
            TestUtil.IsStringArrayEq(null, mruTestInvalid.Get());
            mruTestInvalid.AddToHistory("contains||||");
            TestUtil.IsStringArrayEq(null, mruTestInvalid.Get());
            mruTestInvalid.AddToHistory("");
            TestUtil.IsStringArrayEq(null, mruTestInvalid.Get());
            var longText = Enumerable.Range(0, 500).Select(x => x.ToString());
            mruTestInvalid.AddToHistory(string.Join("a", longText));
            TestUtil.IsStringArrayEq(null, mruTestInvalid.Get());

            // add, without first calling Get
            var mruTestAddFirst = new PersistMostRecentlyUsedList(InputBoxHistory.RenameImage, cfg);
            mruTestAddFirst.AddToHistory("abc");
            TestUtil.IsStringArrayEq("abc", mruTestAddFirst.Get());

            // set valid data
            var mruTest = new PersistMostRecentlyUsedList(InputBoxHistory.RenameImage, cfg, 10);
            TestUtil.IsStringArrayEq("abc", mruTest.Get());
            mruTest.AddToHistory("def");
            TestUtil.IsStringArrayEq("def|abc", mruTest.Get());
            mruTest.AddToHistory("ghi");
            TestUtil.IsStringArrayEq("ghi|def|abc", mruTest.Get());

            // redundant should not be additional, but still moved to top
            mruTest.AddToHistory("abc");
            TestUtil.IsStringArrayEq("abc|ghi|def", mruTest.Get());
            mruTest.AddToHistory("def");
            TestUtil.IsStringArrayEq("def|abc|ghi", mruTest.Get());
            mruTest.AddToHistory("def");
            TestUtil.IsStringArrayEq("def|abc|ghi", mruTest.Get());

            // length should be capped at 10.
            for (int i = 0; i <= 10; i++)
            {
                mruTest.AddToHistory(i.ToString());
            }

            TestUtil.IsStringArrayEq("10|9|8|7|6|5|4|3|2|1", mruTest.Get());
        }
    }
}
