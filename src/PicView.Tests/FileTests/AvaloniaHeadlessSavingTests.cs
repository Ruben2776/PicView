using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using PicView.Avalonia;
using PicView.Avalonia.FileSystem;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Tests.FileTests;

public class AvaloniaHeadlessSavingTests
{
    private static bool _setupDone;
    
    public AvaloniaHeadlessSavingTests()
    {
        if (!_setupDone)
        {
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
            
            LoadSettings();

            TranslationManager.Init();
            if (TranslationManager.Translation != null)
            {
                TranslationManager.Translation.Folder = "Folder";
                TranslationManager.Translation.SaveAs = "SaveAs";
                TranslationManager.Translation.OpenFileDialog = "Open";
            }

            _setupDone = true;
        }
    }

    private T RunWithDispatcher<T>(ValueTask<T> task)
    {
        while (!task.IsCompleted)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
        return task.GetAwaiter().GetResult();
    }

    private MainWindowViewModel CreateDummyVm()
    {
        // Try creating with null dependencies since they might just be saved to properties
        return new MainWindowViewModel(null!, null!, null!, null!);
    }

    [Fact]
    public void SaveCurrentFile_HeadlessNoopProvider_ReturnsFalseWhenContextIsNull()
    {
        var vm = CreateDummyVm();
        var window = new Window();
        var sp = window.StorageProvider;
        var pickerService = new FilePickerService(sp);
        var savingService = new FileSavingService(pickerService);
        
        var result = RunWithDispatcher(savingService.SaveCurrentFile(vm));
        
        Assert.False(result);
    }
    
    [Fact]
    public void SaveFileAs_HeadlessNoopProvider_ReturnsFalse()
    {
        var vm = CreateDummyVm();
        var window = new Window();
        var sp = window.StorageProvider;
        var pickerService = new FilePickerService(sp);
        var savingService = new FileSavingService(pickerService);
        
        var result = RunWithDispatcher(savingService.SaveFileAs(vm));
        
        Assert.False(result);
    }

    [Fact]
    public void SaveFileAsync_HeadlessNoopProvider_ReturnsFalseWhenDataContextIsNull()
    {
        var vm = CreateDummyVm();
        var window = new Window();
        var sp = window.StorageProvider;
        var pickerService = new FilePickerService(sp);
        var savingService = new FileSavingService(pickerService);
        
        var result = RunWithDispatcher(savingService.SaveFileAsync("test.jpg", "test.jpg", vm));
        
        Assert.False(result);
    }
}
