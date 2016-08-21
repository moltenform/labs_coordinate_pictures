// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

// Getting libwebp.dll:
// Get libwebp-0.4.3.tar.gz from http://downloads.webmproject.org/releases/webp/index.html
// Open VS x64 native tools cmd prompt
// nmake /f Makefile.vc CFG=release-dynamic
// https://developers.google.com/speed/webp/faq?hl=en

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
                AppDomain.CurrentDomain.UnhandledException +=
                    new UnhandledExceptionEventHandler(Application_UIException);
            }

            // find directory for logging and configs.
            // looks in parent directories in case we are running from visual studio.
            // todo: use appdata instead of current directory.
            string configDirectory = null;
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "silence.flac")))
            {
                configDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }
            else if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "../../../../tools/silence.flac".Replace("/", Utils.Sep))))
            {
                configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "../../../../tools".Replace("/", Utils.Sep));
            }
            else if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "../../../../../tools/silence.flac".Replace("/", Utils.Sep))))
            {
                configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "../../../../../tools".Replace("/", Utils.Sep));
            }

            if (string.IsNullOrEmpty(configDirectory) || !Directory.Exists(configDirectory))
            {
                MessageBox.Show("We could not find the file silence.flac. Please run this " +
                    "program from the same directory as silence.flac. We will now exit.");
                Environment.Exit(1);
            }

            // initialize logging and configs
            SimpleLog.Init(Path.Combine(configDirectory, "log.txt"));
            SimpleLog.Current.WriteLog("Initializing.");
            Configs.Init(Path.Combine(configDirectory, "options.ini"));
            Configs.Current.LoadPersisted();
            Configs.Current.Set(ConfigKey.Version, "0.1");
            Application.Run(new FormStart());
        }

        private static void Application_ThreadException(object sender,
            System.Threading.ThreadExceptionEventArgs e)
        {
            OnUnhandledException(e.Exception.Message, e.Exception.StackTrace);
        }

        private static void Application_UIException(object sender,
            UnhandledExceptionEventArgs e)
        {
            // The app will still exit, which seems fine. Can be overridden in app.config.
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                OnUnhandledException(exception.Message, exception.StackTrace);
            }
            else
            {
                OnUnhandledException("Unknown exception", "");
            }
        }

        private static void OnUnhandledException(string message, string trace)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            try
            {
                if (message == null)
                {
                    message = "";
                }

                if (trace == null)
                {
                    trace = "";
                }

                SimpleLog.Current.WriteError("Unhandled Exception: " + message + Utils.NL + trace);
                if (!Utils.AskToConfirm("An exception occurred: "
                    + message + Utils.NL + " Continue?"))
                {
                    Environment.Exit(1);
                }
            }
            catch
            {
                // swallow exceptions to avoid infinite recursion.
            }
        }
    }
}
