using PicView.Core.Config;
using PicView.Core.Localization;

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
        const Languages da = Languages.da;
        await TranslationHelper.ChangeLanguage((int)da);
        Assert.Equal("Billede", TranslationHelper.GetTranslation("Image"));
    }

    [Fact]
    public async Task CheckDanishLanguage()
    {
        var exists = await TranslationHelper.LoadLanguage("da");
        Assert.True(exists);
        Assert.Equal("Billede", TranslationHelper.GetTranslation("Image"));
        Assert.Equal("filer", TranslationHelper.GetTranslation("Files"));
        Assert.Equal("Indstillinger", TranslationHelper.GetTranslation("Settings"));
    }

    [Fact]
    public async Task CheckGermanLanguage()
    {
        var exists = await TranslationHelper.LoadLanguage("de");
        Assert.True(exists);
        Assert.Equal("Bild", TranslationHelper.GetTranslation("Image"));
        Assert.Equal("Dateien", TranslationHelper.GetTranslation("Files"));
        Assert.Equal("Einstellungen", TranslationHelper.GetTranslation("Settings"));
    }

    [Fact]
    public async Task CheckEnglishLanguage()
    {
        var exists = await TranslationHelper.LoadLanguage("en");
        Assert.True(exists);
        Assert.Equal("Image", TranslationHelper.GetTranslation("Image"));
        Assert.Equal("files", TranslationHelper.GetTranslation("Files"));
        Assert.Equal("Settings", TranslationHelper.GetTranslation("Settings"));
    }

    [Fact]
    public async Task CheckSpanishLanguage()
    {
        var exists = await TranslationHelper.LoadLanguage("es");
        Assert.True(exists);
        Assert.Equal("Imagen", TranslationHelper.GetTranslation("Image"));
        Assert.Equal("archivos", TranslationHelper.GetTranslation("Files"));
        Assert.Equal("Opciones", TranslationHelper.GetTranslation("Settings"));
    }

    [Fact]
    public async Task CheckFrenchLanguage()
    {
        var exists = await TranslationHelper.LoadLanguage("fr");
        Assert.True(exists);
        Assert.Equal("Image", TranslationHelper.GetTranslation("Image"));
        Assert.Equal("fichiers", TranslationHelper.GetTranslation("Files"));
        Assert.Equal("Paramètres", TranslationHelper.GetTranslation("Settings"));
    }

    [Fact]
    public async Task CheckItalianLanguage()
    {
        var exists = await TranslationHelper.LoadLanguage("it");
        Assert.True(exists);
        Assert.Equal("Immagine", TranslationHelper.GetTranslation("Image"));
        Assert.Equal("File", TranslationHelper.GetTranslation("Files"));
        Assert.Equal("Impostazioni", TranslationHelper.GetTranslation("Settings"));
    }

    [Fact]
    public async Task CheckKoreanLanguage()
    {
        var exists = await TranslationHelper.LoadLanguage("ko");
        Assert.True(exists);
        Assert.Equal("이미지", TranslationHelper.GetTranslation("Image"));
        Assert.Equal("파일", TranslationHelper.GetTranslation("Files"));
        Assert.Equal("설정", TranslationHelper.GetTranslation("Settings"));
    }

    [Fact]
    public async Task CheckPolishLanguage()
    {
        var exists = await TranslationHelper.LoadLanguage("pl");
        Assert.True(exists);
        Assert.Equal("Obraz", TranslationHelper.GetTranslation("Image"));
        Assert.Equal("pliki", TranslationHelper.GetTranslation("Files"));
        Assert.Equal("Ustawienia", TranslationHelper.GetTranslation("Settings"));
    }

    [Fact]
    public async Task CheckRomanianLanguage()
    {
        var exists = await TranslationHelper.LoadLanguage("ro");
        Assert.True(exists);
        Assert.Equal("Imagine", TranslationHelper.GetTranslation("Image"));
        Assert.Equal("fișiere", TranslationHelper.GetTranslation("Files"));
        Assert.Equal("Setări", TranslationHelper.GetTranslation("Settings"));
    }

    [Fact]
    public async Task CheckRussianLanguage()
    {
        var exists = await TranslationHelper.LoadLanguage("ru");
        Assert.True(exists);
        Assert.Equal("Изображение", TranslationHelper.GetTranslation("Image"));
        Assert.Equal("Файлы", TranslationHelper.GetTranslation("Files"));
        Assert.Equal("Настройки", TranslationHelper.GetTranslation("Settings"));
    }

    [Fact]
    public async Task CheckChineseSimplifiedLanguage()
    {
        var exists = await TranslationHelper.LoadLanguage("zh-CN");
        Assert.True(exists);
        Assert.Equal("图片", TranslationHelper.GetTranslation("Image"));
        Assert.Equal("文件", TranslationHelper.GetTranslation("Files"));
        Assert.Equal("设置", TranslationHelper.GetTranslation("Settings"));
    }

    [Fact]
    public async Task CheckChineseTraditionalLanguage()
    {
        var exists = await TranslationHelper.LoadLanguage("zh-TW");
        Assert.True(exists);
        Assert.Equal("圖片", TranslationHelper.GetTranslation("Image"));
        Assert.Equal("檔案", TranslationHelper.GetTranslation("Files"));
        Assert.Equal("設定", TranslationHelper.GetTranslation("Settings"));
    }
}