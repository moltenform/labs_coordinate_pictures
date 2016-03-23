using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace labs_coordinate_pictures
{
    public class CoordinatePicturesTestException : ApplicationException
    {
        public CoordinatePicturesTestException(string message) : base(message) { }
    }

    public static class TestUtil
    {
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

        public static void AssertEqual(object expected, object actual)
        {
            // the nullToken compares equal to itself, but nothing else.
            object nullToken = new object();
            expected = expected ?? nullToken;
            actual = actual ?? nullToken;

            if (!expected.Equals(actual))
            {
                throw new Exception("Assertion failure, expected " + expected + " but got " + actual);
            }
        }

        public static void AssertTrue(bool actual)
        {
            AssertEqual(true, actual);
        }

        public static void AssertStringArrayEqual(string strexpected, IList<string> actual)
        {
            var expected = strexpected.Split(new char[] { '|' });
            AssertEqual(expected.Length, actual.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                AssertEqual(expected[i], actual[i]);
            }
        }

        public static void CallAllTestMethods(Type t, object[] arParams)
        {
            MethodInfo[] methodInfos = t.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
            foreach (MethodInfo methodInfo in methodInfos)
            {
                if (methodInfo.Name.StartsWith("TestMethod_"))
                {
                    Debug.Assert(methodInfo.GetParameters().Length == 0);
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
    }

    public static class CoordinatePicturesTests
    {
        static void TestMethod_Asserts_EqualIntsShouldCompareEqual()
        {
            TestUtil.AssertEqual(1, 1);
        }
        static void TestMethod_Asserts_EqualStringsShouldCompareEqual()
        {
            TestUtil.AssertEqual("abcd", "abcd");
        }
        static void TestMethod_Asserts_EqualBoolsShouldCompareEqual()
        {
            TestUtil.AssertEqual(true, true);
            TestUtil.AssertEqual(false, false);
        }
        static void TestMethod_Asserts_CheckAssertMessage()
        {
            Action fn = delegate () { throw new CoordinatePicturesTestException("test123"); };
            TestUtil.AssertExceptionMessage(fn, "test123");
        }
        static void TestMethod_Asserts_NonEqualIntsShouldCompareNonEqual()
        {
            TestUtil.AssertExceptionMessage(() => TestUtil.AssertEqual(1, 2), "expected 1 but got 2");
        }
        static void TestMethod_Asserts_NonEqualStrsShouldCompareNonEqual()
        {
            TestUtil.AssertExceptionMessage(() => TestUtil.AssertEqual("abcd", "abce"), "expected abcd but got abce");
        }
        static void TestMethod_Asserts_NonEqualBoolsShouldCompareNonEqual()
        {
            TestUtil.AssertExceptionMessage(() => TestUtil.AssertEqual(true, false), "expected True but got False");
        }

        static void TestMethod_UtilsLastTwoChars()
        {
            TestUtil.AssertEqual("", Utils.FirstTwoChars(""));
            TestUtil.AssertEqual("a", Utils.FirstTwoChars("a"));
            TestUtil.AssertEqual("ab", Utils.FirstTwoChars("ab"));
            TestUtil.AssertEqual("ab", Utils.FirstTwoChars("abc"));
        }

        static void TestMethod_UtilsCombineProcessArguments()
        {
            TestUtil.AssertEqual("", Utils.CombineProcessArguments(new string[] { }));
            TestUtil.AssertEqual("\"\"", Utils.CombineProcessArguments(new string[] { "" }));
            TestUtil.AssertEqual("\"\\\"\"", Utils.CombineProcessArguments(new string[] { "\"" }));
            TestUtil.AssertEqual("\"\\\"\\\"\"", Utils.CombineProcessArguments(new string[] { "\"\"" }));
            TestUtil.AssertEqual("\"\\\"a\\\"\"", Utils.CombineProcessArguments(new string[] { "\"a\"" }));
            TestUtil.AssertEqual("\\", Utils.CombineProcessArguments(new string[] { "\\" }));
            TestUtil.AssertEqual("\"a b\"", Utils.CombineProcessArguments(new string[] { "a b" }));
            TestUtil.AssertEqual("a \" b\"", Utils.CombineProcessArguments(new string[] { "a", " b" }));
            TestUtil.AssertEqual("a\\\\b", Utils.CombineProcessArguments(new string[] { "a\\\\b" }));
            TestUtil.AssertEqual("\"a\\\\b c\"", Utils.CombineProcessArguments(new string[] { "a\\\\b c" }));
            TestUtil.AssertEqual("\" \\\\\"", Utils.CombineProcessArguments(new string[] { " \\" }));
            TestUtil.AssertEqual("\" \\\\\\\"\"", Utils.CombineProcessArguments(new string[] { " \\\"" }));
            TestUtil.AssertEqual("\" \\\\\\\\\"", Utils.CombineProcessArguments(new string[] { " \\\\" }));
            TestUtil.AssertEqual("\"C:\\Program Files\\\\\"", Utils.CombineProcessArguments(new string[] { "C:\\Program Files\\" }));
            TestUtil.AssertEqual("\"dafc\\\"\\\"\\\"a\"", Utils.CombineProcessArguments(new string[] { "dafc\"\"\"a" }));
        }
        static void TestMethod_UtilsAddMark()
        {
            var test1 = FilenameUtils.AddMarkToFilename(@"c:\foo\test\bar bar.aaa.jpg", "mk");
            TestUtil.AssertEqual(@"c:\foo\test\bar bar.aaa__MARKAS__mk.jpg", test1);
            string testgetmark, testgetpath;
            FilenameUtils.GetMarkFromFilename(@"c:\foo\test\bar bar.aaa__MARKAS__mk.jpg", out testgetpath, out testgetmark);
            TestUtil.AssertEqual("mk", testgetmark);
            TestUtil.AssertEqual(@"c:\foo\test\bar bar.aaa.jpg", testgetpath);
        }

        static void TestMethod_ClassConfigsPersistedCommonUsage()
        {
            string path = Path.Combine(TestUtil.GetTestWriteDirectory(), "test.ini");
            Configs cfg = new Configs(path);
            cfg.LoadPersisted();
            TestUtil.AssertEqual("", cfg.Get(ConfigsPersistedKeys.EnablePersonalFeatures));
            TestUtil.AssertEqual("", cfg.Get(ConfigsPersistedKeys.FilepathPython));
            TestUtil.AssertEqual("", cfg.Get(ConfigsPersistedKeys.FilepathTrash));
            cfg.Set(ConfigsPersistedKeys.EnablePersonalFeatures, "data=with=equals=");
            cfg.Set(ConfigsPersistedKeys.FilepathPython, " data\twith\t tabs");
            
            /* from memory */
            TestUtil.AssertEqual("data=with=equals=", cfg.Get(ConfigsPersistedKeys.EnablePersonalFeatures));
            TestUtil.AssertEqual(" data\twith\t tabs", cfg.Get(ConfigsPersistedKeys.FilepathPython));
            TestUtil.AssertEqual("", cfg.Get(ConfigsPersistedKeys.FilepathTrash));
            
            /* from disk */
            cfg = new Configs(path);
            cfg.LoadPersisted();
            TestUtil.AssertEqual("data=with=equals=", cfg.Get(ConfigsPersistedKeys.EnablePersonalFeatures));
            TestUtil.AssertEqual(" data\twith\t tabs", cfg.Get(ConfigsPersistedKeys.FilepathPython));
            TestUtil.AssertEqual("", cfg.Get(ConfigsPersistedKeys.FilepathTrash));
        }

        static void TestMethod_ClassConfigsPersistedBools()
        {
            string path = Path.Combine(TestUtil.GetTestWriteDirectory(), "testbools.ini");
            Configs cfg = new Configs(path);
            TestUtil.AssertEqual(false, cfg.GetBool(ConfigsPersistedKeys.EnablePersonalFeatures));
            cfg.SetBool(ConfigsPersistedKeys.EnablePersonalFeatures, true);
            TestUtil.AssertEqual(true, cfg.GetBool(ConfigsPersistedKeys.EnablePersonalFeatures));
            cfg.SetBool(ConfigsPersistedKeys.EnablePersonalFeatures, false);
            TestUtil.AssertEqual(false, cfg.GetBool(ConfigsPersistedKeys.EnablePersonalFeatures));
        }

        static void TestMethod_ClassConfigsNewlinesShouldNotBeAccepted()
        {
            string path = Path.Combine(TestUtil.GetTestWriteDirectory(), "test.ini");
            Configs cfg = new Configs(path);
            TestUtil.AssertExceptionMessage(
                () => cfg.Set(ConfigsPersistedKeys.FilepathPython, "data\rnewline"), "cannot contain newline");
            TestUtil.AssertExceptionMessage(
                () => cfg.Set(ConfigsPersistedKeys.FilepathPython, "data\nnewline"), "cannot contain newline");
        }

        static void TestMethod_ClassConfigsInputBoxHistoryShouldHaveCorrespondingConfig()
        {
            // each enum value in InputBoxHistory should have a corresponding MRUvalue
            foreach (var en in Enum.GetValues(typeof(InputBoxHistory)).Cast<InputBoxHistory>())
            {
                if (en != InputBoxHistory.None)
                {
                    var s = "MRU" + en.ToString();
                    ConfigsPersistedKeys key;
                    TestUtil.AssertEqual(true, Enum.TryParse<ConfigsPersistedKeys>(s, out key));
                }
            }
        }

        static void TestMethod_FileListNavigation()
        {
            /* setup */
            Directory.Delete(TestUtil.GetTestWriteDirectory(), true);
            var dir = TestUtil.GetTestWriteDirectory();
            File.WriteAllText(Path.Combine(dir, "dd.png"), "content");
            File.WriteAllText(Path.Combine(dir, "cc.png"), "content");
            File.WriteAllText(Path.Combine(dir, "bb.png"), "content");
            File.WriteAllText(Path.Combine(dir, "aa.png"), "content");
            List<string> neighbors = new List<string>(new string[4]);

            { // gonext, gofirst
                var nav = new FileListNavigation(dir, new string[] { ".png" }, true);
                TestUtil.AssertEqual(Path.Combine(dir, "aa.png"), nav.Current);
                nav.GoNextOrPrev(true, neighbors, neighbors.Count);
                TestUtil.AssertEqual(Path.Combine(dir, "bb.png"), nav.Current);
                TestUtil.AssertStringArrayEqual("%cc.png|%dd.png|%dd.png|%dd.png".Replace("%", dir + "\\"), neighbors);
                nav.GoNextOrPrev(true, neighbors, neighbors.Count);
                TestUtil.AssertEqual(Path.Combine(dir, "cc.png"), nav.Current);
                TestUtil.AssertStringArrayEqual("%dd.png|%dd.png|%dd.png|%dd.png".Replace("%", dir + "\\"), neighbors);
                nav.GoNextOrPrev(true, neighbors, neighbors.Count);
                TestUtil.AssertEqual(Path.Combine(dir, "dd.png"), nav.Current);
                TestUtil.AssertStringArrayEqual("%dd.png|%dd.png|%dd.png|%dd.png".Replace("%", dir + "\\"), neighbors);
                nav.GoNextOrPrev(true, neighbors, neighbors.Count);
                TestUtil.AssertEqual(Path.Combine(dir, "dd.png"), nav.Current);
                TestUtil.AssertStringArrayEqual("%dd.png|%dd.png|%dd.png|%dd.png".Replace("%", dir + "\\"), neighbors);
                nav.GoFirst();
                TestUtil.AssertEqual(Path.Combine(dir, "aa.png"), nav.Current);
            }

            { // golast, goprev
                var nav = new FileListNavigation(dir, new string[] { ".png" }, true);
                TestUtil.AssertEqual(Path.Combine(dir, "aa.png"), nav.Current);
                nav.GoLast();
                TestUtil.AssertEqual(Path.Combine(dir, "dd.png"), nav.Current);
                nav.GoNextOrPrev(false, neighbors, neighbors.Count);
                TestUtil.AssertEqual(Path.Combine(dir, "cc.png"), nav.Current);
                TestUtil.AssertStringArrayEqual("%bb.png|%aa.png|%aa.png|%aa.png".Replace("%", dir + "\\"), neighbors);
                nav.GoNextOrPrev(false, neighbors, neighbors.Count);
                TestUtil.AssertEqual(Path.Combine(dir, "bb.png"), nav.Current);
                TestUtil.AssertStringArrayEqual("%aa.png|%aa.png|%aa.png|%aa.png".Replace("%", dir + "\\"), neighbors);
                nav.GoNextOrPrev(false, neighbors, neighbors.Count);
                TestUtil.AssertEqual(Path.Combine(dir, "aa.png"), nav.Current);
                TestUtil.AssertStringArrayEqual("%aa.png|%aa.png|%aa.png|%aa.png".Replace("%", dir + "\\"), neighbors);
                nav.GoNextOrPrev(false, neighbors, neighbors.Count);
                TestUtil.AssertEqual(Path.Combine(dir, "aa.png"), nav.Current);
                TestUtil.AssertStringArrayEqual("%aa.png|%aa.png|%aa.png|%aa.png".Replace("%", dir + "\\"), neighbors);
            }

            { // gonext when file is missing
                var nav = new FileListNavigation(dir, new string[] { ".png" }, true);
                nav.GoLast();
                nav.TrySetPath(Path.Combine(dir, "().png"), false);
                nav.GoNextOrPrev(true, neighbors, neighbors.Count);
                TestUtil.AssertEqual(Path.Combine(dir, "aa.png"), nav.Current);
                TestUtil.AssertStringArrayEqual("%bb.png|%cc.png|%dd.png|%dd.png".Replace("%", dir + "\\"), neighbors);

                nav.GoLast();
                nav.TrySetPath(Path.Combine(dir, "ab.png"), false);
                nav.GoNextOrPrev(true);
                TestUtil.AssertEqual(Path.Combine(dir, "bb.png"), nav.Current);

                nav.GoLast();
                nav.TrySetPath(Path.Combine(dir, "bc.png"), false);
                nav.GoNextOrPrev(true);
                TestUtil.AssertEqual(Path.Combine(dir, "cc.png"), nav.Current);

                nav.GoFirst();
                nav.TrySetPath(Path.Combine(dir, "zz.png"), false);
                nav.GoNextOrPrev(true);
                TestUtil.AssertEqual(Path.Combine(dir, "dd.png"), nav.Current);
            }

            { // goprev when file is missing
                var nav = new FileListNavigation(dir, new string[] { ".png" }, true);
                nav.GoLast();
                nav.TrySetPath(Path.Combine(dir, "().png"), false);
                nav.GoNextOrPrev(false);
                TestUtil.AssertEqual(Path.Combine(dir, "aa.png"), nav.Current);

                nav.GoLast();
                nav.TrySetPath(Path.Combine(dir, "bc.png"), false);
                nav.GoNextOrPrev(false);
                TestUtil.AssertEqual(Path.Combine(dir, "bb.png"), nav.Current);

                nav.GoLast();
                nav.TrySetPath(Path.Combine(dir, "cd.png"), false);
                nav.GoNextOrPrev(false);
                TestUtil.AssertEqual(Path.Combine(dir, "cc.png"), nav.Current);

                nav.GoFirst();
                nav.TrySetPath(Path.Combine(dir, "zz.png"), false);
                nav.GoNextOrPrev(false, neighbors, neighbors.Count);
                TestUtil.AssertEqual(Path.Combine(dir, "dd.png"), nav.Current);
                TestUtil.AssertStringArrayEqual("%cc.png|%bb.png|%aa.png|%aa.png".Replace("%", dir + "\\"), neighbors);
            }

            { //gonext and goprev after deleted file
                var nav = new FileListNavigation(dir, new string[] { ".png" }, true);
                nav.GoFirst();
                File.Delete(Path.Combine(dir, "bb.png"));
                nav.GoNextOrPrev(true);
                TestUtil.AssertEqual(Path.Combine(dir, "cc.png"), nav.Current);
                nav.GoNextOrPrev(true);
                TestUtil.AssertEqual(Path.Combine(dir, "dd.png"), nav.Current);
                File.Delete(Path.Combine(dir, "cc.png"));
                nav.GoNextOrPrev(false);
                TestUtil.AssertEqual(Path.Combine(dir, "aa.png"), nav.Current);

                // go down to 1 file
                File.Delete(Path.Combine(dir, "dd.png"));
                nav.GoLast();
                TestUtil.AssertEqual(Path.Combine(dir, "aa.png"), nav.Current);
                nav.GoNextOrPrev(true);
                TestUtil.AssertEqual(Path.Combine(dir, "aa.png"), nav.Current);
                nav.GoNextOrPrev(false);
                TestUtil.AssertEqual(Path.Combine(dir, "aa.png"), nav.Current);

                // go down to no files
                File.Delete(Path.Combine(dir, "aa.png"));
                nav.GoNextOrPrev(true);
                TestUtil.AssertEqual(null, nav.Current);
                nav.GoNextOrPrev(false);
                TestUtil.AssertEqual(null, nav.Current);
                nav.GoFirst();
                TestUtil.AssertEqual(null, nav.Current);
                nav.GoLast();
                TestUtil.AssertEqual(null, nav.Current);

                // recover from no files
                File.WriteAllText(Path.Combine(dir, "new.png"), "content");
                nav.NotifyFileChanges();
                nav.GoNextOrPrev(true);
                TestUtil.AssertEqual(Path.Combine(dir, "new.png"), nav.Current);
                nav.GoNextOrPrev(false);
                TestUtil.AssertEqual(Path.Combine(dir, "new.png"), nav.Current);
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
