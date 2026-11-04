using System.Text.RegularExpressions;

namespace PicView.Tests.Keybindings;

public partial class SettingBindingContractTests
{
    /// <summary>
    /// ADR-0020: a binding on a published setting is never TwoWay. A ToggleButton binds
    /// IsChecked TwoWay by default, so a TwoWay checkbox beside an Action that also writes
    /// the setting toggles twice. Every such binding must say <c>Mode=OneWay</c>.
    /// </summary>
    [Fact]
    public void IsChecked_BoundToAPublishedSetting_IsOneWay()
    {
        var root = FindSourceRoot();
        var total = 0;
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(root, "*.axaml", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (parts.Contains("bin") || parts.Contains("obj"))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (Match match in IsCheckedBindingRegex().Matches(text))
            {
                var binding = match.Value;
                if (!binding.Contains("GlobalSettings.", StringComparison.Ordinal))
                {
                    continue;
                }

                if (IsInsideComment(text, match.Index))
                {
                    continue;
                }

                total++;
                if (!binding.Contains("Mode=OneWay", StringComparison.Ordinal))
                {
                    var line = text.AsSpan(0, match.Index).Count('\n') + 1;
                    offenders.Add($"{relative}:{line}");
                }
            }
        }

        Assert.True(total > 0, "No IsChecked binding to GlobalSettings was found; the scan is broken.");
        Assert.True(offenders.Count == 0, "IsChecked bindings to a published setting without Mode=OneWay:\n" +
                                          string.Join("\n", offenders));
    }

    private static bool IsInsideComment(string text, int index)
    {
        var open = text.LastIndexOf("<!--", index, StringComparison.Ordinal);
        if (open < 0)
        {
            return false;
        }

        var close = text.LastIndexOf("-->", index, StringComparison.Ordinal);
        return close < open;
    }

    private static string FindSourceRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PicView.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("PicView.slnx not found above " + AppContext.BaseDirectory);
    }

    [GeneratedRegex("IsChecked=\"\\{[^\"]*\\}\"")]
    private static partial Regex IsCheckedBindingRegex();
}
