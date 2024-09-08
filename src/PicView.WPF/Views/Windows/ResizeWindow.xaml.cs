using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ImageMagick;
using PicView.Core.Localization;
using PicView.WPF.Animations;
using PicView.WPF.ChangeImage;
using PicView.WPF.ConfigureSettings;
using PicView.WPF.FileHandling;
using PicView.WPF.ImageHandling;
using PicView.WPF.Shortcuts;
using PicView.WPF.SystemIntegration;
using PicView.WPF.Views.UserControls.Misc;
using static PicView.WPF.UILogic.Sizing.WindowSizing;

namespace PicView.WPF.Views.Windows;

public partial class ResizeWindow
{
    private bool running;
    private readonly List<BatchFunctions.ThumbNailHolder> thumbs = new();

    public ResizeWindow()
    {
        MaxHeight = MonitorInfo.WorkArea.Height;
        Width *= MonitorInfo.DpiScaling;
        if (double.IsNaN(Width)) // Fixes if user opens window when loading from startup
        {
            MonitorInfo = MonitorSize.GetMonitorSize(this);
            MaxHeight = MonitorInfo.WorkArea.Height;
            Width *= MonitorInfo.DpiScaling;
        }

        InitializeComponent();

        ContentRendered += (_, _) =>
        {
            WindowBlur.EnableBlur(this);
            Owner = null; // Remove owner, so that minimizing main-window will not minimize this
            UpdateLanguage();
            Deactivated += (_, _) => ConfigColors.WindowUnfocusOrFocus(TitleBar, TitleText, null, false);
            Activated += (_, _) => ConfigColors.WindowUnfocusOrFocus(TitleBar, TitleText, null, true);
            IsVisibleChanged += (_, _) =>
            {
                Update();
                UpdateThumbnails();
            };
            Update();

            SourceFolderButton.FileMenuButton.Click += (_, _) =>
            {
                var newFolder = OpenSave.SelectAndReturnFolder();
                if (string.IsNullOrWhiteSpace(newFolder) == false)
                {
                    SourceFolderInput.Text = newFolder;
                }

                Focus();
            };

            OutputFolderButton.FileMenuButton.Click += (_, _) =>
            {
                var newFolder = OpenSave.SelectAndReturnFolder();
                if (string.IsNullOrWhiteSpace(newFolder) == false)
                {
                    OutputFolderInput.Text = newFolder;
                }

                Focus();
            };

            ThumbnailsComboBox.SelectionChanged += (_, _) => UpdateThumbnails();

            MouseLeftButtonDown += (_, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };

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

    internal void UpdateLanguage()
    {
        TitleText.Text = TranslationHelper.GetTranslation("BatchResize");
        Title = TranslationHelper.GetTranslation("BatchResize") + " - PicView";
        OutputFolderTextBlock.Text = TranslationHelper.GetTranslation("OutputFolder");
        ConvertToTextBlock.Text = TranslationHelper.GetTranslation("ConvertTo");
        SourceFolderTextBlock.Text = TranslationHelper.GetTranslation("SourceFolder");
        NoConversion.Content = TranslationHelper.GetTranslation("NoConversion");
        CompressionTextBlock.Text = TranslationHelper.GetTranslation("Compression");
        LosslessCompressionChoice.Content = TranslationHelper.GetTranslation("Lossless");
        LossyCompressionChoice.Content = TranslationHelper.GetTranslation("Lossy");
        QualityTextBlock.Text = TranslationHelper.GetTranslation("Quality");
        NoneChoice.Content = TranslationHelper.GetTranslation("None");
        ResizeTextBlock.Text = TranslationHelper.GetTranslation("Resize");
        NoResize.Content = TranslationHelper.GetTranslation("NoResize");
        WidthResize.Content = WidthTextBlock.Text = TranslationHelper.GetTranslation("Width");
        HeightResize.Content = HeightTextBlock.Text = TranslationHelper.GetTranslation("Height");
        PercentageResize.Content = PercentageTextBlock.Text = TranslationHelper.GetTranslation("Percentage");
        GenerateThumbnailsTextBlock.Text = TranslationHelper.GetTranslation("GenerateThumbnails");
        StartButton.Content = TranslationHelper.GetTranslation("Start");
        CancelButton.Content = TranslationHelper.GetTranslation("Cancel");
    }

    private void Update()
    {
        if (ErrorHandling.CheckOutOfRange() == false)
        {
            SourceFolderInput.Text = Path.GetDirectoryName(Navigation.Pics[Navigation.FolderIndex]);
            OutputFolderInput.Text = SourceFolderInput.Text + @"\Processed Pictures";
        }

        SetTextBoxDragEvent(SourceFolderInput);
        SetTextBoxDragEvent(OutputFolderInput);
    }

    private void UpdateThumbnails()
    {
        // TODO refactor and improve thumbnails output
        var selected = (ComboBoxItem)ThumbnailsComboBox.SelectedItem;
        if (!int.TryParse(selected?.Content.ToString(), out var count)) return;
        GeneratedThumbnailsContainer.Children.Clear();

        if (count <= 0)
        {
            return;
        }

        var size = new string[count + 1];
        var newSize = new string[size.Length];
        switch (count)
        {
            case 7:
                size[7] = "xxs";
                size[6] = "xs";
                size[5] = "small";
                size[4] = "medium";
                size[3] = "large";
                size[2] = "xl";
                size[1] = "xxl";
                newSize[7] = "20";
                newSize[6] = "30";
                newSize[5] = "40";
                newSize[4] = "50";
                newSize[3] = "60";
                newSize[2] = "70";
                newSize[1] = "80";
                break;

            case 6:
                size[6] = "xxs";
                size[5] = "xs";
                size[4] = "small";
                size[3] = "medium";
                size[2] = "large";
                size[1] = "xl";
                newSize[6] = "20";
                newSize[5] = "30";
                newSize[4] = "40";
                newSize[3] = "50";
                newSize[2] = "60";
                newSize[1] = "70";
                break;

            case 5:
                size[5] = "xs";
                size[4] = "small";
                size[3] = "medium";
                size[2] = "large";
                size[1] = "xl";
                newSize[5] = "20";
                newSize[4] = "30";
                newSize[3] = "50";
                newSize[2] = "60";
                newSize[1] = "70";
                break;

            case 4:
                size[4] = "xs";
                size[3] = "small";
                size[2] = "medium";
                size[1] = "large";
                newSize[4] = "25";
                newSize[3] = "40";
                newSize[2] = "50";
                newSize[1] = "70";
                break;

            case 3:
                size[3] = "small";
                size[2] = "medium";
                size[1] = "large";
                newSize[3] = "25";
                newSize[2] = "50";
                newSize[1] = "70";
                break;

            case 2:
                size[1] = "small";
                size[2] = "medium";
                newSize[1] = "30";
                newSize[2] = "50";
                break;

            default:
                size[1] = "small";
                newSize[1] = "30";
                break;
        }

        for (var i = 1; i <= count; i++)
        {
            GeneratedThumbnailsContainer.Children.Add(new ThumbnailOutputUC(i, OutputFolderInput.Text, size[i],
                newSize[i]));
        }
    }

    private async Task Load()
    {
        running = true;
        using var source = new CancellationTokenSource();
        var task = Task.Run(() => LoopAsync(source.Token), source.Token);
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
    }

    private async Task LoopAsync(CancellationToken cancellationToken)
    {
        running = true;

        var toResize = false;
        uint width = 0, height = 0;
        Percentage? percentage = null;

        uint quality = 100;
        bool? compress = null;

        string? ext = null;

        var sameDir = false;

        var cancelToken = new CancellationTokenSource();

        uint thumbW = 0, thumbH = 0;
        Percentage? thumbPercentage = null;

        List<string>? sourceFileist = null;
        var outputFolder = "";

        await Dispatcher.InvokeAsync(() =>
        {
            LogTextBox.Text = string.Empty;

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
            if (uint.TryParse(parseQ, out var q))
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

            if (toResize)
            {
                if (PercentageResize.IsSelected && int.TryParse(PercentageBox.Text, out var number))
                {
                    percentage = new Percentage(number);
                }
                else
                {
                    if (WidthResize.IsSelected && uint.TryParse(WidthValue.Text, out var resizeWidth))
                    {
                        width = resizeWidth;
                    }
                    else if (HeightResize.IsSelected && uint.TryParse(HeightValue.Text, out var resizeHeight))
                    {
                        height = resizeHeight;
                    }
                }
            }

            if (ErrorHandling.CheckOutOfRange() == false)
            {
                sameDir = Path.GetDirectoryName(Navigation.Pics[0]) ==
                          Path.GetDirectoryName(SourceFolderInput.Text);
            }

            sourceFileist = sameDir ? Navigation.Pics : FileLists.FileList(new FileInfo(SourceFolderInput.Text));
            outputFolder = OutputFolderInput.Text + @"\";

            for (var i = 0; i < GeneratedThumbnailsContainer.Children.Count; i++)
            {
                var container = (ThumbnailOutputUC)GeneratedThumbnailsContainer.Children[i];
                if (container == null)
                {
                    continue;
                }

                if (container.Percentage.IsSelected && int.TryParse(container.ValueBox.Text, out var number))
                {
                    thumbPercentage = new Percentage(number);
                    thumbW = 0;
                    thumbH = 0;
                }
                else
                {
                    if (container.WidthBox.IsSelected && uint.TryParse(container.ValueBox.Text, out var resizeWidth))
                    {
                        thumbW = resizeWidth;
                        thumbH = 0;
                    }
                    else if (container.HeightBox.IsSelected &&
                             uint.TryParse(container.ValueBox.Text, out var resizeHeight))
                    {
                        thumbW = 0;
                        thumbH = resizeHeight;
                    }

                    thumbPercentage = null;
                }

                thumbs.Add(new BatchFunctions.ThumbNailHolder(container.OutPutStringBox.Text, thumbW, thumbH,
                    thumbPercentage));
            }

            ProgressBar.Maximum = sourceFileist.Count;
            ProgressBar.Value = 0;
        }, DispatcherPriority.Normal, cancellationToken);

        try
        {
            await Parallel.ForEachAsync(sourceFileist, cancelToken.Token, async (sourceFile, token) =>
            {
                if (sourceFileist is null)
                {
                    return;
                }

                if (running is false)
                {
                    await cancelToken.CancelAsync();
                    if (sourceFileist is not null)
                    {
                        sourceFileist.Clear();
                        sourceFileist = null;
                    }

                    await Dispatcher.InvokeAsync(() => { ProgressBar.Value = 0; }, DispatcherPriority.Render,
                        token);
                    return;
                }

                var fileInfo = new FileInfo(sourceFile);
                ext ??= fileInfo.Extension;
                StringBuilder sb = new();
                sb.Append(await BatchFunctions.RunAsync(fileInfo, width, height, quality, ext, percentage, compress,
                    outputFolder, toResize).ConfigureAwait(false));

                foreach (var thumb in thumbs)
                {
                    if (running is false)
                    {
                        await cancelToken.CancelAsync();
                        if (sourceFileist is not null)
                        {
                            sourceFileist.Clear();
                            sourceFileist = null;
                        }

                        Dispatcher.Invoke(DispatcherPriority.Background, () => { ProgressBar.Value = 0; });
                        return;
                    }

                    sb.Append(await BatchFunctions.RunAsync(fileInfo, thumb.Width, thumb.Height, quality, ext,
                        thumb.Percentage, compress, thumb.Directory, true).ConfigureAwait(false));
                }

                if (cancelToken.IsCancellationRequested)
                {
                    if (sourceFileist is not null)
                    {
                        sourceFileist.Clear();
                        sourceFileist = null;
                    }

                    await Dispatcher.InvokeAsync(() => { ProgressBar.Value = 0; }, DispatcherPriority.Render,
                        token);
                    return;
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    LogTextBox.Text += sb.ToString();
                    LogTextBox.ScrollToEnd();
                    ProgressBar.Value++;
                }, DispatcherPriority.DataBind, token);
            }).ConfigureAwait(false);
        }
        catch (Exception)
        {
            sourceFileist?.Clear();
            await Dispatcher.InvokeAsync(() => { ProgressBar.Value = 0; }, DispatcherPriority.Render,
                cancellationToken);
        }
    }

    internal static void SetTextBoxDragEvent(TextBox textBox)
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