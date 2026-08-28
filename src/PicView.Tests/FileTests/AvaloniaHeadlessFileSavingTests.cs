using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using PicView.Avalonia;
using PicView.Avalonia.FileSystem;
using PicView.Core.Localization;

namespace PicView.Tests.FileTests;

public class AvaloniaHeadlessFilePickingTests
{
    private static bool _setupDone;
    
    public AvaloniaHeadlessFilePickingTests()
    {
        if (!_setupDone)
        {
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
            
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

    private T RunWithDispatcher<T>(Task<T> task)
    {
        while (!task.IsCompleted)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
        return task.GetAwaiter().GetResult();
    }

    [Fact]
    public void SelectFile_HeadlessNoopProvider_ShouldReturnNull()
    {
        var window = new Window();
        var sp = window.StorageProvider;
        
        var service = new FilePickerService(sp);
        var result = RunWithDispatcher(service.SelectFile());
        
        Assert.Null(result);
    }

    [Fact]
    public void SelectDirectory_HeadlessNoopProvider_ShouldReturnEmptyString()
    {
        var window = new Window();
        var sp = window.StorageProvider;
        
        var service = new FilePickerService(sp);
        var result = RunWithDispatcher(service.SelectDirectory());
        
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void PickFileForSavingAsync_HeadlessNoopProvider_ShouldReturnNull()
    {
        var window = new Window();
        var sp = window.StorageProvider;
        
        var service = new FilePickerService(sp);
        var result = RunWithDispatcher(service.PickFileForSavingAsync("test.jpg"));
        
        Assert.Null(result);
    }
}
