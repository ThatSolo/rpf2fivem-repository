using System;
using System.Windows.Forms;
using Sentry;

namespace rpf2fivem
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            SentrySdk.Init(o =>
            {
                o.Dsn = "https://d40cb8d380bf40a0a1b8c99f0d214978@o1113761.ingest.sentry.io/4504494068334592";
                o.Debug = true;
                o.TracesSampleRate = 1.0;
                o.IsGlobalModeEnabled = true;
                o.Release = Properties.Resources.sentry_version;
                o.AutoSessionTracking = true;
                o.Environment = Properties.Resources.sentry_enviroment;
                o.StackTraceMode = Sentry.StackTraceMode.Enhanced;
            });

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);

            Application.Run(new Main());
        }
    }
}
