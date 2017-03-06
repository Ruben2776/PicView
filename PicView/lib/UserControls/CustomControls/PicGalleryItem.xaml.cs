using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace PicView.lib.UserControls.CustomControls
{
    /// <summary>
    /// Interaction logic for PicGalleryItem.xaml
    /// </summary>
    public partial class PicGalleryItem : UserControl
    {
        public PicGalleryItem(string file)
        {
            InitializeComponent();

            var pic = ImageManager.GetBitmapSourceThumb(file);
            var x = Path.GetFileName(file);

            img.Source = pic;
            img.ToolTip += x;
            txt.MaxWidth = img.Width;
            txt.ToolTip = file;
            txt.Text = x;
        }
    }
}
