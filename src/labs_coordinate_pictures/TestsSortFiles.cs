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

    public static class CoordinateFilesTests
    {
        static void TestMethod_ValidateGoodFilesSettings()
        {
            var dirFirst = TestUtil.GetTestSubDirectory("first");
            var dirSecond = TestUtil.GetTestSubDirectory("second");
            var settings = FormSortFiles.FillFromUI(SortFilesAction.SearchDupes, "", "",
                dirFirst, dirSecond, true, true, false);

            TestUtil.IsEq(true, settings.AllowFiletimesDifferForFAT);
            TestUtil.IsEq(true, settings.AllowFiletimesDifferForDST);
            TestUtil.IsEq(dirSecond, settings.DestDirectory);
            TestUtil.IsTrue(Directory.Exists(Path.GetDirectoryName(settings.LogFile)));
            TestUtil.IsEq(false, settings.Mirror);
            TestUtil.IsStringArrayEq(null, settings.GetSkipDirectories());
            TestUtil.IsStringArrayEq(null, settings.GetSkipFiles());
            TestUtil.IsEq(dirFirst, settings.SourceDirectory);

            settings = FormSortFiles.FillFromUI(SortFilesAction.SearchDupes, "a", "a\nb b\n\nc\n\n ",
                dirSecond, dirFirst, false, false, true);

            TestUtil.IsEq(false, settings.AllowFiletimesDifferForFAT);
            TestUtil.IsEq(false, settings.AllowFiletimesDifferForDST);
            TestUtil.IsEq(dirFirst, settings.DestDirectory);
            TestUtil.IsTrue(Directory.Exists(Path.GetDirectoryName(settings.LogFile)));
            TestUtil.IsEq(true, settings.Mirror);
            TestUtil.IsStringArrayEq("a", settings.GetSkipDirectories());
            TestUtil.IsStringArrayEq("a|b b|c", settings.GetSkipFiles());
            TestUtil.IsEq(dirSecond, settings.SourceDirectory);
        }

        static void TestMethod_RejectBadFilesSettings()
        {
            var dirFirst = TestUtil.GetTestSubDirectory("first");
            var dirSecond = TestUtil.GetTestSubDirectory("second");

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDupes, "", "",
                Path.Combine(dirFirst, "notexist"), dirSecond, true, true, true));

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDupes, "", "",
                dirFirst, Path.Combine(dirSecond, "notexist"), true, true, true));

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDupes, "", "",
                dirFirst + Path.DirectorySeparatorChar, dirSecond, true, true, true));

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDupes, "", "",
                dirFirst, dirSecond + Path.DirectorySeparatorChar, true, true, true));

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDupes, "", "",
                dirFirst, dirFirst, true, true, true));

            Directory.CreateDirectory(Path.Combine(dirFirst, "sub"));
            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDupes, "", "",
                dirFirst, Path.Combine(dirFirst, "sub"), true, true, true));

           // valid for dest to be empty if action is FindDupeFilesInOneDir
            TestUtil.IsTrue(FormSortFiles.FillFromUI(SortFilesAction.SearchDupesInOneDir, "", "",
                dirFirst, "", true, false, true) != null);
        }

        static void WriteTextAndLastWriteTime(string dir, string path, string contents, DateTime basetime, int lastwritetime)
        {
            File.WriteAllText(Path.Combine(dir, path), contents);
            var time = basetime.AddMinutes(lastwritetime);
            File.SetLastWriteTimeUtc(Path.Combine(dir, path), time);
        }

        static void TestMethod_HighLevelFindMovedFiles()
        {
            var dirFirst = TestUtil.GetTestSubDirectory("first_fndmved");
            var dirSecond = TestUtil.GetTestSubDirectory("second_fndmved");
            var baseTime = DateTime.Now;

            // set up contents
            WriteTextAndLastWriteTime(dirFirst, "a_same.txt", "0000", baseTime, 0);
            WriteTextAndLastWriteTime(dirFirst, "a_diffwritetimediffname1.txt", "0001", baseTime, 100);
            WriteTextAndLastWriteTime(dirFirst, "a_diffwritetime.txt", "0002", baseTime, 1);
            WriteTextAndLastWriteTime(dirFirst, "a_diffwritetimediffcontents.txt", "0003LL", baseTime, 1);
            WriteTextAndLastWriteTime(dirFirst, "a_diffsizediffcontents.txt", "0004LL", baseTime, 1);
            WriteTextAndLastWriteTime(dirFirst, "a_diffcontents.txt", "0005LL", baseTime, 8);
            WriteTextAndLastWriteTime(dirFirst, "a_diffcontentsdiffname_1.txt", "0005LLL", baseTime, 9);
            WriteTextAndLastWriteTime(dirFirst, "a_leftonly.txt", "0006LL", baseTime, 6);
            WriteTextAndLastWriteTime(dirFirst, "a_moved_once1.txt", "0007", baseTime, 1);
            WriteTextAndLastWriteTime(dirFirst, "a_moved_twice_a_1.txt", "0008", baseTime, 20);
            WriteTextAndLastWriteTime(dirFirst, "a_moved_twice_b_1.txt", "0008", baseTime, 20);
            WriteTextAndLastWriteTime(dirFirst, "a_moved_and_copied_1.txt", "0009", baseTime, 1);
            WriteTextAndLastWriteTime(dirSecond, "a_same.txt", "0000", baseTime, 0);
            WriteTextAndLastWriteTime(dirSecond, "a_diffwritetimediffname2.txt", "0001", baseTime, 101);
            WriteTextAndLastWriteTime(dirSecond, "a_diffwritetime.txt", "0002", baseTime, 2);
            WriteTextAndLastWriteTime(dirSecond, "a_diffwritetimediffcontents.txt", "0003RR", baseTime, 2);
            WriteTextAndLastWriteTime(dirSecond, "a_diffsizediffcontents.txt", "0004RR::::", baseTime, 1);
            WriteTextAndLastWriteTime(dirSecond, "a_diffcontents.txt", "0005RR", baseTime, 8);
            WriteTextAndLastWriteTime(dirSecond, "a_diffcontentsdiffname_2.txt", "0005RRR", baseTime, 9);
            WriteTextAndLastWriteTime(dirSecond, "a_rightonly.txt", "0006RR", baseTime, 7);
            WriteTextAndLastWriteTime(dirSecond, "a_moved_once2.txt", "0007", baseTime, 1);
            WriteTextAndLastWriteTime(dirSecond, "a_moved_twice_a_2.txt", "0008", baseTime, 20);
            WriteTextAndLastWriteTime(dirSecond, "a_moved_twice_b_2.txt", "0008", baseTime, 20);
            WriteTextAndLastWriteTime(dirSecond, "a_moved_and_copied_2.txt", "0009", baseTime, 1);
            WriteTextAndLastWriteTime(dirSecond, "a_moved_and_copied_3.txt", "0009", baseTime, 1);
        }
    }
}
