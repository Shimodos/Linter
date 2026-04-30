using System;
using System.Windows.Forms;
using Linter.Shell;

namespace Linter;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => CrashReporter.Report(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => CrashReporter.Report(e.ExceptionObject as Exception);

        using var context = new LinterApplicationContext();
        Application.Run(context);
    }
}
