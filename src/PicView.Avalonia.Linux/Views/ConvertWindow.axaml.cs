using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Extensions;
using PicView.Core.Localization;

namespace PicView.Avalonia.Linux.Views;

public partial class ConvertWindow : GenericWindow
{
    public ConvertWindow()
    {
        InitializeComponent();
        GenericWindowHelper.GenericWindowInitialize(this, StringExtensions.CombineWithAppName(TranslationManager.Translation.ConvertTo));
    }
}