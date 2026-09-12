using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Win32.Views;

public partial class SingleImageResizeWindow : GenericWindow
{
    public SingleImageResizeWindow(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        GenericWindowHelper.GenericWindowInitialize(this, StringExtensions.CombineWithAppName(TranslationManager.Translation.Resize));
    }
}