using PicView.Core.FileHandling;

namespace PicView.Tests.FileTests;

public class SupportedFilesTests
{
    [Theory]
    [InlineData("photo.jpg", true)]
    [InlineData("IMG_1234.JPG", true)]
    [InlineData("scan.Jpeg", true)]
    [InlineData("image.PNG", true)]
    [InlineData("picture.jfif", true)]
    [InlineData("photo.heic", false)]
    [InlineData("scan.TIF", false)]
    [InlineData("noextension", false)]
    public void IsCommon_Extension_IgnoresCase(string fileName, bool expected)
    {
        Assert.Equal(expected, fileName.IsCommon());
        Assert.Equal(expected, new FileInfo(fileName).IsCommon());
    }
}
