using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace labs_coordinate_pictures
{
    public static class OsHelpers
    {
        // By Roger Knapp, http://csharptest.net/529/how-to-correctly-escape-command-line-arguments-in-c/
        public static string CombineProcessArguments(string[] args)
        {
            StringBuilder arguments = new StringBuilder();
            Regex invalidChar = new Regex("[\x00\x0a\x0d]");//  these can not be escaped
            Regex needsQuotes = new Regex(@"\s|""");//          contains whitespace or two quote characters
            Regex escapeQuote = new Regex(@"(\\*)(""|$)");//    one or more '\' followed with a quote or end of string
            for (int carg = 0; carg < args.Length; carg++)
            {
                if (invalidChar.IsMatch(args[carg]))
                {
                    throw new ArgumentOutOfRangeException("args[" + carg + "]");
                }
                if (args[carg] == String.Empty)
                {
                    arguments.Append("\"\"");
                }
                else if (!needsQuotes.IsMatch(args[carg]))
                {
                    arguments.Append(args[carg]);
                }
                else
                {
                    arguments.Append('"');
                    arguments.Append(escapeQuote.Replace(args[carg], m =>
                        m.Groups[1].Value + m.Groups[1].Value +
                        (m.Groups[2].Value == "\"" ? "\\\"" : "")
                        ));
                    arguments.Append('"');
                }
                if (carg + 1 < args.Length)
                {
                    arguments.Append(' ');
                }
            }
            return arguments.ToString();
        }

    }

    public class SimpleLog
    {
        private static readonly SimpleLog instance = new SimpleLog("./log.txt");
        private SimpleLog(string path) { _path = path; }
        string _path;

        public static SimpleLog Current
        {
            get
            {
                return instance;
            }
        }
        public void WriteLog(string s)
        {
            File.AppendAllText(_path, s);
        }
        public void WriteWarning(string s)
        {
            File.AppendAllText(_path, "[warning] " + s);
        }
        public void WriteError(string s)
        {
            File.AppendAllText(_path, "[error] " + s);
        }
    }

    public class CoordinatePicturesException : ApplicationException
    {
        public CoordinatePicturesException(string message) : base(message) { }
    }
}
