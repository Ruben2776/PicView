using PicView.Animations;
using PicView.ChangeImage;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
                Owner = null; // Remove owner, so that minizing mainwindow will not minize this

                if (Error_Handling.CheckOutOfRange() == false)
                {
                    SourceFolderInput.Text = Path.GetDirectoryName(Navigation.Pics[Navigation.FolderIndex]);
                    OutputFolderInput.Text = SourceFolderInput.Text + @"\Processed Pictures";
                }

                SetTextboxDragEvent(SourceFolderInput);
                SetTextboxDragEvent(OutputFolderInput);

                SourceFolderButton.FileMenuButton.Click += (_, _) =>
                {
                    var newFolder = FileHandling.Open_Save.SelectAndReturnFolder();
                    if (string.IsNullOrWhiteSpace(newFolder) == false)
                    {
                        SourceFolderInput.Text = newFolder;
                    }
                };

                OutputFolderButton.FileMenuButton.Click += (_, _) =>
                {
                    var newFolder = FileHandling.Open_Save.SelectAndReturnFolder();
                    if (string.IsNullOrWhiteSpace(newFolder) == false)
                    {
                        OutputFolderInput.Text = newFolder;
                    }
                };

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

                            default:
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

                StartButton.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(StartText); };
                StartButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(StartBrush); };
                StartButton.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(StartText); };
                StartButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(StartBrush); };

                StartButton.MouseLeftButtonDown += async delegate
                {
                    bool toResize = NoResize.IsSelected == false;
                    double ResizeAmount = 0;
                    ImageMagick.Percentage? percentage = null;

                    if (toResize)
                    {
                        if (PercentageResize.IsSelected && int.TryParse(PercentageBox.Text, out var number))
                        {
                            percentage = new ImageMagick.Percentage(number);
                        }
                        else
                        {
                            if (WidthResize.IsSelected && int.TryParse(WidthResize.Content.ToString(), out var resizeWidth))
                            {
                                ResizeAmount = resizeWidth;
                            }
                            else if (HeightResize.IsSelected && int.TryParse(HeightResize.Content.ToString(), out var resizeHeight))
                            {
                                ResizeAmount = resizeHeight;
                            }
                        }
                    }

                    bool sameDir = false;
                    if (Error_Handling.CheckOutOfRange() == false)
                    {
                        sameDir = Path.GetDirectoryName(Navigation.Pics[0]) == Path.GetDirectoryName(SourceFolderInput.Text);
                    }

                    var sourceFileist = sameDir ? Navigation.Pics : FileHandling.FileLists.FileList(new FileInfo(SourceFolderInput.Text));

                    await Task.Run(() =>
                    {
                        Parallel.For(0, sourceFileist.Count, i =>
                        {
                            if (toResize)
                            {

                            }
                        });
                    });

                };

                CencelButton.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(CencelText); };
                CencelButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(CancelBrush); };
                CencelButton.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(CencelText); };
                CencelButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(CancelBrush); };
            };
        }

        internal static void SetTextboxDragEvent(TextBox textBox)
        {
            textBox.PreviewDragOver += (_, e) =>
            {
                e.Handled = true; // Needs this to allow drag to work

                textBox.Background = (SolidColorBrush)Application.Current.Resources["BackgroundHoverHighlightBrush"];
            };

            textBox.PreviewDragLeave += (_, _) =>
            {
                textBox.Background = (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushAlt"];
            };

            textBox.Drop += (_, e) =>
            {
                if (e.Data.GetData(DataFormats.FileDrop, true) is not string[] files)
                {
                    var data = e.Data.GetData(DataFormats.Text);

                    if (data != null)
                    {
                        var text = data.ToString();
                        if (Directory.Exists(text))
                        {
                            textBox.Text = text;
                        }
                    }
                }
                else if (Directory.Exists(files[0]))
                {
                    textBox.Text = files[0];
                }

                textBox.Background = (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushAlt"];
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
