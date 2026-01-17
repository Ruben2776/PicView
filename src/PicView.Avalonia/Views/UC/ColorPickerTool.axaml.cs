using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace PicView.Avalonia.Views.UC;

public partial class ColorPickerTool : UserControl
    {
        public ColorPickerTool()
        {
            InitializeComponent();
        }

        public void UpdateImage(Bitmap bitmap)
        {
            // Update the Image control, not a brush
            MagnifierImage.Source = bitmap;
        }

        public void UpdateColor(Color color)
        {
            HexLabel.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            
        }
    }