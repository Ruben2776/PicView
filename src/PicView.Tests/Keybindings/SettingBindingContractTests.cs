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
                // A Main Window's own settings beside the Action that toggles them; one with no Action, as Stay
                // Centered in the Settings window, is written by its control
                if (!binding.Contains("GlobalSettings.", StringComparison.Ordinal) &&
                    !(binding.Contains("WindowSettings.", StringComparison.Ordinal) &&
                      ElementAt(text, match.Index).Contains("Command=", StringComparison.Ordinal)))
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

        Assert.True(total > 0, "No IsChecked binding to a published setting was found; the scan is broken.");
        Assert.True(offenders.Count == 0, "IsChecked bindings to a published setting without Mode=OneWay:\n" +
                                          string.Join("\n", offenders));
    }

    /// <summary>
    /// ADR-0009: a menu item that shows a toggled setting's state toggles it through its Action, so clicking it is
    /// never a check mark that changes nothing.
    /// </summary>
    [Fact]
    public void MenuItem_ShowingAToggledSetting_RunsAnAction()
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
            foreach (Match match in StyledMenuItemRegex().Matches(text))
            {
                var isChecked = IsCheckedBindingRegex().Match(match.Value);
                if (!isChecked.Success || IsInsideComment(text, match.Index) ||
                    (!isChecked.Value.Contains("GlobalSettings.", StringComparison.Ordinal) &&
                     !isChecked.Value.Contains("WindowSettings.", StringComparison.Ordinal)))
                {
                    continue;
                }

                total++;
                if (!match.Value.Contains("Command=", StringComparison.Ordinal))
                {
                    var line = text.AsSpan(0, match.Index).Count('\n') + 1;
                    offenders.Add($"{relative}:{line}");
                }
            }
        }

        Assert.True(total > 0, "No menu item showing a published setting was found; the scan is broken.");
        Assert.True(offenders.Count == 0, "Menu items that show a toggled setting but run no Action:\n" +
                                          string.Join("\n", offenders));
    }

    // The start tag around index; an attribute holds no '<' or '>'
    private static string ElementAt(string text, int index)
    {
        var start = text.LastIndexOf('<', index);
        var end = text.IndexOf('>', index);
        return text[start..(end + 1)];
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

    [GeneratedRegex(@"<customControls:StyledMenuItem\b[^>]*>")]
    private static partial Regex StyledMenuItemRegex();

    [GeneratedRegex("IsChecked=\"\\{[^\"]*\\}\"")]
    private static partial Regex IsCheckedBindingRegex();
}
