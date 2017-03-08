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

namespace PicView.lib.UserControls.CustomControls
{
    /// <summary>
    /// Interaction logic for PicGallery.xaml
    /// </summary>
    public partial class PicGallery : UserControl
    {
        public PicGallery()
        {
            InitializeComponent();
        }

        internal void Add(string file)
        {
            var item = new PicGalleryItem(file);
            Container.Children.Add(item);
        }

        internal void Add(string[] files)
        {
            var index = files.Length / 2;
            for (int i = index; i < files.Length; i++)
            {
                Add(files[i]);
                for (int x = index - 1; x >= 0; x--)
                {
                    Add(files[x]);
                }
            }

        }

        internal void Remove(string file)
        {

        }

        internal void Remove(string[] files)
        {

        }

        internal void Clear()
        {

        }


    }
}
