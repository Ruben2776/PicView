using System.Globalization;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Tests.Gallery;

/// <summary>
/// Pins the texts a gallery item's tooltip shows about its file. They come from the file alone; the image size comes
/// with the item's details instead.
/// </summary>
[Collection("Sequential")]
public class GalleryItemTextsTests : IDisposable
{
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "PicViewGalleryItemTextsTests_" + Guid.NewGuid().ToString("N"));

    public GalleryItemTextsTests()
    {
        SetDefaults();
        TranslationManager.LoadLanguage("en");
        Directory.CreateDirectory(_folder);
    }

    public void Dispose() => Directory.Delete(_folder, true);

    [Fact]
    public void Texts_OfAnItemWithAFile_DescribeTheFile()
    {
        var path = Path.Combine(_folder, "photo.jpg");
        File.WriteAllBytes(path, new byte[100]);
        File.SetLastWriteTimeUtc(path, new DateTime(2026, 10, 31, 12, 30, 45, DateTimeKind.Utc));
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            var item = new GalleryItemViewModel { FileInfo = new FileInfo(path) };

            Assert.Equal(path, item.FileLocation);
            Assert.Equal("photo", item.FileName);
            Assert.Equal("File size: 100 B", item.FileSize);
            Assert.Equal("Modified: 10/31/2026 12:30:45", item.FileDate);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Texts_OfAnItemWithoutAFile_AreEmpty()
    {
        var item = new GalleryItemViewModel();

        Assert.Equal(string.Empty, item.FileLocation);
        Assert.Equal(string.Empty, item.FileName);
        Assert.Equal(string.Empty, item.FileSize);
        Assert.Equal(string.Empty, item.FileDate);
    }
}
