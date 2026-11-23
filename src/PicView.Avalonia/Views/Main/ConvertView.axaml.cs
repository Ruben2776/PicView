using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.UI;
using PicView.Core.Localization;
using PicView.Core.Saving;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Views.Main;

public partial class ConvertView : UserControl
{
    public ConvertView()
    {
        InitializeComponent();
        
        Loaded += delegate
        {
            if (DataContext is not MainWindowViewModel vm)
            {
                return;
            }
        
            SaveButton.Click += async (_, _) => await ConvertAsync(vm, saveAs: false).ConfigureAwait(false);

            SaveAsButton.Click += async (_, _) => await ConvertAsync(vm, saveAs: true).ConfigureAwait(false);

            CancelButton.Click += (_, _) => SafeClose();
        };
    }
    
    // Reads the tab and the chosen format once, at the click, and writes it through the save module. A Single Image has
    // no file to replace, so Save asks where to save it as Save As does
    private async Task ConvertAsync(MainWindowViewModel vm, bool saveAs)
    {
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        var source = tab.FileInfo.CurrentValue?.FullName;
        var destination = source is null ? "PicView.png" : GetDestinationWithChangedExtension(source);
        if (saveAs || source is null)
        {
            saveAs = true;
            destination = await FilePicker.PickFileForSavingAsync(destination).ConfigureAwait(false);
            if (destination is null)
            {
                return;
            }
        }

        var result = await ConvertWriter.ConvertAsync(vm.Core.Writer, tab, destination, saveAs).ConfigureAwait(false);
        switch (result.Outcome)
        {
            case WriteOutcome.Written:
                SafeClose();
                break;
            case WriteOutcome.NameTaken:
                TooltipHelper.ShowTooltipMessage(vm,
                    TranslationManager.Translation.FileAlreadyExists + Environment.NewLine +
                    Path.GetFileName(destination), true);
                break;
        }
    }

    private void SafeClose()
    {
        Dispatcher.Invoke(() =>
        {
            if (TopLevel.GetTopLevel(this) is not Window window)
            {
                return;
            }
            window.Close();
        });
    }

    private string GetDestinationWithChangedExtension(string source)
    {
        if (NoConversion.IsSelected)
        {
            return source;
        }
        string ext;
        
        if (PngItem.IsSelected)
        {
            ext = ".png";
        }
        else if (JpgItem.IsSelected)
        {
            ext = ".jpg";
        }
        else if (WebpItem.IsSelected)
        {
            ext = ".webp";
        }
        else if (AvifItem.IsSelected)
        {
            ext = ".avif";
        }
        else if (HeicItem.IsSelected)
        {
            ext = ".heic";
        }
        else if (JxlItem.IsSelected)
        {
            ext = ".jxl";
        }
        else
        {
            return source;
        }
        
        source = Path.ChangeExtension(source, ext);
        return source;
    }

    private void BottomBorder_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not GenericWindow window)
        {
            return;
        }
        window.MoveWindow(sender,e);
    }
}