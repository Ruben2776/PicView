using System.Windows.Controls;

namespace PicView.Views.UserControls
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
                var newFolder = FileHandling.Open_Save.SelectAndReturnFolder();
                if (string.IsNullOrWhiteSpace(newFolder) == false)
                {
                    OutPutStringBox.Text = newFolder;
                }
            };
         }
    }
}
