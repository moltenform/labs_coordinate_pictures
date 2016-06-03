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
    public class CoordinatePicturesTestException : ApplicationException
    {
        public CoordinatePicturesTestException(string message)
            : base(message)
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
                throw new Exception("Assertion failure, expected " + expected + " but got " + actual);
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
                if (methodInfo.Name.StartsWith("TestMethod_"))
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
        
    }
}
