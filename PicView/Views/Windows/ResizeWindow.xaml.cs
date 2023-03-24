using ImageMagick;
using PicView.Animations;
using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.Shortcuts;
using PicView.SystemIntegration;
using PicView.UILogic;
using PicView.Views.UserControls.Misc;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static PicView.UILogic.Sizing.WindowSizing;

namespace PicView.Views.Windows
{
    public partial class ResizeWindow : Window
    {
        private bool running;
        private readonly List<BatchFunctions.ThumbNailHolder> thumbs = new();

        public ResizeWindow()
        {
            Title = Application.Current.Resources["BatchResize"] + " - PicView";
            MaxHeight = MonitorInfo.WorkArea.Height;
            Width *= MonitorInfo.DpiScaling;
            if (double.IsNaN(Width)) // Fixes if user opens window when loading from startup
            {
                MonitorInfo = MonitorSize.GetMonitorSize();
                MaxHeight = MonitorInfo.WorkArea.Height;
                Width *= MonitorInfo.DpiScaling;
            }

            InitializeComponent();

            ContentRendered += (sender, e) =>
            {
                WindowBlur.EnableBlur(this);
                Owner = null; // Remove owner, so that minizing mainwindow will not minize this

                if (ErrorHandling.CheckOutOfRange() == false)
                {
                    SourceFolderInput.Text = Path.GetDirectoryName(Navigation.Pics[Navigation.FolderIndex]);
                    OutputFolderInput.Text = SourceFolderInput.Text + @"\Processed Pictures";
                }

                SetTextboxDragEvent(SourceFolderInput);
                SetTextboxDragEvent(OutputFolderInput);

                SourceFolderButton.FileMenuButton.Click += (_, _) =>
                {
                    var newFolder = Open_Save.SelectAndReturnFolder();
                    if (string.IsNullOrWhiteSpace(newFolder) == false)
                    {
                        SourceFolderInput.Text = newFolder;
                    }
                    Focus();
                };

                OutputFolderButton.FileMenuButton.Click += (_, _) =>
                {
                    var newFolder = Open_Save.SelectAndReturnFolder();
                    if (string.IsNullOrWhiteSpace(newFolder) == false)
                    {
                        OutputFolderInput.Text = newFolder;
                    }
                    Focus();
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
                            GeneratedThumbnailsContainer.Children.Add(new ThumbnailOutputUC(i, OutputFolderInput.Text, size[i], newSize[i]));
                        }
                    }
                };

                MouseLeftButtonDown += (_, e) =>
                { if (e.LeftButton == MouseButtonState.Pressed) { DragMove(); } };

                KeyDown += (_, e) => GenericWindowShortcuts.KeysDown(null, e, this);

                // CloseButton
                CloseButton.TheButton.Click += delegate { Hide(); };

                // MinButton
                MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

                TitleBar.MouseLeftButtonDown += delegate { DragMove(); };

