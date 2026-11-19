using System.Text.RegularExpressions;

namespace PicView.Tests.Config;

public partial class WindowSettingRecordReadsTests
{
    [GeneratedRegex(@"\bWindowProperties\b")]
    private static partial Regex WindowPropertiesWord();

    // The two properties that stay app-wide and are still read from the record.
    [GeneratedRegex(@"\bWindowProperties[!?]?\.(Margin|EscLeavesFullscreen)\b")]
    private static partial Regex AppWideRead();

    // A mention of the record's Sorting group; the one app-wide member (IncludeSubDirectories) is stripped first.
    [GeneratedRegex(@"\.Sorting\b")]
    private static partial Regex SortingGroup();

    [GeneratedRegex(@"[!?]?\.Sorting[!?]?\.IncludeSubDirectories\b")]
    private static partial Regex IncludeSubDirectoriesRead();

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

    // Exempt because they seed the window's File Order from the record and store it back, or define the record;
    // a new entry needs the same reason. Paths are relative to src.
    private static readonly string[] SortingAllowedFiles =
    [
        @"PicView.Core/ViewModels/WindowSettings.cs",
        @"PicView.Core/FileSorting/FileOrder.cs",
        @"PicView.Core/Config/AppSettings.cs",
        @"PicView.Core/Config/SettingsManager.cs"
    ];

    private static bool OffendsSorting(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        var stripped = IncludeSubDirectoriesRead().Replace(line, string.Empty);
        return SortingGroup().IsMatch(stripped);
    }

    private static List<string> FindOffenders(Func<string, bool> offends, string[] allowedFiles)
    {
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(SourceTree.Src, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(SourceTree.Src, file);
            var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (segments.Any(s => SkippedSegments.Contains(s, StringComparer.Ordinal)) ||
                allowedFiles.Contains(string.Join('/', segments), StringComparer.Ordinal))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (offends(lines[i]))
                {
                    offenders.Add($"{relative}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        return offenders;
    }

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
        var offenders = FindOffenders(Offends, AllowedFiles);

        Assert.True(offenders.Count == 0,
            "Window Settings are read from the window, not the record:" + Environment.NewLine +
            string.Join(Environment.NewLine, offenders));
    }

    [Theory]
    [InlineData("var f = Settings.Sorting.SortPreference;", true)]
    [InlineData("Settings.Sorting!.Ascending = false;", true)]
    [InlineData("var sorting = Settings.Sorting;", true)]
    [InlineData("if (Settings.Sorting?.Name == true)", true)]
    [InlineData("var s = SettingsManager.Settings.Sorting.Name;", true)]
    [InlineData("if (Settings.Sorting.IncludeSubDirectories)", false)]
    [InlineData("if (Settings.Sorting!.IncludeSubDirectories)", false)]
    [InlineData("if (Settings.Sorting?.IncludeSubDirectories == true)", false)]
    [InlineData("// Settings.Sorting.SortPreference", false)]
    [InlineData("/// Settings.Sorting.Ascending", false)]
    [InlineData("var x = tab.CurrentOrdering();", false)]
    public void OffendsSorting_Line_MatchesTheRule(string line, bool expected)
    {
        Assert.Equal(expected, OffendsSorting(line));
    }

    [Fact]
    public void ProductionCode_ReadsTheSortOnlyThroughTheWindow()
    {
        var offenders = FindOffenders(OffendsSorting, SortingAllowedFiles);

        Assert.True(offenders.Count == 0,
            "The sort field and direction belong to the window (WindowSettings.FileOrder), not the record:" +
            Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }
}
