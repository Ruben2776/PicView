using System.Windows;
using PicView.Core.Localization;

namespace PicView.WPF.Views.UserControls.Misc;

public partial class DragDropOverlay
{
    public DragDropOverlay()
    {
        InitializeComponent();
        TextMsg.Text = TranslationHelper.Translation.DropToLoad;
    }

    public void UpdateContent(UIElement element)
    {
        TextMsg.Text = TranslationHelper.Translation.DropToLoad;
        if (element is null)
        {
            return;
        }
        try
        {
            ContentHolder.Content = element;
        }
        catch (Exception)
        {
            return;
        }
    }
}