                StartButton.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(StartText); };
                StartButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(StartBrush); };
                StartButton.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(StartText); };
                StartButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(StartBrush); };

                StartButton.MouseLeftButtonDown += async (_, _) => await Load().ConfigureAwait(false);

                CancelButton.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(CancelText); };
                CancelButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(CancelBrush); };
                CancelButton.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(CancelText); };
                CancelButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(CancelBrush); };

                CancelButton.MouseLeftButtonDown += (_, _) => running = false;
            };
        }

        private async Task Load()
        {
            running = true;
            CancellationTokenSource source = new CancellationTokenSource();
            Task task = Task.Run(() => LoopAsync(source.Token), source.Token);
            try
            {
                await task.ConfigureAwait(false);
                running = false;
            }
            catch (TaskCanceledException)
            {
                Dispatcher.Invoke(DispatcherPriority.ContextIdle, () =>
                {
                    LogTextBox.Text = string.Empty;
                    ProgressBar.Value = 0;
                });
            }
            finally { source.Dispose(); }
        }

        private async Task LoopAsync(CancellationToken cancellationToken)
        {
            running = true;

            bool toResize = false;
            int width = 0, height = 0;
            Percentage? percentage = null;

            int quality = 100;
            bool? compress = null;

            string? ext = null;

            bool sameDir = false;

            var cancelToken = new CancellationTokenSource();

            int thumbW = 0, thumbH = 0;
            Percentage? thumbPercentage = null;

            List<string>? sourceFileist = null;
            string outputFolder = "";

            await ConfigureWindows.GetResizeWindow.Dispatcher.InvokeAsync(() =>
            {
                LogTextBox.Text = String.Empty;

                toResize = NoResize.IsSelected == false;

                if (LosslessCompressionChoice.IsSelected)
                {
                    compress = true;
                }
                else if (LossyCompressionChoice.IsSelected)
                {
                    compress = false;
                }

                var selectedQ = QualityPercentage.SelectedItem as ComboBoxItem;
                var parseQ = selectedQ.Content.ToString();
                parseQ = parseQ.Remove(parseQ.Length - 1);
                if (int.TryParse(parseQ, out var q))
                {
                    quality = q;
                }

                if (webp.IsSelected)
                {
                    ext = ".webp";
                }
                else if (png.IsSelected)
                {
                    ext = ".png";
                }
                else if (jpg.IsSelected)
                {
                    ext = ".jpg";
                }
                else
                {
                    ext = null;
                }

                if (toResize)
                {
                    if (PercentageResize.IsSelected && int.TryParse(PercentageBox.Text, out var number))
                    {
                        percentage = new Percentage(number);
                    }
                    else
                    {
                        if (WidthResize.IsSelected && int.TryParse(WidthValue.Text, out var resizeWidth))
                        {
                            width = resizeWidth;
                        }
                        else if (HeightResize.IsSelected && int.TryParse(HeightValue.Text, out var resizeHeight))
                        {
                            height = resizeHeight;
                        }
                    }
                }

                if (ErrorHandling.CheckOutOfRange() == false)
                {
                    sameDir = Path.GetDirectoryName(Navigation.Pics[0]) == Path.GetDirectoryName(SourceFolderInput.Text);
                }

                sourceFileist = sameDir ? Navigation.Pics : FileLists.FileList(new FileInfo(SourceFolderInput.Text));
                outputFolder = OutputFolderInput.Text + @"\";

                for (int i = 0; i < GeneratedThumbnailsContainer.Children.Count; i++)
                {
                    var container = (ThumbnailOutputUC)GeneratedThumbnailsContainer.Children[i];
                    if (container == null) { continue; }
                    if (container.Percentage.IsSelected && int.TryParse(container.ValueBox.Text, out var number))
                    {
                        thumbPercentage = new Percentage(number);
                        thumbW = 0;
                        thumbH = 0;
                    }
                    else
                    {
                        if (container.WidthBox.IsSelected && int.TryParse(container.ValueBox.Text, out var resizeWidth))
                        {
                            thumbW = resizeWidth;
                            thumbH = 0;
                        }
                        else if (container.HeightBox.IsSelected && int.TryParse(container.ValueBox.Text, out var resizeHeight))
                        {
                            thumbW = 0;
                            thumbH = resizeHeight;
                        }
                        thumbPercentage = null;
                    }
                    thumbs.Add(new BatchFunctions.ThumbNailHolder(container.OutPutStringBox.Text, thumbW, thumbH, thumbPercentage));
                }

                ProgressBar.Maximum = sourceFileist.Count;
            }, DispatcherPriority.Normal, cancellationToken);

            for (int i = 0; i < sourceFileist.Count; i++)
            {
                if (sourceFileist is null)
                {
                    return;
                }

                if (running is false)
                {
                    cancelToken.Cancel();
                    if (sourceFileist is not null)
                    {
                        sourceFileist.Clear();
                        sourceFileist = null;
                    }
                    Dispatcher.Invoke(DispatcherPriority.Background, () =>
                    {
                        ProgressBar.Value = 0;
                    });
                    return;
                }

                FileInfo fileInfo;
                lock (sourceFileist)
                {
                    fileInfo = new FileInfo(sourceFileist[i]);
                }
                StringBuilder sb = new();
                sb.Append(await BatchFunctions.RunAsync(fileInfo, width, height, quality, ext, percentage, compress, outputFolder, toResize).ConfigureAwait(false));

                for (int x = 0; x < thumbs.Count; x++)
                {
                    if (running is false)
                    {
                        cancelToken.Cancel();
                        if (sourceFileist is not null)
                        {
                            sourceFileist.Clear();
                            sourceFileist = null;
                        }
                        Dispatcher.Invoke(DispatcherPriority.Background, () =>
                        {
                            ProgressBar.Value = 0;
                        });
                        return;
                    }

                    sb.Append(await BatchFunctions.RunAsync(fileInfo, thumbs[x].Width, thumbs[x].Height, quality, ext, thumbs[x].Percentage, compress, thumbs[x].Directory, true).ConfigureAwait(false));
                }

                if (cancelToken.IsCancellationRequested)
                {
                    if (sourceFileist is not null)
                    {
                        sourceFileist.Clear();
                        sourceFileist = null;
                    }
                    Dispatcher.Invoke(DispatcherPriority.Background, () =>
                    {
                        ProgressBar.Value = 0;
                    });
                    return;
                }

                Dispatcher.Invoke(DispatcherPriority.Background, () =>
                {
                    LogTextBox.Text += sb.ToString();
                    LogTextBox.ScrollToEnd();
                    ProgressBar.Value++;
                });
            }
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
    }
}