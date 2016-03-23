using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!Debugger.IsAttached)
            {
                // by default fatal exceptions like AV won't be caught, seems fine to me.
                Application.ThreadException += Application_ThreadException;
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(Application_UIException);
            }

            // find best directory for logging and configs.
            string dir;
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "silence.flac")))
                dir = AppDomain.CurrentDomain.BaseDirectory;
            else if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\tools\silence.flac")))
                dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\tools");
            else
                throw new CoordinatePicturesException("cannot find silence.flac");

            // initialize logging and configs
            SimpleLog.Init(Path.Combine(dir, "log.txt"));
            SimpleLog.Current.WriteLog("Initializing.");
            Configs.Init(Path.Combine(dir, "options.ini"));
            Configs.Current.LoadPersisted();
            Configs.Current.Set(ConfigKey.Version, "0.1");
            Application.Run(new FormStart());
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            OnUnhandledException(e.Exception.Message, e.Exception.StackTrace);
        }

        private static void Application_UIException(object sender, UnhandledExceptionEventArgs e)
        {
            // The app will still exit, which seems fine. Can be overridden in app.config.
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
                OnUnhandledException(exception.Message, exception.StackTrace);
            else
                OnUnhandledException("Unknown exception", "");
        }

        private static void OnUnhandledException(string s, string trace)
        {
            if (Debugger.IsAttached)
                Debugger.Break();

            try
            {
                if (s == null)
                    s = "";
                if (trace == null)
                    trace = "";

                SimpleLog.Current.WriteError("Unhandled Exception: " + s + "\r\n" + trace);
                if (!Utils.AskToConfirm("An exception occurred: " + s + " \r\n Continue?"))
                    Environment.Exit(1);
            }
            catch
            {
                // swallow exceptions to avoid infinite recursion.
            }
        }
    }
}
