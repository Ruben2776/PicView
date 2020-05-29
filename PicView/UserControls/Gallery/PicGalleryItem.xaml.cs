using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static PicView.Fields;

namespace PicView.UserControls
{
    /// <summary>
    /// Interaction logic for PicGalleryItem.xaml
    /// </summary>
    public partial class PicGalleryItem : UserControl
    {
        //internal bool Selected { get; set; }

        internal readonly int Id;

        public PicGalleryItem(BitmapSource pic, int id, bool selected = false)
        {
            InitializeComponent();

            if (pic != null)
            {
                img.Source = pic;
            }

            //Selected = selected;
            Id = id;

            outterborder.Width = outterborder.Height = picGalleryItem_Size;
            innerborder.Width = innerborder.Height = picGalleryItem_Size_s;

            img.MouseEnter += (s, y) => AnimationHelper.HoverSizeAnim(
                innerborder,
                false,
                picGalleryItem_Size_s,
                picGalleryItem_Size
            );

            img.MouseLeave += (s, y) => AnimationHelper.HoverSizeAnim(
                innerborder,
                true,
                picGalleryItem_Size,
                picGalleryItem_Size_s
            );

            if (selected)
            {
                innerborder.BorderBrush = new SolidColorBrush(AnimationHelper.GetPrefferedColorOverAlpha());
                innerborder.Width = innerborder.Height = picGalleryItem_Size;
            }
        }

//        private void SelectedStyle()
//        {
//            Selected = true;
//            innerborder.BorderBrush = new SolidColorBrush(AnimationHelper.GetPrefferedColorOverAlpha());
//            innerborder.Width = innerborder.Height = picGalleryItem_Size;
//        }

//        private void Unselect()
//        {
//            Selected = false;
//            innerborder.BorderBrush = new SolidColorBrush(Colors.Yellow);
//            innerborder.Width = innerborder.Height = picGalleryItem_Size_s;
//        }

//        internal void SetSelected(bool b)
//        {
//            if (b)
//            {
//                SelectedStyle();
//            }
//            else
//            {
//                Unselect();
//            }

//#if DEBUG
//            Trace.WriteLine(nameof(SetSelected) + " [" + FolderIndex + "] = " + b);
//#endif
//        }
    }
}
