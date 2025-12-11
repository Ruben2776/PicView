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

    public void UpdateSubscription()
    {
        Observable.EveryValueChanged(this, x => IsOpen, UIHelper.GetFrameProvider)
            .Subscribe(_ => { UpdateRotation(); });
    }
    
    private void UpdateRotation()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        Rotation0Item.IsChecked = false;
        Rotation90Item.IsChecked = false;
        Rotation180Item.IsChecked = false;
        Rotation270Item.IsChecked = false;
        switch (vm.PicViewer.RotationAngle.CurrentValue)
        {
            case 0:
                Rotation0Item.IsChecked = true;
                break;
            case 90:
                Rotation90Item.IsChecked = true;
                break;
            case 180:
                Rotation180Item.IsChecked = true;
                break;
            case 270:
                Rotation270Item.IsChecked = true;
                break;
        }
    }
}