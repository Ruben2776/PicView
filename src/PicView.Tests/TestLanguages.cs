using PicView.Core.Localization;

namespace PicView.Tests;

internal static class TestLanguages
{
    /// <summary>
    /// A language where every text is its key and a suffix, so a test can tell which text it reads and in which
    /// language.
    /// </summary>
    public static LanguageModel Suffixed(string suffix)
    {
        var model = new LanguageModel();
        foreach (var property in typeof(LanguageModel).GetProperties())
        {
            if (property.PropertyType == typeof(string))
            {
                property.SetValue(model, $"{property.Name}-{suffix}");
            }
        }

        return model;
    }
}
