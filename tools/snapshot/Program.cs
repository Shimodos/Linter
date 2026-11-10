using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Linter.Configuration;
using Linter.Localization;
using Linter.Services;
using Linter.Shell;

namespace Linter.Snapshot;

internal static class Program
{
    private static readonly string[] TabNames = { "general", "hotkeys", "spelling", "exceptions" };

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("usage: Linter.Snapshot <outDir>");
            return 1;
        }
        var outDir = Directory.CreateDirectory(args[0]).FullName;

        Application.EnableVisualStyles();
        Application.SetHighDpiMode(HighDpiMode.DpiUnawareGdiScaled);

        // A realistic, populated state — never the user's own settings file.
        var settings = new AppSettings { SoundFile = "tick.wav" };
        settings.IgnoredWords.AddRange(new[] { "GitHub", "xD", "лол" });
        settings.ExcludedProcesses.AddRange(new[] { "mstsc", "WindowsTerminal" });

        using var spell = new SpellChecker(settings.Languages);
        var host = new SettingsFormHost(
            HasDictionary: spell.IsLanguageSupported,
            InstalledLanguages: spell.InstalledLanguages,
            BeginHotkeyCapture: _ => { },
            CancelHotkeyCapture: () => { },
            Saved: () => { });

        foreach (var language in Localizer.Languages)
        {
            Localizer.Use(language);
            using var form = new SettingsForm(settings, host)
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-4000, -4000),
                ShowInTaskbar = false,
            };
            form.Show();
            Application.DoEvents();

            var tabs = FindTabs(form) ?? throw new InvalidOperationException("TabControl not found");
            for (var i = 0; i < tabs.TabCount; i++)
            {
                tabs.SelectedIndex = i;
                Application.DoEvents();
                using var bitmap = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                var file = Path.Combine(outDir, $"settings-{TabNames[i]}-{language}.png");
                bitmap.Save(file, ImageFormat.Png);
                Console.WriteLine($"wrote {Path.GetFileName(file)} {bitmap.Width}x{bitmap.Height}");
            }
            form.Close();
        }
        return 0;
    }

    private static TabControl? FindTabs(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is TabControl tabs) return tabs;
            if (FindTabs(child) is { } nested) return nested;
        }
        return null;
    }
}
