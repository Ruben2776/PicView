using PicView.Editing.Crop;
using System.Windows.Controls;

namespace PicView.UILogic.UserControls
{
    /// <summary>
    /// Interaction logic for CroppingTool.xaml
    /// </summary>
    public partial class CroppingTool : UserControl
    {
        public CroppingTool()
        {
            InitializeComponent();

            Loaded += delegate { CropFunctions.InitilizeCrop(); };
        }
    }
}