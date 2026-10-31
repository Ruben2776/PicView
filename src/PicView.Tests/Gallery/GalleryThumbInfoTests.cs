using System.Globalization;
using PicView.Core.Gallery;
using PicView.Core.Localization;

namespace PicView.Tests.Gallery;

[Collection("Sequential")]
public class GalleryThumbInfoTests : IDisposable
{
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "PicViewGalleryThumbInfoTests_" + Guid.NewGuid().ToString("N"));

    public GalleryThumbInfoTests()
    {
        SetDefaults();
        TranslationManager.LoadLanguage("en");
        Directory.CreateDirectory(_folder);
    }

    public void Dispose() => Directory.Delete(_folder, true);

    // A gallery item shows its image size once its details are read, so the file alone describes it
    [Fact]
    public void GetThumbData_OfAFile_DescribesTheFile()
    {
        var path = Path.Combine(_folder, "photo.jpg");
        File.WriteAllBytes(path, new byte[100]);
        File.SetLastWriteTimeUtc(path, new DateTime(2026, 10, 31, 12, 30, 45, DateTimeKind.Utc));
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(new FileInfo(path));

            Assert.Equal(path, thumbData.FileLocation);
            Assert.Equal("photo", thumbData.FileName);
            Assert.Equal("File size: 100 B", thumbData.FileSize);
            Assert.Equal("Modified: 10/31/2026 12:30:45", thumbData.FileDate);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }
}
