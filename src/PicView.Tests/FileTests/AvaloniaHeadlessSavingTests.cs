using PicView.Avalonia.FileSystem;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Tests.FileTests;

[Collection("Sequential")]
public class AvaloniaHeadlessSavingTests
{
    public AvaloniaHeadlessSavingTests()
    {
        LoadSettings();

        TranslationManager.Init();
        TranslationManager.Translation.Folder = "Folder";
        TranslationManager.Translation.SaveAs = "SaveAs";
        TranslationManager.Translation.OpenFileDialog = "Open";
    }

    private static MainWindowViewModel CreateDummyVm()
    {
        // Try creating with null dependencies since they might just be saved to properties
        return new MainWindowViewModel(null!, null!, null!, null!);
    }

    [Fact]
    public async Task SaveCurrentFile_HeadlessNoopProvider_ReturnsFalseWhenContextIsNull()
    {
        var vm = CreateDummyVm();
        var pickerService = new FilePickerService(null);
        var savingService = new FileSavingService(pickerService);
        var result = await savingService.SaveCurrentFile(vm);
        
        Assert.False(result);
    }
    
    [Fact]
    public async Task SaveFileAs_HeadlessNoopProvider_ReturnsFalse()
    {
        var vm = CreateDummyVm();
        var pickerService = new FilePickerService(null);
        var savingService = new FileSavingService(pickerService);
        var result = await savingService.SaveFileAs(vm);
        
        Assert.False(result);
    }

    [Fact]
    public async Task SaveFileAsync_HeadlessNoopProvider_ReturnsFalseWhenDataContextIsNull()
    {
        var vm = CreateDummyVm();
        var pickerService = new FilePickerService(null);
        var savingService = new FileSavingService(pickerService);
        var result = await savingService.SaveFileAsync("test.jpg", "test.jpg", vm);
        
        Assert.False(result);
    }
}
