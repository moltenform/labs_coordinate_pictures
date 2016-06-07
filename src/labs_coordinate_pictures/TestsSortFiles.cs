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

    public static class CreateFileCombinations
    {
        enum ModifiedTimes { Same, RightOlder, RightNewer }
        enum Content { Same, RightAltered, RightAppended }
        enum Filename { Same, RightAppended }
        enum ExtraCopies { None, OneOnLeft, TwoOnLeft, OneOnRight, TwoOnRight }

        public static void Go(string dirLeft, string dirRight)
        {
            var baseTime = DateTime.Now;
            foreach (ModifiedTimes m in Enum.GetValues(typeof(ModifiedTimes)))
            {
                foreach (Content c in Enum.GetValues(typeof(Content)))
                {
                    foreach (Filename f in Enum.GetValues(typeof(Filename)))
                    {
                        foreach (ExtraCopies copies in Enum.GetValues(typeof(ExtraCopies)))
                        {
                            Go(dirLeft, dirRight, baseTime, m, c, f, copies);
                        }
                    }
                }
            }
        }

        static void Go(string dirLeft, string dirRight, DateTime baseTime,
            ModifiedTimes m, Content c, Filename f, ExtraCopies copies)
        {
            var baseName = m.ToString() + c.ToString() + f.ToString() + copies.ToString();
            var fileLeft = Path.Combine(dirLeft, baseName + ".a");
            var fileRight = Path.Combine(dirRight, baseName + (f == Filename.RightAppended ? ".aa" : ".a"));
            WriteFiles(fileLeft, fileRight, baseTime, m, c);
            switch (copies)
            {
                case ExtraCopies.OneOnLeft:
                    File.Copy(fileLeft, fileLeft + "_1");
                    break;
                case ExtraCopies.TwoOnLeft:
                    File.Copy(fileLeft, fileLeft + "_1");
                    File.Copy(fileLeft, fileLeft + "_2");
                    break;
                case ExtraCopies.OneOnRight:
                    File.Copy(fileRight, fileRight + "_1");
                    break;
                case ExtraCopies.TwoOnRight:
                    File.Copy(fileRight, fileRight + "_1");
                    File.Copy(fileRight, fileRight + "_2");
                    break;
            }
        }

        static void WriteFiles(string fileLeft, string fileRight, DateTime baseTime, ModifiedTimes m, Content c)
        {
            var addTime = (m == ModifiedTimes.RightNewer) ? 1 :
                (m == ModifiedTimes.RightOlder) ? -1 : 0;
            var contentsLeft = fileLeft;
            var contentsRight = contentsLeft;
            if (c == Content.RightAltered)
            {
                // replace first char with *
                contentsRight = "*" + contentsRight.Substring(1);
            }
            else if (c == Content.RightAppended)
            {
                contentsRight += "***";
            }

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

        static void TestMethod_HighLevel()
        {
            var dirLeft = TestUtil.GetTestSubDirectory("first_fndmved");
            var dirRight = TestUtil.GetTestSubDirectory("second_fndmved");
            CreateFileCombinations.Go(dirLeft, dirRight);
            var dirLeft5 = TestUtil.GetTestSubDirectory("first_fndmved");
        }
    }
}
