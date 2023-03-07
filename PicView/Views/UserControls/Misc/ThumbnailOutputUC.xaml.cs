using PicView.FileHandling;
using PicView.UILogic;
using PicView.Views.Windows;
using System.Windows.Controls;

namespace PicView.Views.UserControls.Misc
{
    public partial class ThumbnailOutputUC : UserControl
    {
        public ThumbnailOutputUC(int i, string folderPath, string filename, string value)
        {
            InitializeComponent();

            OutPutString.Text = $"Thumb {i}";
            OutPutStringBox.Text = folderPath + @"\" + filename;
            ValueBox.Text = value;

            OutputFolderButton.FileMenuButton.Click += (_, _) =>
            {
                var newFolder = Open_Save.SelectAndReturnFolder();
                if (string.IsNullOrWhiteSpace(newFolder) == false)
                {
                    OutPutStringBox.Text = newFolder;
                }
                ConfigureWindows.GetResizeWindow.Focus();
            };

            ResizeWindow.SetTextboxDragEvent(OutPutStringBox);
        }
    }
}