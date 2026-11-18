using System.Text.Json;
using PicView.Core.Localization;

namespace PicView.Tests.Localization;

public class LanguageFileTests
{
    [Fact]
    public void EnglishFile_EveryModelProperty_HasAKey()
    {
        var path = Path.Combine(SourceTree.Src, "PicView.Core", "Config", "Languages", "en.json");
        using var document = JsonDocument.Parse(File.ReadAllBytes(path),
            new JsonDocumentOptions { AllowTrailingCommas = true });

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            keys.Add(property.Name);
        }

        var missing = new List<string>();
        foreach (var property in typeof(LanguageModel).GetProperties())
        {
            if (property.PropertyType == typeof(string) && !keys.Contains(property.Name))
            {
                missing.Add(property.Name);
            }
        }

        Assert.True(missing.Count == 0, "Missing from en.json: " + string.Join(", ", missing));
    }
}
