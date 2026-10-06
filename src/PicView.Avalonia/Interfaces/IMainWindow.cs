using Avalonia.Controls;
using PicView.Avalonia.Clipboard;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.UC;
using R3;
using R3.Avalonia;

namespace PicView.Avalonia.Interfaces;

public interface IMainWindow
{
    CompositeDisposable Disposables { get; set; }
    
    bool IsChangingWindowState { get; set; }
    
    BottomBar? SharedBottomBar { get; set; }
    
    MainTitleBar? SharedTitleBar { get; set; }
    
    AvaloniaRenderingFrameProvider FrameProvider { get; set; }
    
    ClipboardCopyService? ClipboardCopyService { get; }
}
