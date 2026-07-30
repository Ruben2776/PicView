using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Extensions;
using PicView.Core.Localization;

namespace PicView.Avalonia.Win32.Views;

public partial class ConvertWindow : GenericWindow
{
    public ConvertWindow()
    {
        InitializeComponent();
        GenericWindowHelper.GenericWindowInitialize(this, StringExtensions.CombineWithAppName(TranslationManager.Translation.FileConversion));
    }
}