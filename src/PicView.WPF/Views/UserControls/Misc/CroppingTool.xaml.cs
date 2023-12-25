using PicView.WPF.Editing.Crop;

namespace PicView.WPF.Views.UserControls.Misc;

public partial class CroppingTool
{
    public CroppingTool()
    {
        InitializeComponent();

        Loaded += delegate { CropFunctions.InitializeCrop(); };
    }
}