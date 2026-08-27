using PicView.Core.MotionPhoto;

namespace PicView.Tests.MotionPhoto;

public class MotionPhotoDetectorTests : IDisposable
{
    private readonly string _tempDirectory = MotionPhotoFixtures.CreateTempDirectory();

    public void Dispose()
    {
        MotionPhotoFixtures.DeleteDirectory(_tempDirectory);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void TryDetectFromXmp_NewStandardElementForm_ReturnsEmbeddedInfo()
    {
        var xmp = MotionPhotoFixtures.NewStandardXmp(4000);

        var result = MotionPhotoDetector.TryDetectFromXmp(10000, xmp);

        Assert.NotNull(result);
        Assert.Equal(MotionPhotoSource.EmbeddedXmp, result.Source);
        Assert.Equal(6000, result.VideoOffset);
        Assert.Equal(4000, result.VideoLength);
    }

    [Fact]
    public void TryDetectFromXmp_NewStandardAttributeForm_ReturnsEmbeddedInfo()
    {
        const string xmp =
            """
            <Container:Item Item:Semantic="MotionPhoto" Item:Mime="video/mp4" Item:Length="2500"/>
            """;

        var result = MotionPhotoDetector.TryDetectFromXmp(8000, xmp);

        Assert.NotNull(result);
        Assert.Equal(5500, result.VideoOffset);
        Assert.Equal(2500, result.VideoLength);
    }

    [Fact]
    public void TryDetectFromXmp_LengthBeforeSemantic_FindsLengthViaBackwardSearch()
    {
        const string xmp =
            """
            <Container:Item Item:Mime="video/mp4" Item:Length="1500" Item:Semantic="MotionPhoto"/>
            """;

        var result = MotionPhotoDetector.TryDetectFromXmp(8000, xmp);

        Assert.NotNull(result);
        Assert.Equal(6500, result.VideoOffset);
        Assert.Equal(1500, result.VideoLength);
    }

    [Fact]
    public void TryDetectFromXmp_MicroVideoOffset_ReturnsOffsetFromEnd()
    {
        var xmp = MotionPhotoFixtures.MicroVideoXmp(3000);

        var result = MotionPhotoDetector.TryDetectFromXmp(10000, xmp);

        Assert.NotNull(result);
        Assert.Equal(MotionPhotoSource.EmbeddedXmp, result.Source);
        Assert.Equal(7000, result.VideoOffset);
        Assert.Equal(3000, result.VideoLength);
    }

    [Fact]
    public void TryDetectFromXmp_VendorNamespaceVariant_Detected()
    {
        // Vendor namespaces differ (OpCamera/dji/...); detection must not depend on them.
        const string xmp =
            """
            <rdf:Description xmlns:OpCamera="http://ns.oppo.com/camera/">
              <OpCamera:MicroVideoOffset>1200</OpCamera:MicroVideoOffset>
            </rdf:Description>
            """;

        var result = MotionPhotoDetector.TryDetectFromXmp(5000, xmp);

        Assert.NotNull(result);
        Assert.Equal(3800, result.VideoOffset);
    }

    [Fact]
    public void TryDetectFromXmp_LengthExceedsFileSize_ReturnsNull()
    {
        var xmp = MotionPhotoFixtures.NewStandardXmp(20000);

        var result = MotionPhotoDetector.TryDetectFromXmp(10000, xmp);

        Assert.Null(result);
    }

    [Fact]
    public void TryDetectFromXmp_PlainXmp_ReturnsNull()
    {
        var result = MotionPhotoDetector.TryDetectFromXmp(10000, MotionPhotoFixtures.PlainXmp);

        Assert.Null(result);
    }

    [Fact]
    public void TryDetect_LivpExtension_ReturnsLivpContainer()
    {
        var path = Path.Combine(_tempDirectory, "IMG_0001.livp");
        File.WriteAllBytes(path, [1, 2, 3, 4]);

        var result = MotionPhotoDetector.TryDetect(new FileInfo(path), null);

        Assert.NotNull(result);
        Assert.Equal(MotionPhotoSource.LivpContainer, result.Source);
    }

    [Fact]
    public void TryDetect_JpegWithEmbeddedXmp_DetectsEmbeddedVideo()
    {
        // The XMP packet is only present inside the file bytes (xmpPacket argument is null),
        // so this exercises the JPEG APP1 byte-scan fallback.
        var video = MotionPhotoFixtures.BuildMp4Head(64);
        var xmp = MotionPhotoFixtures.NewStandardXmp(video.Length);
        var file = MotionPhotoFixtures.CreateEmbeddedMotionPhoto(_tempDirectory, "pixel.jpg", video, xmp);

        var result = MotionPhotoDetector.TryDetect(file, null);

        Assert.NotNull(result);
        Assert.Equal(MotionPhotoSource.EmbeddedXmp, result.Source);
        Assert.Equal(file.Length - video.Length, result.VideoOffset);
    }

    [Fact]
    public void ReadJpegXmpPacket_FileWithXmpSegment_ReturnsPacket()
    {
        var jpeg = MotionPhotoFixtures.BuildJpegWithXmp(MotionPhotoFixtures.PlainXmp);
        var path = Path.Combine(_tempDirectory, "xmp.jpg");
        File.WriteAllBytes(path, jpeg);

        var packet = MotionPhotoDetector.ReadJpegXmpPacket(new FileInfo(path));

        Assert.NotNull(packet);
        Assert.Contains("x:xmpmeta", packet);
    }

    [Fact]
    public void TryDetect_SamsungTrailer_FindsVideoAfterMarker()
    {
        var video = MotionPhotoFixtures.BuildMp4Head(48);
        var path = Path.Combine(_tempDirectory, "samsung.jpg");
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            stream.Write(new byte[1024]);
            stream.Write("MotionPhoto_Data"u8);
            stream.Write(video);
        }

        var result = MotionPhotoDetector.TryDetect(new FileInfo(path), MotionPhotoFixtures.PlainXmp);

        Assert.NotNull(result);
        Assert.Equal(MotionPhotoSource.SamsungTrailer, result.Source);
        Assert.Equal(1024 + "MotionPhoto_Data".Length, result.VideoOffset);
    }

