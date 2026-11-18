using System.Text.RegularExpressions;

namespace PicView.Tests.Config;

public partial class WindowSettingRecordReadsTests
{
    [GeneratedRegex(@"\bWindowProperties\b")]
    private static partial Regex WindowPropertiesWord();

    // The two properties that stay app-wide and are still read from the record.
    [GeneratedRegex(@"\bWindowProperties[!?]?\.(Margin|EscLeavesFullscreen)\b")]
    private static partial Regex AppWideRead();

    // Secondary windows keep their own remembered placement in a RememberedWindowConfig,
    // exposed as <something>Config.WindowProperties. Those are not the app settings record.
    [GeneratedRegex(@"\w*Config[!?]?\.WindowProperties\b", RegexOptions.IgnoreCase)]
    private static partial Regex SecondaryWindowConfigRead();

    private static readonly string[] SkippedSegments = ["bin", "obj", "PicView.Tests", "PicView.Benchmarks"];
    private static readonly string[] AllowedFiles = ["WindowSettings.cs", "AppSettings.cs", "SettingsManager.cs", "RememberedWindowConfig.cs"];

    private static bool Offends(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        var stripped = SecondaryWindowConfigRead().Replace(AppWideRead().Replace(line, string.Empty), string.Empty);
        return WindowPropertiesWord().IsMatch(stripped);
    }

    [Theory]
    [InlineData("var properties = Settings.WindowProperties;", true)]
    [InlineData("_ = Settings.WindowProperties!.AutoFit;", true)]
    [InlineData("var m = Settings.WindowProperties.Margin;", false)]
    [InlineData("if (Settings.WindowProperties?.EscLeavesFullscreen == true)", false)]
    [InlineData("config.WindowProperties!.LastTab = 1;", false)]
    [InlineData("vm.InfoWindow.ImageInfoWindowConfig?.WindowProperties is null", false)]
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
                AllowedFiles.Contains(segments[^1], StringComparer.Ordinal))
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
