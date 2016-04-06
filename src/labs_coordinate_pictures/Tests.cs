// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    [Serializable]
    public class CoordinatePicturesTestException : ApplicationException
    {
        public CoordinatePicturesTestException(string message)
            : base(message)
        {
        }
    }

    public static class TestUtil
    {
        // expect an exception to occur when running the action, the exception should have the string in its message.
        public static void AssertExceptionMessage(Action fn, string strExpect)
        {
            string strException = null;
            try
            {
                fn();
            }
            catch (Exception exc)
            {
                strException = exc.ToString();
            }

            if (strException == null || !strException.Contains(strExpect))
            {
                throw new CoordinatePicturesTestException("Testing.AssertExceptionMessageIncludes expected " +
                    strExpect + " but got " + strException + ".");
            }
        }

        public static void IsEq(object expected, object actual)
        {
            // use a specific token to make sure that AreEq(null, null) works.
            object nullToken = new object();
            expected = expected ?? nullToken;
            actual = actual ?? nullToken;

            if (!expected.Equals(actual))
            {
                throw new Exception("Assertion failure, expected " + expected + " but got " + actual);
            }
        }

        public static void IsTrue(bool actual)
        {
            IsEq(true, actual);
        }

        public static void IsStringArrayEq(string strexpected, IList<string> actual)
        {
            if (strexpected == null)
            {
                IsTrue(actual == null || actual.Count == 0);
            }
            else
            {
                var expected = strexpected.Split(new char[] { '|' });
                IsEq(expected.Length, actual.Count);
                for (int i = 0; i < expected.Length; i++)
                {
                    IsEq(expected[i], actual[i]);
                }
            }
        }

        // use reflection to call all methods that start with TestMethod_
        public static void CallAllTestMethods(Type t, object[] arParams)
        {
            MethodInfo[] methodInfos = t.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
            var sortedMethods = methodInfos.OrderBy(item => item.Name);
            foreach (MethodInfo methodInfo in sortedMethods)
            {
                if (methodInfo.Name.StartsWith("TestMethod_"))
                {
                    TestUtil.IsTrue(methodInfo.GetParameters().Length == 0);
                    methodInfo.Invoke(null, arParams);
                }
            }
        }

        public static string GetTestWriteDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), "test_labs_coordinate_pictures");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public static string GetTestSubDirectory(string dirname)
        {
            string path = Path.Combine(GetTestWriteDirectory(), dirname);
            Directory.CreateDirectory(path);
            return path;
        }
    }

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
            TestUtil.AssertExceptionMessage(() => TestUtil.IsEq(1, 2), "expected 1 but got 2");
        }

        static void TestMethod_Asserts_NonEqualStrsShouldCompareNonEqual()
        {
            TestUtil.AssertExceptionMessage(() => TestUtil.IsEq("abcd", "abce"), "expected abcd but got abce");
        }

        static void TestMethod_Asserts_NonEqualBoolsShouldCompareNonEqual()
        {
            TestUtil.AssertExceptionMessage(() => TestUtil.IsEq(true, false), "expected True but got False");
        }

        static void TestMethod_UtilsSameExceptExtension()
        {
            TestUtil.IsTrue(FilenameUtils.SameExceptExtension("a\\test6.jpg", "a\\test6.jpg"));
            TestUtil.IsTrue(FilenameUtils.SameExceptExtension("a\\test6.jpg", "a\\test6.png"));
            TestUtil.IsTrue(FilenameUtils.SameExceptExtension("a\\test6.jpg", "a\\test6.BMP"));
            TestUtil.IsTrue(!FilenameUtils.SameExceptExtension("a\\test6.jpg", "b\\test6.jpg"));
            TestUtil.IsTrue(!FilenameUtils.SameExceptExtension("a\\test6.jpg", "a\\test6.jpg.jpg"));
            TestUtil.IsTrue(!FilenameUtils.SameExceptExtension("a\\test6a.jpg", "a\\test6.jpg"));
            TestUtil.IsTrue(FilenameUtils.SameExceptExtension("b\\aa.jpg.test6.jpg", "b\\aa.jpg.test6.bmp"));
            TestUtil.IsTrue(!FilenameUtils.SameExceptExtension("b\\aa.jpg.test6.jpg", "b\\aa.bmp.test6.jpg"));
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

        static void TestMethod_UtilsLastTwoChars()
        {
            TestUtil.IsEq("", Utils.FirstTwoChars(""));
            TestUtil.IsEq("a", Utils.FirstTwoChars("a"));
            TestUtil.IsEq("ab", Utils.FirstTwoChars("ab"));
            TestUtil.IsEq("ab", Utils.FirstTwoChars("abc"));
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
            TestUtil.IsEq("\"a\\\\b c\"", Utils.CombineProcessArguments(new string[] { "a\\\\b c" }));
            TestUtil.IsEq("\" \\\\\"", Utils.CombineProcessArguments(new string[] { " \\" }));
            TestUtil.IsEq("\" \\\\\\\"\"", Utils.CombineProcessArguments(new string[] { " \\\"" }));
            TestUtil.IsEq("\" \\\\\\\\\"", Utils.CombineProcessArguments(new string[] { " \\\\" }));
            TestUtil.IsEq("\"C:\\Program Files\\\\\"", Utils.CombineProcessArguments(new string[] { "C:\\Program Files\\" }));
            TestUtil.IsEq("\"dafc\\\"\\\"\\\"a\"", Utils.CombineProcessArguments(new string[] { "dafc\"\"\"a" }));
        }

        static void TestMethod_UtilsGetFirstHttpLink()
        {
            TestUtil.IsEq(null, Utils.GetFirstHttpLink(""));
            TestUtil.IsEq(null, Utils.GetFirstHttpLink("no urls present http none"));
            TestUtil.IsEq("http://www.ok.com", Utils.GetFirstHttpLink("http://www.ok.com"));
            TestUtil.IsEq("http://www.ok.com", Utils.GetFirstHttpLink("http://www.ok.com a b c http://www.second.com"));
            TestUtil.IsEq("http://www.ok.com", Utils.GetFirstHttpLink("a b c http://www.ok.com a b c http://www.second.com"));
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
            TestUtil.IsEq(@"c:\test\([0000])abc.jpg", FilenameUtils.AddNumberedPrefix(@"c:\test\abc.jpg", 0));
            TestUtil.IsEq(@"c:\test\([0010])abc.jpg", FilenameUtils.AddNumberedPrefix(@"c:\test\abc.jpg", 1));
            TestUtil.IsEq(@"c:\test\([1230])abc.jpg", FilenameUtils.AddNumberedPrefix(@"c:\test\abc.jpg", 123));
            TestUtil.IsEq(@"c:\test\([1230])abc.jpg", FilenameUtils.AddNumberedPrefix(@"c:\test\([1230])abc.jpg", 123));
            TestUtil.IsEq(@"c:\test\([9999])abc.jpg", FilenameUtils.AddNumberedPrefix(@"c:\test\([9999])abc.jpg", 123));
            TestUtil.IsEq(@"a.jpg", FilenameUtils.GetFileNameWithoutNumberedPrefix(@"a.jpg"));
            TestUtil.IsEq(@"abc.jpg", FilenameUtils.GetFileNameWithoutNumberedPrefix(@"c:\test\([9999])abc.jpg"));
            TestUtil.IsEq(@"abc.jpg", FilenameUtils.GetFileNameWithoutNumberedPrefix(@"c:\test\([0000])abc.jpg"));
            TestUtil.IsEq(@"abc.jpg", FilenameUtils.GetFileNameWithoutNumberedPrefix(@"c:\test\([1230])abc.jpg"));
        }

        static void TestMethod_UtilsAddMark()
        {
            var testAdd = FilenameUtils.AddMarkToFilename(@"c:\dir\test\b b.aaa.jpg", "mk");
            TestUtil.IsEq(@"c:\dir\test\b b.aaa__MARKAS__mk.jpg", testAdd);
            testAdd = FilenameUtils.AddMarkToFilename(@"c:\dir\test\b b.aaa.jpg", "");
            TestUtil.IsEq(@"c:\dir\test\b b.aaa__MARKAS__.jpg", testAdd);

            Func<string, string> testGetMark = (input) =>
            {
                string pathWithoutCategory, category;
                FilenameUtils.GetMarkFromFilename(input, out pathWithoutCategory, out category);
                return pathWithoutCategory + "|" + category;
            };

            TestUtil.IsEq(@"C:\dir\test\file.jpg|123", testGetMark(@"C:\dir\test\file__MARKAS__123.jpg"));
            TestUtil.IsEq(@"C:\dir\test\file.also.jpg|123", testGetMark(@"C:\dir\test\file.also__MARKAS__123.jpg"));
            TestUtil.IsEq(@"C:\dir\test\file.jpg|", testGetMark(@"C:\dir\test\file__MARKAS__.jpg"));
            TestUtil.AssertExceptionMessage(() => testGetMark(@"C:\dir\test\dirmark__MARKAS__b\file__MARKAS__123.jpg"), "Directories");
            TestUtil.AssertExceptionMessage(() => testGetMark(@"C:\dir\test\dirmark__MARKAS__b\file.jpg"), "Directories");
            TestUtil.AssertExceptionMessage(() => testGetMark(@"C:\dir\test\file__MARKAS__123__MARKAS__123.jpg"), "exactly 1");
            TestUtil.AssertExceptionMessage(() => testGetMark(@"C:\dir\test\file.jpg"), "exactly 1");
            TestUtil.AssertExceptionMessage(() => testGetMark(@"C:\dir\test\file__MARKAS__123.dir.jpg"), "after the marker");
        }

        static void TestMethod_FindSimilarFilenames()
        {
            var mode = new ModeCategorizeAndRename();
            bool hasMiddleName;
            string newname;
            var allfiles = new string[] { "c:\\a\\a.png", "c:\\a\\b.png", "c:\\a\\ab.png", "c:\\a\\b_out.png", "c:\\a\\a_out.png", "c:\\a\\a.png60.jpg", "c:\\a\\a.png80.jpg", "c:\\b\\a.png90.jpg" };

            // alone with no middlename
            TestUtil.IsStringArrayEq(null, FilenameFindSimilarFilenames.FindSimilarNames("c:\\a\\b.png", mode.GetFileTypes(), allfiles, out hasMiddleName, out newname));
            TestUtil.IsTrue(!hasMiddleName);
            TestUtil.IsEq(null, newname);

            // alone with a middlename
            TestUtil.IsStringArrayEq(null, FilenameFindSimilarFilenames.FindSimilarNames("c:\\b\\a.png90.jpg", mode.GetFileTypes(), allfiles, out hasMiddleName, out newname));
            TestUtil.IsTrue(hasMiddleName);
            TestUtil.IsEq("c:\\b\\a.jpg", newname);

            // has similar names with no middlename
            TestUtil.IsStringArrayEq("c:\\a\\a.png60.jpg|c:\\a\\a.png80.jpg", FilenameFindSimilarFilenames.FindSimilarNames("c:\\a\\a.png", mode.GetFileTypes(), allfiles, out hasMiddleName, out newname));
            TestUtil.IsTrue(!hasMiddleName);
            TestUtil.IsEq(null, newname);

            // has similar names with a middlename
            TestUtil.IsStringArrayEq("c:\\a\\a.png|c:\\a\\a.png80.jpg", FilenameFindSimilarFilenames.FindSimilarNames("c:\\a\\a.png60.jpg", mode.GetFileTypes(), allfiles, out hasMiddleName, out newname));
            TestUtil.IsTrue(hasMiddleName);
            TestUtil.IsEq("c:\\a\\a.jpg", newname);
        }
        
        static void TestMethod_ClassConfigsPersistedCommonUsage()
        {
            string path = Path.Combine(TestUtil.GetTestSubDirectory("testcfg"), "test.ini");
            Configs cfg = new Configs(path);
            cfg.LoadPersisted();
            TestUtil.IsEq("", cfg.Get(ConfigKey.EnablePersonalFeatures));
            TestUtil.IsEq("", cfg.Get(ConfigKey.FilepathPython));
            TestUtil.IsEq("", cfg.Get(ConfigKey.FilepathTrash));
            cfg.Set(ConfigKey.EnablePersonalFeatures, "data=with=equals=");
            cfg.Set(ConfigKey.FilepathPython, " data\twith\t tabs");

            // from memory
            TestUtil.IsEq("data=with=equals=", cfg.Get(ConfigKey.EnablePersonalFeatures));
            TestUtil.IsEq(" data\twith\t tabs", cfg.Get(ConfigKey.FilepathPython));
            TestUtil.IsEq("", cfg.Get(ConfigKey.FilepathTrash));

            // from disk
            cfg = new Configs(path);
            cfg.LoadPersisted();
            TestUtil.IsEq("data=with=equals=", cfg.Get(ConfigKey.EnablePersonalFeatures));
            TestUtil.IsEq(" data\twith\t tabs", cfg.Get(ConfigKey.FilepathPython));
            TestUtil.IsEq("", cfg.Get(ConfigKey.FilepathTrash));
        }

        static void TestMethod_ClassConfigsPersistedBools()
        {
            string path = Path.Combine(TestUtil.GetTestSubDirectory("testcfg"), "testbools.ini");
            Configs cfg = new Configs(path);
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
            TestUtil.AssertExceptionMessage(
                () => cfg.Set(ConfigKey.FilepathPython, "data\rnewline"), "cannot contain newline");
            TestUtil.AssertExceptionMessage(
                () => cfg.Set(ConfigKey.FilepathPython, "data\nnewline"), "cannot contain newline");
        }

        static void TestMethod_ClassConfigsInputBoxHistoryShouldHaveCorrespondingConfig()
        {
            // each enum value in InputBoxHistory should have a corresponding MRUvalue
            foreach (var en in Enum.GetValues(typeof(InputBoxHistory)).Cast<InputBoxHistory>())
            {
                if (en != InputBoxHistory.None)
                {
                    var s = "MRU" + en.ToString();
                    ConfigKey key;
                    TestUtil.IsEq(true, Enum.TryParse<ConfigKey>(s, out key));
                }
            }
        }

        static void TestMethod_FileListNavigation()
        {
            // setup
            var dir = TestUtil.GetTestSubDirectory("filelist");
            File.WriteAllText(Path.Combine(dir, "dd.png"), "content");
            File.WriteAllText(Path.Combine(dir, "cc.png"), "content");
            File.WriteAllText(Path.Combine(dir, "bb.png"), "content");
            File.WriteAllText(Path.Combine(dir, "aa.png"), "content");
            List<string> neighbors = new List<string>(new string[4]);

            { // gonext, gofirst
                var nav = new FileListNavigation(dir, new string[] { ".png" }, true);
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);
                nav.GoNextOrPrev(true, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "bb.png"), nav.Current);
                TestUtil.IsStringArrayEq("%cc.png|%dd.png|%dd.png|%dd.png".Replace("%", dir + "\\"), neighbors);
                nav.GoNextOrPrev(true, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "cc.png"), nav.Current);
                TestUtil.IsStringArrayEq("%dd.png|%dd.png|%dd.png|%dd.png".Replace("%", dir + "\\"), neighbors);
                nav.GoNextOrPrev(true, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "dd.png"), nav.Current);
                TestUtil.IsStringArrayEq("%dd.png|%dd.png|%dd.png|%dd.png".Replace("%", dir + "\\"), neighbors);
                nav.GoNextOrPrev(true, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "dd.png"), nav.Current);
                TestUtil.IsStringArrayEq("%dd.png|%dd.png|%dd.png|%dd.png".Replace("%", dir + "\\"), neighbors);
                nav.GoFirst();
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);
            }

            { // golast, goprev
                var nav = new FileListNavigation(dir, new string[] { ".png" }, true);
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);
                nav.GoLast();
                TestUtil.IsEq(Path.Combine(dir, "dd.png"), nav.Current);
                nav.GoNextOrPrev(false, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "cc.png"), nav.Current);
                TestUtil.IsStringArrayEq("%bb.png|%aa.png|%aa.png|%aa.png".Replace("%", dir + "\\"), neighbors);
                nav.GoNextOrPrev(false, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "bb.png"), nav.Current);
                TestUtil.IsStringArrayEq("%aa.png|%aa.png|%aa.png|%aa.png".Replace("%", dir + "\\"), neighbors);
                nav.GoNextOrPrev(false, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);
                TestUtil.IsStringArrayEq("%aa.png|%aa.png|%aa.png|%aa.png".Replace("%", dir + "\\"), neighbors);
                nav.GoNextOrPrev(false, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);
                TestUtil.IsStringArrayEq("%aa.png|%aa.png|%aa.png|%aa.png".Replace("%", dir + "\\"), neighbors);
            }

            { // gonext when file is missing
                var nav = new FileListNavigation(dir, new string[] { ".png" }, true);
                nav.GoLast();
                nav.TrySetPath(Path.Combine(dir, "().png"), false);
                nav.GoNextOrPrev(true, neighbors, neighbors.Count);
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);
                TestUtil.IsStringArrayEq("%bb.png|%cc.png|%dd.png|%dd.png".Replace("%", dir + "\\"), neighbors);

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

            { // goprev when file is missing
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
                TestUtil.IsStringArrayEq("%cc.png|%bb.png|%aa.png|%aa.png".Replace("%", dir + "\\"), neighbors);
            }

            { // gonext and goprev after deleted file
                var nav = new FileListNavigation(dir, new string[] { ".png" }, true);
                nav.GoFirst();
                TestUtil.IsEq(Path.Combine(dir, "aa.png"), nav.Current);
                File.Delete(Path.Combine(dir, "bb.png"));
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

            List<object> removedFromCache = new List<object>();
            const int cachesize = 3;
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

            { // standard lookup
                var imcache = new ImageCache(20, 20, cachesize,
                    callbackOnUiThread, canDisposeBitmap);

                // retrieve from the cache
                int gotW = 0, gotH = 0;
                var bmp1 = imcache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                TestUtil.IsEq(1, gotW);
                TestUtil.IsEq(1, gotH);

                // retrieving same path from the cache should return the exact same image
                var bmp1Same = imcache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                TestUtil.IsEq(1, gotW);
                TestUtil.IsEq(1, gotH);
                TestUtil.IsEq((object)bmp1, (object)bmp1Same);

                // however, if lmt has changed, cached copy should be refreshed.
                var wasTime = File.GetLastWriteTime(Path.Combine(dir, "a1.png"));
                File.SetLastWriteTime(Path.Combine(dir, "a1.png"), wasTime - new TimeSpan(0, 0, 10));
                var bmp1Changed = imcache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                TestUtil.IsEq(1, gotW);
                TestUtil.IsEq(1, gotH);
                TestUtil.IsTrue((object)bmp1 != (object)bmp1Changed);

                // and further lookups should get this new copy.
                var bmp1ChangedAfter = imcache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                TestUtil.IsEq((object)bmp1ChangedAfter, (object)bmp1Changed);

                // verify that comparisons work after dispose.
                bmp1.Dispose();
                bmp1Same.Dispose();
                bmp1Changed.Dispose();
                TestUtil.IsTrue((object)bmp1 != (object)bmp1Changed);
                TestUtil.IsTrue((object)bmp1Same != (object)bmp1Changed);
                TestUtil.IsEq((object)bmp1, (object)bmp1Same);
                TestUtil.IsEq((object)bmp1Same, (object)bmp1Same);
                TestUtil.IsEq((object)bmp1Same, (object)bmp1);
            }

            { // add past the limit
                var imcache = new ImageCache(20, 20, cachesize,
                    callbackOnUiThread, canDisposeBitmap);

                // fill up cache
                removedFromCache.Clear();
                int gotW = 0, gotH = 0;
                var bmp1 = imcache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                var bmp2 = imcache.Get(Path.Combine(dir, "a2.png"), out gotW, out gotH);
                var bmp3 = imcache.Get(Path.Combine(dir, "a3.png"), out gotW, out gotH);
                TestUtil.IsEq(0, removedFromCache.Count);

                // add one more
                var bmp4 = imcache.Get(Path.Combine(dir, "a4.png"), out gotW, out gotH);
                TestUtil.IsEq(1, removedFromCache.Count);
                TestUtil.IsEq((object)bmp1, (object)removedFromCache[0]);

                // bmp4 should now be in the cache
                var bmp4Again = imcache.Get(Path.Combine(dir, "a4.png"), out gotW, out gotH);
                TestUtil.IsEq((object)bmp4Again, (object)bmp4);

                // add one more
                var bmp5 = imcache.Get(Path.Combine(dir, "a5.png"), out gotW, out gotH);
                TestUtil.IsEq(2, removedFromCache.Count);
                TestUtil.IsEq((object)bmp2, (object)removedFromCache[1]);

                // add many
                removedFromCache.Clear();
                imcache.Add(new string[] { Path.Combine(dir, "b1.png"), Path.Combine(dir, "b2.png"), Path.Combine(dir, "b3.png"), Path.Combine(dir, "b4.png"), Path.Combine(dir, "b5.png") });
                TestUtil.IsEq(5, removedFromCache.Count);
                TestUtil.IsEq((object)bmp3, (object)removedFromCache[4]);
                TestUtil.IsEq((object)bmp4, (object)removedFromCache[3]);
                TestUtil.IsEq((object)bmp5, (object)removedFromCache[2]);
            }
        }

        static void TestMethod_ImageViewExcerptCoordinates()
        {
            using (var excerpt = new ImageViewExcerpt(160, 120))
            using (var mockFullImage = new Bitmap(200, 240))
            {
                int shiftx, shifty;
                excerpt.GetShiftAmount(mockFullImage, clickX: 30, clickY: 40, wasWidth: 150, wasHeight: 110, shiftx: out shiftx, shifty: out shifty);
                TestUtil.IsEq(-40, shiftx);
                TestUtil.IsEq(27, shifty);

                excerpt.GetShiftAmount(mockFullImage, clickX: 90, clickY: 20, wasWidth: 150, wasHeight: 110, shiftx: out shiftx, shifty: out shifty);
                TestUtil.IsEq(40, shiftx);
                TestUtil.IsEq(-17, shifty);
            }
        }

        static void TestMethod_CategoriesStringToTuple()
        {
            { // should be valid to have no categories defined
                var ret = ModeUtils.CategoriesStringToTuple("");
                TestUtil.IsEq(0, ret.Length);
            }

            { // typical valid categories string
                var ret = ModeUtils.CategoriesStringToTuple("A/categoryReadable/categoryId|B/categoryReadable2/categoryId2");
                TestUtil.IsEq(2, ret.Length);
                TestUtil.IsEq("A", ret[0].Item1);
                TestUtil.IsEq("categoryReadable", ret[0].Item2);
                TestUtil.IsEq("categoryId", ret[0].Item3);
                TestUtil.IsEq("B", ret[1].Item1);
                TestUtil.IsEq("categoryReadable2", ret[1].Item2);
                TestUtil.IsEq("categoryId2", ret[1].Item3);
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
            using (var form = new FormGallery(mode, dir))
            {
                form.CallCompletionAction();
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1.png")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "art", "t2.png")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "art", "t3.jpg")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "other", "t4.png")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t5__MARKAS__badfiletype.wav")));
            }
        }

        static void TestMethod_UndoStack()
        {
            UndoStack<int?> testUndo = new UndoStack<int?>();
            TestUtil.IsEq(null, testUndo.PeekUndo());
            TestUtil.IsEq(null, testUndo.PeekRedo());
            testUndo.Add(1);
            TestUtil.IsEq(1, testUndo.PeekUndo().Value);
            testUndo.Add(2);
            TestUtil.IsEq(2, testUndo.PeekUndo().Value);
            testUndo.Add(3);
            TestUtil.IsEq(3, testUndo.PeekUndo().Value);
            testUndo.Add(4);
            TestUtil.IsEq(4, testUndo.PeekUndo().Value);
            testUndo.Undo();
            TestUtil.IsEq(3, testUndo.PeekUndo().Value);
            testUndo.Undo();
            TestUtil.IsEq(2, testUndo.PeekUndo().Value);
            testUndo.Undo();
            TestUtil.IsEq(1, testUndo.PeekUndo().Value);
            testUndo.Redo();
            TestUtil.IsEq(2, testUndo.PeekUndo().Value);
            testUndo.Redo();
            TestUtil.IsEq(3, testUndo.PeekUndo().Value);

            // not at the top of stack, will overwrite other values
            testUndo.Add(40);
            TestUtil.IsEq(40, testUndo.PeekUndo().Value);

            testUndo.Add(50);
            TestUtil.IsEq(50, testUndo.PeekUndo().Value);
            testUndo.Undo();
            TestUtil.IsEq(40, testUndo.PeekUndo().Value);
            testUndo.Undo();
            TestUtil.IsEq(3, testUndo.PeekUndo().Value);
            testUndo.Undo();
            TestUtil.IsEq(2, testUndo.PeekUndo().Value);
            testUndo.Undo();
            TestUtil.IsEq(1, testUndo.PeekUndo().Value);
            testUndo.Undo();
            TestUtil.IsEq(null, testUndo.PeekUndo());
            testUndo.Undo();
            TestUtil.IsEq(null, testUndo.PeekUndo());
        }

        static void TestMethod_AutoAcceptSmallFilesAndWrapMoveFile()
        {
            var dir = TestUtil.GetTestSubDirectory("autoaccept");
            File.WriteAllText(Path.Combine(dir, "t1.bmp"), "1234");
            File.WriteAllText(Path.Combine(dir, "t2.bmp"), "123456789");
            File.WriteAllText(Path.Combine(dir, "a1.jpg"), "");
            File.WriteAllText(Path.Combine(dir, "a2.jpg"), "1234");
            File.WriteAllText(Path.Combine(dir, "a3.jpg"), "123456789");
            var mode = new ModeCheckFilesizes();
            using (var form = new FormGallery(mode, dir))
            {
                // ok to call undo when undo stack is empty.
                form.UndoOrRedo(true);

                // call wrapmovefile
                TestUtil.IsTrue(form.WrapMoveFile(Path.Combine(dir, "t1.bmp"), Path.Combine(dir, "t1m.bmp")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t1.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1m.bmp")));

                // call Undo
                form.UndoOrRedo(true);
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t1m.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1.bmp")));

                // call wrapmovefile
                TestUtil.IsTrue(form.WrapMoveFile(Path.Combine(dir, "t1.bmp"), Path.Combine(dir, "t1m.bmp")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t1.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1m.bmp")));
                TestUtil.IsTrue(form.WrapMoveFile(Path.Combine(dir, "t2.bmp"), Path.Combine(dir, "t2m.bmp")));
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

                // test AutoAcceptSmallFiles
                var acceptFilesSmallerThanBytes = 6;
                mode.AutoAcceptSmallFiles(form, acceptFilesSmallerThanBytes);
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t2.bmp")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "a1.jpg")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "a2.jpg")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "a3.jpg")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "a1__MARKAS__size is good.jpg")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "a2__MARKAS__size is good.jpg")));
            }
        }

        public static void RunTests()
        {
            string dir = TestUtil.GetTestWriteDirectory();
            Directory.Delete(dir, true);
            Configs.Current.SupressDialogs = true;
            try
            {
                TestUtil.CallAllTestMethods(typeof(CoordinatePicturesTests), null);
            }
            finally
            {
                Configs.Current.SupressDialogs = false;
            }
        }
    }
}
