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
        
        // Window titles
        Assert.Equal("About", TranslationHelper.GetTranslation("About"));
        Assert.Equal("Settings", TranslationHelper.GetTranslation("Settings"));
        
        // Clipboard
        Assert.Equal("Added to clipboard", TranslationHelper.GetTranslation("AddedToClipboard"));
        Assert.Equal("Copy", TranslationHelper.GetTranslation("Copy"));
        Assert.Equal("Paste", TranslationHelper.GetTranslation("Paste"));
        
        // EXIF data
        Assert.Equal("ActionProgram", TranslationHelper.GetTranslation("ActionProgram"));
        Assert.Equal("Camera maker", TranslationHelper.GetTranslation("CameraMaker"));
        
        // Basic UI elements
        Assert.Equal("Image", TranslationHelper.GetTranslation("Image"));
        Assert.Equal("files", TranslationHelper.GetTranslation("Files"));

        
        // Image operations
        Assert.Equal("Zoom in", TranslationHelper.GetTranslation("ZoomIn"));
        Assert.Equal("Zoom out", TranslationHelper.GetTranslation("ZoomOut"));
        Assert.Equal("Rotate left", TranslationHelper.GetTranslation("RotateLeft"));
        Assert.Equal("Rotate right", TranslationHelper.GetTranslation("RotateRight"));
        
        // Slideshow
        Assert.Equal("Start slideshow", TranslationHelper.GetTranslation("StartSlideshow"));
        Assert.Equal("Adjust timing for slideshow", TranslationHelper.GetTranslation("AdjustTimingForSlideshow"));
        Assert.Equal("Slideshow", TranslationHelper.GetTranslation("Slideshow"));
        
        // Zoom
        Assert.Equal("Adjust zooming speed", TranslationHelper.GetTranslation("AdjustTimingForZoom"));
        Assert.Equal("Avoid zooming out the image when it is already at the maximum size", TranslationHelper.GetTranslation("AllowZoomOut"));
        Assert.Equal("Reset zoom", TranslationHelper.GetTranslation("ResetZoom"));
        Assert.Equal("Zoom", TranslationHelper.GetTranslation("Zoom"));
        Assert.Equal("Zoom in", TranslationHelper.GetTranslation("ZoomIn"));
        Assert.Equal("Zoom out", TranslationHelper.GetTranslation("ZoomOut"));
        Assert.Equal("Ctrl to zoom, scroll to navigate", TranslationHelper.GetTranslation("CtrlToZoom"));
        Assert.Equal("Zoom with mousewheel, navigate with Ctrl", TranslationHelper.GetTranslation("ScrollToZoom"));
        
        // File operations
        Assert.Equal("Save", TranslationHelper.GetTranslation("Save"));
        Assert.Equal("Save as", TranslationHelper.GetTranslation("SaveAs"));
        Assert.Equal("Open", TranslationHelper.GetTranslation("Open"));
        Assert.Equal("Delete file", TranslationHelper.GetTranslation("DeleteFile"));
        
        // Navigation
        Assert.Equal("Next image", TranslationHelper.GetTranslation("NextImage"));
        Assert.Equal("Previous image", TranslationHelper.GetTranslation("PrevImage"));
        Assert.Equal("First image", TranslationHelper.GetTranslation("FirstImage"));
        Assert.Equal("Last image", TranslationHelper.GetTranslation("LastImage"));
        Assert.Equal("Adjust speed when key is held down", TranslationHelper.GetTranslation("AdjustNavSpeed"));
        
        // Window controls
        Assert.Equal("Fullscreen", TranslationHelper.GetTranslation("Fullscreen"));
        Assert.Equal("Minimize", TranslationHelper.GetTranslation("Minimize"));
        Assert.Equal("Maximize", TranslationHelper.GetTranslation("Maximize"));
        Assert.Equal("Close", TranslationHelper.GetTranslation("Close"));
        
        // Ratings
        Assert.Equal("1 star rating", TranslationHelper.GetTranslation("_1Star"));
        Assert.Equal("2 star rating", TranslationHelper.GetTranslation("_2Star"));
        Assert.Equal("3 star rating", TranslationHelper.GetTranslation("_3Star"));
        Assert.Equal("4 star rating", TranslationHelper.GetTranslation("_4Star"));
        Assert.Equal("5 star rating", TranslationHelper.GetTranslation("_5Star"));
        
        // Error messages
        Assert.Equal("Loading...", TranslationHelper.GetTranslation("Loading"));
        Assert.Equal("No image loaded", TranslationHelper.GetTranslation("NoImage"));
        Assert.Equal("Unsupported file", TranslationHelper.GetTranslation("UnsupportedFile"));
        
        // Theme settings
        Assert.Equal("Dark theme", TranslationHelper.GetTranslation("DarkTheme"));
        Assert.Equal("Light theme", TranslationHelper.GetTranslation("LightTheme"));
        Assert.Equal("Glass Theme", TranslationHelper.GetTranslation("GlassTheme"));
        
        // Gallery settings
        Assert.Equal("Show bottom gallery", TranslationHelper.GetTranslation("ShowBottomGallery"));
        Assert.Equal("Hide bottom gallery", TranslationHelper.GetTranslation("HideBottomGallery"));
        Assert.Equal("Gallery settings", TranslationHelper.GetTranslation("GallerySettings"));
        
        // Image effects
        Assert.Equal("Effects", TranslationHelper.GetTranslation("Effects"));
        Assert.Equal("Black & White", TranslationHelper.GetTranslation("BlackAndWhite"));
        Assert.Equal("Blur", TranslationHelper.GetTranslation("Blur"));
        Assert.Equal("Brightness", TranslationHelper.GetTranslation("Brightness"));
        Assert.Equal("Contrast", TranslationHelper.GetTranslation("Contrast"));
        
        // Image info
        Assert.Equal("Width", TranslationHelper.GetTranslation("Width"));
        Assert.Equal("Height", TranslationHelper.GetTranslation("Height"));
        Assert.Equal("Resolution", TranslationHelper.GetTranslation("Resolution"));
        Assert.Equal("Aspect ratio", TranslationHelper.GetTranslation("AspectRatio"));
        Assert.Equal("File size", TranslationHelper.GetTranslation("FileSize"));
        
        // Misc
        Assert.Equal("Additional functions", TranslationHelper.GetTranslation("AdditionalFunctions"));
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