using Avalonia.Controls;

namespace PicView.Avalonia.Views;

public partial class ShortcutsView : UserControl
{
    public ShortcutsView()
    {
        InitializeComponent();
        NextBox.KeyUp += delegate
        {
            LastImageBox.Text = LastImageBox.GetFunctionKey();
            NextFolderBox.Text = NextFolderBox.GetFunctionKey();
        };
    }
}