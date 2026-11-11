using System;
using System.Threading;
using System.Windows.Forms;
using Linter.Diagnostics;
using Linter.Shell;

namespace Linter;

internal static class Program
{
    private const string SingletonName = "Local\\Linter.Singleton.A1B2";

    [STAThread]
    private static void Main()
    {
        // Two simultaneous Linters would each install their own LL hook
        // and double every keystroke. The mutex makes the second launch a
        // no-op instead of a silent-but-visible mess.
        using var singleton = new Mutex(initiallyOwned: true, SingletonName, out var createdNew);
        if (!createdNew) return;

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => CrashReporter.Report(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => CrashReporter.Report(e.ExceptionObject as Exception);
        Lifecycle.Begin();

        using var context = new LinterApplicationContext();
        Application.Run(context);
        // Reached on a tray "Exit" and on anything else that ends the message
        // loop. A run with no line like this after its START was killed.
        Lifecycle.Record("message loop ended");
    }
}
