using System.Text.RegularExpressions;

namespace PicView.Tests.Config;

public partial class WindowSettingRecordReadsTests
{
    [GeneratedRegex(@"\bWindowProperties\b")]
    private static partial Regex WindowPropertiesWord();

    // The two properties that stay app-wide and are still read from the record.
    [GeneratedRegex(@"\bWindowProperties[!?]?\.(Margin|EscLeavesFullscreen)\b")]
    private static partial Regex AppWideRead();

    private static readonly string[] SkippedSegments = ["bin", "obj", "PicView.Tests", "PicView.Benchmarks"];
    // Exempt because they read WindowSettings itself or a tool window's own remembered placement (a different
    // WindowProperties); a new entry needs the same reason. Paths are relative to src.
    private static readonly string[] AllowedFiles =
    [
        @"PicView.Core/ViewModels/WindowSettings.cs",
        @"PicView.Core/Config/AppSettings.cs",
        @"PicView.Core/Config/SettingsManager.cs",
        @"PicView.Core/Config/RememberedWindowConfig.cs",
        @"PicView.Avalonia/Services/WindowInitializer.cs",
        @"PicView.Avalonia/Printing/PrintJobStarter.cs",
        @"PicView.Avalonia.Win32/Views/SettingsWindow.axaml.cs",
        @"PicView.Avalonia.Linux/Views/SettingsWindow.axaml.cs",
        @"PicView.Avalonia.MacOS/Views/SettingsWindow.axaml.cs"
    ];

    private static bool Offends(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        var stripped = AppWideRead().Replace(line, string.Empty);
        return WindowPropertiesWord().IsMatch(stripped);
    }

    [Theory]
    [InlineData("var properties = Settings.WindowProperties;", true)]
    [InlineData("_ = Settings.WindowProperties!.AutoFit;", true)]
    [InlineData("var m = Settings.WindowProperties.Margin;", false)]
    [InlineData("if (Settings.WindowProperties?.EscLeavesFullscreen == true)", false)]
    [InlineData("config.WindowProperties!.LastTab = 1;", true)]
    [InlineData("appConfig.WindowProperties!.TopMost = true;", true)]
    [InlineData("IWindowProperties x;", false)]
    [InlineData("RememberedWindowProperties x;", false)]
    [InlineData("// Settings.WindowProperties.AutoFit", false)]
    [InlineData("/// Settings.WindowProperties.AutoFit", false)]
    public void Offends_Line_MatchesTheRule(string line, bool expected)
    {
        Assert.Equal(expected, Offends(line));
    }

    [Fact]
    public void ProductionCode_ReadsWindowPropertiesOnlyThroughWindowSettings()
    {
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(SourceTree.Src, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(SourceTree.Src, file);
            var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (segments.Any(s => SkippedSegments.Contains(s, StringComparer.Ordinal)) ||
                AllowedFiles.Contains(string.Join('/', segments), StringComparer.Ordinal))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (Offends(lines[i]))
                {
                    offenders.Add($"{relative}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "Window Settings are read from the window, not the record:" + Environment.NewLine +
            string.Join(Environment.NewLine, offenders));
    }
}
