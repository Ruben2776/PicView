using System.Windows;
using System.Windows.Controls;

namespace PicView.Views.UserControls
{
    /// <summary>
    /// Interaction logic for DragDropOverlay.xaml
    /// </summary>
    public partial class DragDropOverlay : UserControl
    {
        public DragDropOverlay(UIElement element)
        {
            InitializeComponent();

            ContentHolder.Content = element;
        }
    }
}