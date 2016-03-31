using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
            var sortedMethods = methodInfos.OrderBy(item => item.Name);
            foreach (MethodInfo methodInfo in sortedMethods)
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

        static void TestMethod_UtilsSameExceptExtension()
        {
            TestUtil.AssertTrue(FilenameUtils.SameExceptExtension("a\\test6.jpg", "a\\test6.jpg"));
            TestUtil.AssertTrue(FilenameUtils.SameExceptExtension("a\\test6.jpg", "a\\test6.png"));
            TestUtil.AssertTrue(FilenameUtils.SameExceptExtension("a\\test6.jpg", "a\\test6.BMP"));
            TestUtil.AssertTrue(!FilenameUtils.SameExceptExtension("a\\test6.jpg", "b\\test6.jpg"));
            TestUtil.AssertTrue(!FilenameUtils.SameExceptExtension("a\\test6.jpg", "a\\test6.jpg.jpg"));
            TestUtil.AssertTrue(!FilenameUtils.SameExceptExtension("a\\test6a.jpg", "a\\test6.jpg"));
            TestUtil.AssertTrue(FilenameUtils.SameExceptExtension("b\\aa.jpg.test6.jpg", "b\\aa.jpg.test6.bmp"));
            TestUtil.AssertTrue(!FilenameUtils.SameExceptExtension("b\\aa.jpg.test6.jpg", "b\\aa.bmp.test6.jpg"));
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
            string path = Path.Combine(TestUtil.GetTestSubDirectory("testcfg"), "test.ini");
            Configs cfg = new Configs(path);
            cfg.LoadPersisted();
            TestUtil.AssertEqual("", cfg.Get(ConfigKey.EnablePersonalFeatures));
            TestUtil.AssertEqual("", cfg.Get(ConfigKey.FilepathPython));
            TestUtil.AssertEqual("", cfg.Get(ConfigKey.FilepathTrash));
            cfg.Set(ConfigKey.EnablePersonalFeatures, "data=with=equals=");
            cfg.Set(ConfigKey.FilepathPython, " data\twith\t tabs");
            
            /* from memory */
            TestUtil.AssertEqual("data=with=equals=", cfg.Get(ConfigKey.EnablePersonalFeatures));
            TestUtil.AssertEqual(" data\twith\t tabs", cfg.Get(ConfigKey.FilepathPython));
            TestUtil.AssertEqual("", cfg.Get(ConfigKey.FilepathTrash));
            
            /* from disk */
            cfg = new Configs(path);
            cfg.LoadPersisted();
            TestUtil.AssertEqual("data=with=equals=", cfg.Get(ConfigKey.EnablePersonalFeatures));
            TestUtil.AssertEqual(" data\twith\t tabs", cfg.Get(ConfigKey.FilepathPython));
            TestUtil.AssertEqual("", cfg.Get(ConfigKey.FilepathTrash));
        }

        static void TestMethod_ClassConfigsPersistedBools()
        {
            string path = Path.Combine(TestUtil.GetTestSubDirectory("testcfg"), "testbools.ini");
            Configs cfg = new Configs(path);
            TestUtil.AssertEqual(false, cfg.GetBool(ConfigKey.EnablePersonalFeatures));
            cfg.SetBool(ConfigKey.EnablePersonalFeatures, true);
            TestUtil.AssertEqual(true, cfg.GetBool(ConfigKey.EnablePersonalFeatures));
            cfg.SetBool(ConfigKey.EnablePersonalFeatures, false);
            TestUtil.AssertEqual(false, cfg.GetBool(ConfigKey.EnablePersonalFeatures));
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
                    TestUtil.AssertEqual(true, Enum.TryParse<ConfigKey>(s, out key));
                }
            }
        }

        static void TestMethod_FileListNavigation()
        {
            /* setup */
            var dir = TestUtil.GetTestSubDirectory("filelist");
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

            { // gonext and goprev after deleted file
                var nav = new FileListNavigation(dir, new string[] { ".png" }, true);
                nav.GoFirst();
                TestUtil.AssertEqual(Path.Combine(dir, "aa.png"), nav.Current);
                File.Delete(Path.Combine(dir, "bb.png"));
                nav.NotifyFileChanges();
                nav.GoNextOrPrev(true);
                TestUtil.AssertEqual(Path.Combine(dir, "cc.png"), nav.Current);
                nav.GoNextOrPrev(true);
                TestUtil.AssertEqual(Path.Combine(dir, "dd.png"), nav.Current);
                File.Delete(Path.Combine(dir, "cc.png"));
                nav.NotifyFileChanges();
                nav.GoNextOrPrev(false);
                TestUtil.AssertEqual(Path.Combine(dir, "aa.png"), nav.Current);

                // go down to 1 file
                File.Delete(Path.Combine(dir, "dd.png"));
                nav.NotifyFileChanges();
                nav.GoLast();
                TestUtil.AssertEqual(Path.Combine(dir, "aa.png"), nav.Current);
                nav.GoNextOrPrev(true);
                TestUtil.AssertEqual(Path.Combine(dir, "aa.png"), nav.Current);
                nav.GoNextOrPrev(false);
                TestUtil.AssertEqual(Path.Combine(dir, "aa.png"), nav.Current);

                // go down to no files
                File.Delete(Path.Combine(dir, "aa.png"));
                nav.NotifyFileChanges();
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

        static void TestMethod_ImageCache()
        {
            var dir = TestUtil.GetTestSubDirectory("imcache");
            File.WriteAllText(Path.Combine(dir, "a1.png"), "fake image1");
            File.WriteAllText(Path.Combine(dir, "b1.png"), "fake image2");
            List<object> removedFromCache = new List<object>();
            Func<Bitmap, bool> canDisposeBitmap =
                (bmp) => { removedFromCache.Add(bmp); return true; };
            Func<Action, bool> callbackOnUiThread =
                (act) => { act(); return true; };
            

            { // standard lookup
                var imcache = new ImageCache(20, 20, 3 /*cache size*/,
                    callbackOnUiThread, canDisposeBitmap);

                // retrieve from the cache
                int gotW = 0, gotH = 0;
                var bmp1 = imcache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                TestUtil.AssertEqual(1, gotW);
                TestUtil.AssertEqual(1, gotH);

                // retrieving same path from the cache should return the exact same image
                var bmp1Same = imcache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                TestUtil.AssertEqual(1, gotW);
                TestUtil.AssertEqual(1, gotH);
                TestUtil.AssertEqual((object) bmp1, (object)bmp1Same);

                // however, if lmt has changed, cached copy should be refreshed.
                var wasTime = File.GetLastWriteTime(Path.Combine(dir, "a1.png"));
                File.SetLastWriteTime(Path.Combine(dir, "a1.png"), wasTime - new TimeSpan(0, 0, 10));
                var bmp1Changed = imcache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                TestUtil.AssertEqual(1, gotW);
                TestUtil.AssertEqual(1, gotH);
                TestUtil.AssertTrue((object)bmp1 != (object)bmp1Changed);

                // and further lookups should get this new copy.
                var bmp1ChangedAfter = imcache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                TestUtil.AssertEqual((object)bmp1ChangedAfter, (object)bmp1Changed);

                // do comparisons work after dispose.
                bmp1.Dispose();
                bmp1Same.Dispose();
                bmp1Changed.Dispose();
                TestUtil.AssertTrue((object)bmp1 != (object)bmp1Changed);
                TestUtil.AssertTrue((object)bmp1Same != (object)bmp1Changed);
                TestUtil.AssertEqual((object)bmp1, (object)bmp1Same);
                TestUtil.AssertEqual((object)bmp1Same, (object)bmp1Same);
                TestUtil.AssertEqual((object)bmp1Same, (object)bmp1);
            }

            { // add past the limit
                var imcache = new ImageCache(20, 20, 3 /*cache size*/,
                    callbackOnUiThread, canDisposeBitmap);

                // fill up cache
                removedFromCache.Clear();
                int gotW = 0, gotH = 0;
                var bmp1 = imcache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                var bmp2 = imcache.Get(Path.Combine(dir, "a2.png"), out gotW, out gotH);
                var bmp3 = imcache.Get(Path.Combine(dir, "a3.png"), out gotW, out gotH);
                TestUtil.AssertEqual(0, removedFromCache.Count);

                // add one more
                var bmp4 = imcache.Get(Path.Combine(dir, "a4.png"), out gotW, out gotH);
                TestUtil.AssertEqual(1, removedFromCache.Count);
                TestUtil.AssertEqual((object)bmp1, (object)removedFromCache[0]);

                // bmp4 should now be in the cache
                var bmp4Again = imcache.Get(Path.Combine(dir, "a4.png"), out gotW, out gotH);
                TestUtil.AssertEqual((object)bmp4Again, (object)bmp4);

                // add one more
                var bmp5 = imcache.Get(Path.Combine(dir, "a5.png"), out gotW, out gotH);
                TestUtil.AssertEqual(2, removedFromCache.Count);
                TestUtil.AssertEqual((object)bmp2, (object)removedFromCache[1]);

                // add many
                removedFromCache.Clear();
                imcache.Add(new string[] { Path.Combine(dir, "b1.png"), Path.Combine(dir, "b2.png"), Path.Combine(dir, "b3.png"), Path.Combine(dir, "b4.png"), Path.Combine(dir, "b5.png") });
                TestUtil.AssertEqual(5, removedFromCache.Count);
                TestUtil.AssertEqual((object)bmp3, (object)removedFromCache[4]);
                TestUtil.AssertEqual((object)bmp4, (object)removedFromCache[3]);
                TestUtil.AssertEqual((object)bmp5, (object)removedFromCache[2]);
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