    [Fact]
    public void TryDetect_Sidecar_PrefersMovOverMp4()
    {
        var imagePath = Path.Combine(_tempDirectory, "IMG_100.heic");
        File.WriteAllBytes(imagePath, [1, 2, 3]);
        File.WriteAllBytes(Path.Combine(_tempDirectory, "IMG_100.mov"), MotionPhotoFixtures.BuildMp4Head(24));
        File.WriteAllBytes(Path.Combine(_tempDirectory, "IMG_100.mp4"), MotionPhotoFixtures.BuildMp4Head(32));

        var result = MotionPhotoDetector.TryDetect(new FileInfo(imagePath), MotionPhotoFixtures.PlainXmp);

        Assert.NotNull(result);
        Assert.Equal(MotionPhotoSource.Sidecar, result.Source);
        Assert.EndsWith(".mov", result.SidecarFile?.Name);
    }

    [Fact]
    public void TryDetect_Sidecar_FallsBackToMp4()
    {
        var imagePath = Path.Combine(_tempDirectory, "IMG_200.jpg");
        File.WriteAllBytes(imagePath, [1, 2, 3]);
        File.WriteAllBytes(Path.Combine(_tempDirectory, "IMG_200.mp4"), MotionPhotoFixtures.BuildMp4Head(32));

        var result = MotionPhotoDetector.TryDetect(new FileInfo(imagePath), MotionPhotoFixtures.PlainXmp);

        Assert.NotNull(result);
        Assert.Equal(MotionPhotoSource.Sidecar, result.Source);
        Assert.EndsWith(".mp4", result.SidecarFile?.Name);
    }

    [Fact]
    public void TryDetect_SidecarWithoutVideoHeader_ReturnsNull()
    {
        // A same-named file that is not a video must not be treated as a motion photo sidecar.
        var imagePath = Path.Combine(_tempDirectory, "IMG_250.heic");
        File.WriteAllBytes(imagePath, [1, 2, 3]);
        File.WriteAllBytes(Path.Combine(_tempDirectory, "IMG_250.mp4"), [5, 6, 7, 8, 9, 10]);

        var result = MotionPhotoDetector.TryDetect(new FileInfo(imagePath), MotionPhotoFixtures.PlainXmp);

        Assert.Null(result);
    }

    [Fact]
    public void TryDetect_HeicWithSamsungTrailerMarker_ReturnsNull()
    {
        // The Samsung trailer scan only applies to JPEG files.
        var video = MotionPhotoFixtures.BuildMp4Head(48);
        var path = Path.Combine(_tempDirectory, "samsung.heic");
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            stream.Write(new byte[1024]);
            stream.Write("MotionPhoto_Data"u8);
            stream.Write(video);
        }

        var result = MotionPhotoDetector.TryDetect(new FileInfo(path), MotionPhotoFixtures.PlainXmp);

        Assert.Null(result);
    }

    [Fact]
    public void TryDetectFromXmp_MotionItemWithoutLength_IgnoresSiblingLength()
    {
        // The still-image item carries its own Item:Length, which must not be used
        // as the video length when the MotionPhoto item lacks one.
        const string xmp =
            """
            <Container:Directory>
              <rdf:Seq>
                <rdf:li rdf:parseType="Resource">
                  <Container:Item Item:Semantic="Primary" Item:Mime="image/jpeg" Item:Length="3000"/>
                </rdf:li>
                <rdf:li rdf:parseType="Resource">
                  <Container:Item Item:Semantic="MotionPhoto" Item:Mime="video/mp4"/>
                </rdf:li>
              </rdf:Seq>
            </Container:Directory>
            """;

        var result = MotionPhotoDetector.TryDetectFromXmp(10000, xmp);

        Assert.Null(result);
    }

    [Fact]
    public void TryDetect_NoMotionPhotoData_ReturnsNull()
    {
        var imagePath = Path.Combine(_tempDirectory, "plain.jpg");
        File.WriteAllBytes(imagePath, new byte[2048]);

        var result = MotionPhotoDetector.TryDetect(new FileInfo(imagePath), MotionPhotoFixtures.PlainXmp);

        Assert.Null(result);
    }

    [Fact]
    public void TryDetect_NonexistentFile_ReturnsNull()
    {
        var result = MotionPhotoDetector.TryDetect(new FileInfo(Path.Combine(_tempDirectory, "missing.jpg")), null);

        Assert.Null(result);
    }
}
