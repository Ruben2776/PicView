using PicView.ChangeImage;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static PicView.UILogic.Sizing.WindowSizing;

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

                ThumbnailsComboBox.SelectionChanged += delegate
                {
                    var selected = (ComboBoxItem)ThumbnailsComboBox.SelectedItem;
                    if (int.TryParse(selected?.Content.ToString(), out var count)) 
                    {
                        GeneratedThumbnailsContainer.Children.Clear();

                        if (count <= 0) { return; }

                        var size = new string[count + 1];
                        var newSize = new string[size.Length]; 
                        switch (count)
                        {
                            case 7: 
                                size[7] = "xxs"; size[6] = "xs"; size[5] = "small"; size[4] = "medium"; size[3] = "large"; size[2] = "xl"; size[1] = "xxl";
                                newSize[7] = "20"; newSize[6] = "30"; newSize[5] = "40"; newSize[4] = "50"; newSize[3] = "60"; newSize[2] = "70"; newSize[1] = "80"; 
                                break;

                            case 6: 
                                size[6] = "xxs"; size[5] = "xs"; size[4] = "small"; size[3] = "medium"; size[2] = "large"; size[1] = "xl";
                                newSize[6] = "20"; newSize[5] = "30"; newSize[4] = "40"; newSize[3] = "50"; newSize[2] = "60"; newSize[1] = "70";
                                break;

                            case 5: 
                                size[5] = "xs"; size[4] = "small"; size[3] = "medium"; size[2] = "large"; size[1] = "xl";
                                newSize[5] = "20"; newSize[4] = "30"; newSize[3] = "50"; newSize[2] = "60"; newSize[1] = "70";
                                break;

                            case 4: 
                                size[4] = "xs"; size[3] = "small"; size[2] = "medium"; size[1] = "large";
                                newSize[4] = "25"; newSize[3] = "40"; newSize[2] = "50"; newSize[1] = "70";
                                break;

                            case 3: 
                                size[3] = "small"; size[2] = "medium"; size[1] = "large";
                                newSize[3] = "25"; newSize[2] = "50"; newSize[1] = "70";
                                break;

                            case 2: 
                                size[1] = "small"; size[2] = "medium";
                                newSize[1] = "30"; newSize[2] = "50";
                                break;

                            case 1: default: 
                                size[1] = "small";
                                newSize[1] = "30";
                                break;
                        }

                        for (int i = 1; i <= count; i++)
                        {
                            GeneratedThumbnailsContainer.Children.Add(new UserControls.ThumbnailOutputUC(i, OutputFolderInput.Text, size[i], newSize[i]));
                        }
                    }
                };

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

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (sizeInfo == null || !sizeInfo.WidthChanged && !sizeInfo.HeightChanged)
            {
                return;
            }

            //Keep position when size has changed
            Top += ((sizeInfo.PreviousSize.Height / MonitorInfo.DpiScaling) - (sizeInfo.NewSize.Height / MonitorInfo.DpiScaling)) / 2;
            Left += ((sizeInfo.PreviousSize.Width / MonitorInfo.DpiScaling) - (sizeInfo.NewSize.Width / MonitorInfo.DpiScaling)) / 2;

            base.OnRenderSizeChanged(sizeInfo);
        }
    }
}
