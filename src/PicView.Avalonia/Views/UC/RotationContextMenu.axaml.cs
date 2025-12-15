using Avalonia.Controls;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC;

public partial class RotationContextMenu : ContextMenu
{
    protected override Type StyleKeyOverride => typeof(ContextMenu);
    
    public RotationContextMenu()
    {
        InitializeComponent();
    }
}