using Avalonia.Controls;
using PicView.Avalonia.UI;
using PicView.Core.Localization;

namespace PicView.Avalonia.MacOS.Views;

public partial class ConvertWindow : Window
{
    public ConvertWindow()
    {
        InitializeComponent();
        GenericWindowHelper.GenericWindowInitialize(this, TranslationManager.Translation.ConvertTo + " - PicView");
    }
}