using System.Text.RegularExpressions;

namespace PicView.Tests.Localization;

public partial class TranslationBindingTests
{
    // A binding whose path stops at the translation's bindable shows the bindable, not its text.
    [GeneratedRegex(@"Binding\s+(Path=)?([\w.]*\.)?Translation\.\w+\s*[,}]")]
    private static partial Regex BindingWithoutValue();

    [Fact]
    public void AxamlBindings_OnATranslation_EndInValue()
    {
        var separator = Path.DirectorySeparatorChar;
        var found = new List<string>();
        foreach (var file in Directory.EnumerateFiles(SourceTree.Src, "*.axaml", SearchOption.AllDirectories))
        {
            if (file.Contains($"{separator}bin{separator}", StringComparison.Ordinal) ||
                file.Contains($"{separator}obj{separator}", StringComparison.Ordinal))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (BindingWithoutValue().IsMatch(lines[i]))
                {
                    found.Add($"{Path.GetRelativePath(SourceTree.Src, file)}:{i + 1}");
                }
            }
        }

        Assert.True(found.Count == 0,
            "Bindings on a translation without .Value:" + Environment.NewLine +
            string.Join(Environment.NewLine, found));
    }
}
