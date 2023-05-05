using System.Windows.Controls;
using PicView.Editing.Crop;

namespace PicView.Views.UserControls.Misc
{
    /// <summary>
    /// Interaction logic for CroppingTool.xaml
    /// </summary>
    public partial class CroppingTool : UserControl
    {
        public CroppingTool()
        {
            InitializeComponent();

            Loaded += delegate { CropFunctions.InitializeCrop(); };
        }
    }
}