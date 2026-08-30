using PicView.Avalonia.FileSystem;
using PicView.Core.Localization;

namespace PicView.Tests.FileTests;

[Collection("Sequential")]
public class AvaloniaHeadlessFilePickingTests
{
    public AvaloniaHeadlessFilePickingTests()
    {
        TranslationManager.Init();
        TranslationManager.Translation.Folder = "Folder";
        TranslationManager.Translation.SaveAs = "SaveAs";
        TranslationManager.Translation.OpenFileDialog = "Open";
    }

    [Fact]
    public async Task SelectFile_HeadlessNoopProvider_ShouldReturnNull()
    {
        var service = new FilePickerService(null);
        var result = await service.SelectFile();
        
        Assert.Null(result);
    }

    [Fact]
    public async Task SelectDirectory_HeadlessNoopProvider_ShouldReturnEmptyString()
    {
        var service = new FilePickerService(null);
        var result = await service.SelectDirectory();
        
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task PickFileForSavingAsync_HeadlessNoopProvider_ShouldReturnNull()
    {
        var service = new FilePickerService(null);
        var result = await service.PickFileForSavingAsync("test.jpg");
        
        Assert.Null(result);
    }
}
