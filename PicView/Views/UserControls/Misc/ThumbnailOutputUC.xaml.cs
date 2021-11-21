using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace PicView.Views.UserControls
{
    public partial class ThumbnailOutputUC : UserControl
    {
        public ThumbnailOutputUC(int i, string folderPath, string filename, string value)
        {
            InitializeComponent();

            OutPutString.Text = $"Thumbnail {i} destination";
            OutPutStringBox.Text = folderPath + @"\" + filename;
            ValueBox.Text = value;
         }
    }
}
