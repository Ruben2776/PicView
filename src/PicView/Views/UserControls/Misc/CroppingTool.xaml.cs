using PicView.Editing.Crop;

namespace PicView.Views.UserControls.Misc
{
    public partial class CroppingTool
    {
        public CroppingTool()
        {
            InitializeComponent();

            Loaded += delegate { CropFunctions.InitializeCrop(); };
        }
    }
}