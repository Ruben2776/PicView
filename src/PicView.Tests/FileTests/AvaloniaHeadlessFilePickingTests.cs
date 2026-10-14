using Avalonia.Controls;
using Avalonia.Threading;
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
    
    private T RunWithDispatcher<T>(Task<T> task)
    {
        while (!task.IsCompleted)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
        return task.GetAwaiter().GetResult();
    }

    [AvaloniaFact]
    public void SelectFile_HeadlessNoopProvider_ShouldReturnNull()
    {
        var window = new Window();
        var sp = window.StorageProvider;
        
        var service = new FilePickerService(sp);
        var result = RunWithDispatcher(service.SelectFile());
        
        Assert.Null(result);
    }

    [AvaloniaFact]
    public void SelectDirectory_HeadlessNoopProvider_ShouldReturnEmptyString()
    {
        var window = new Window();
        var sp = window.StorageProvider;
        
        var service = new FilePickerService(sp);
        var result = RunWithDispatcher(service.SelectDirectory());
        
        Assert.Equal(string.Empty, result);
    }

    [AvaloniaFact]
    public void PickFileForSavingAsync_HeadlessNoopProvider_ShouldReturnNull()
    {
        var window = new Window();
        var sp = window.StorageProvider;
        
        var service = new FilePickerService(sp);
        var result = RunWithDispatcher(service.PickFileForSavingAsync("test.jpg"));
        
        Assert.Null(result);
    }
}
