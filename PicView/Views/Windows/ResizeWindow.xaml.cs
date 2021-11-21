using PicView.ChangeImage;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace PicView.Views.Windows
{
    public partial class ResizeWindow : Window
    {
        public ResizeWindow()
        {
            Title = Application.Current.Resources["BatchResize"] + " - PicView";
            MaxHeight = WindowSizing.MonitorInfo.WorkArea.Height;
            Width *= WindowSizing.MonitorInfo.DpiScaling;
            if (double.IsNaN(Width)) // Fixes if user opens window when loading from startup
            {
                WindowSizing.MonitorInfo = SystemIntegration.MonitorSize.GetMonitorSize();
                MaxHeight = WindowSizing.MonitorInfo.WorkArea.Height;
                Width *= WindowSizing.MonitorInfo.DpiScaling;
            }

            InitializeComponent();

            ContentRendered += (sender, e) =>
            {
                if (Error_Handling.CheckOutOfRange() == false)
                {
                    SourceFolderInput.Text = Path.GetDirectoryName(Navigation.Pics[Navigation.FolderIndex]);
                    OutputFolderInput.Text = SourceFolderInput.Text + @"\Processed Pictures";
                }

                //GenerateThumbnailsCB.SelectionChanged += delegate
                //{

                //};

                for (int i = 1; i <= 5; i++)
                {
                    GeneratedThumbnailsContainer.Children.Add(new UserControls.ThumbnailOutputUC(i, OutputFolderInput.Text, "xl"));
                }

                


                MouseLeftButtonDown += (_, e) =>
                { if (e.LeftButton == MouseButtonState.Pressed) { DragMove(); } };

                KeyDown += (_, e) => Shortcuts.GenericWindowShortcuts.KeysDown(null, e, this);

                // CloseButton
                CloseButton.TheButton.Click += delegate { Hide(); };

                // MinButton
                MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

                TitleBar.MouseLeftButtonDown += delegate { DragMove(); };
            };
        }
    }
}
