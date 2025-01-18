using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.Tests.LanguageTests;

namespace PicView.Tests;

public class LanguageAndSettingsUnitTest
{
    [Fact]
    public async Task CheckIfSettingsWorks()
    {
        await SettingsHelper.LoadSettingsAsync();
        Assert.NotNull(SettingsHelper.Settings);
        var testSave = await SettingsHelper.SaveSettingsAsync();
        Assert.True(testSave);
    }

    [Fact]
    public void GetLanguages()
    {
        var languages = TranslationHelper.GetLanguages();
        Assert.NotNull(languages);
        Assert.NotEmpty(languages);
    }

    [Fact]
    public async Task ChangeLanguage()
    {
        await SettingsHelper.LoadSettingsAsync();
        Assert.NotNull(SettingsHelper.Settings);

        var exists = await TranslationHelper.LoadLanguage("en");
        Assert.True(exists);
        Assert.Equal("Image", TranslationHelper.Translation.Image);
        const Languages da = Languages.da;
        await TranslationHelper.ChangeLanguage((int)da);
        Assert.Equal("Billede", TranslationHelper.Translation.Image);
    }
    
    [Fact]
    public async Task CheckDanishLanguage()
    {
        await DanishUnitTest.CheckDanishLanguage();
    }
    
    [Fact]
    public async Task CheckEnglishLanguage()
    {
        await EnglishUnitTest.CheckEnglishLanguage();
    }
    
    [Fact]
    public async Task CheckGermanLanguage()
    {
        await GermanUnitTest.CheckGermanLanguage();
    }
    
    [Fact]
    public async Task CheckFrenchLanguage()
    {
        await FrenchUnitTest.CheckFrenchLanguage();
    }

    [Fact]
    public async Task CheckItalianLanguage()
    {
        await ItalianUnitTest.CheckItalianLanguage();
    }

    [Fact]
    public async Task CheckKoreanLanguage()
    {
        await KoreanUnitTest.CheckKoreanLanguage();
    }

    [Fact]
    public async Task CheckPolishLanguage()
    {
        await PolishUnitTest.CheckPolishLanguage();
    }

    [Fact]
    public async Task CheckBrazilianPortugueseLanguage()
    {
        await BrazilianPortugueseUnitTest.CheckBrazilianPortugueseLanguage();
    }

    [Fact]
    public async Task CheckRomanianLanguage()
    {
        await RomanianUnitTest.CheckRomanianLanguage();
    }

    [Fact]
    public async Task CheckRussianLanguage()
    {
        await RussianUnitTest.CheckRussianLanguage();
    }

    [Fact]
    public async Task CheckSwedishLanguage()
    {
        await SwedishUnitTest.CheckSwedishLanguage();
    }

    [Fact]
    public async Task CheckTurkishLanguage()
    {
        await TurkishUnitTest.CheckTurkishLanguage();
    }

    [Fact]
    public async Task CheckChineseSimplifiedLanguage()
    {
        await ChineseSimplifiedUnitTest.CheckChineseSimplifiedLanguage();
    }

    [Fact]
    public async Task CheckChineseTraditionalLanguage()
    {
        await ChineseTraditionalUnitTest.CheckChineseTraditionalLanguage();
    }
}