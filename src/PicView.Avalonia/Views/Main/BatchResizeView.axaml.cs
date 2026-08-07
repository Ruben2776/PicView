using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using ImageMagick;
using PicView.Core.BatchResize;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.Main;

public partial class BatchResizeView : UserControl
{
    private readonly CompositeDisposable _disposables = new();
    private bool _suppressAspectRatioUpdate;
    private double _aspectRatio;

    public BatchResizeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        InitializeData();
        SubscribeToEvents();
        if (!Settings.Theme.Dark || Settings.Theme.GlassTheme)
        {
            ColorThemeAdjustments();
        }
    }

    private void ColorThemeAdjustments()
    {
        if (!Settings.Theme.Dark && !Settings.Theme.GlassTheme)
        {
            if (!Application.Current.TryGetResource("MenuBackgroundColor",
                    Application.Current.RequestedThemeVariant, out var menuBackgroundColor))
            {
                return;
            }

            if (menuBackgroundColor is not Color color)
            {
                return;
            }

            Background = new SolidColorBrush(color);
            var lightColor = new SolidColorBrush(Color.Parse("#F0FFFFFF"));
            FileLogHeaderBorder.Background = FilePanelLogBorder.Background = lightColor;
        }
        else
        {
            AddFileButton.BorderThickness = AddFolderButton.BorderThickness = new Thickness(0);

            if (!Application.Current.TryGetResource("SoftenColorBrush",
                    Application.Current.RequestedThemeVariant, out var menuBackgroundColor))
            {
                return;
            }

            if (menuBackgroundColor is not SolidColorBrush color)
            {
                return;
            }

            FileLogHeaderBorder.Background = FilePanelLogBorder.Background = color;
        }

        AddFileButton.Classes.Remove("altHover");
        AddFileButton.Classes.Add("hover");

        AddFolderButton.Classes.Remove("altHover");
        AddFolderButton.Classes.Add("hover");
    }

    private void SubscribeToEvents()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        var batch = core.BatchResize;
        
        CancelButton.Click += delegate
        {
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                window.Close();
            }
        };
        Observable.EveryValueChanged(this, x => x.ConversionComboBox.SelectedIndex)
            .Subscribe(x =>
            {
                batch.Conversion.Value = (ConversionTarget)x;
            }).AddTo(_disposables);
        Observable.EveryValueChanged(this, x => x.ThumbnailsComboBox.SelectedIndex)
            .Subscribe(x =>
            {
                batch.ThumbnailAmount = x;
                for (var i = 0; i < x; i++)
                {
                    SetThumbValues(i);
                }
            }).AddTo(_disposables);
        Observable.EveryValueChanged(batch.IsKeepingAspectRatio, x => x.Value)
            .Subscribe(x =>
            {
                if (x)
                {
                    LinkChainImage.IsVisible = true;
                    UnlinkChainImage.IsVisible = false;

                    var w = batch.WidthValue.CurrentValue;
                    var h = batch.HeightValue.CurrentValue;
                    if (w > 0 && h > 0)
                    {
                        _aspectRatio = (double)w / h;
                    }
                }
                else
                {
                    LinkChainImage.IsVisible = false;
                    UnlinkChainImage.IsVisible = true;
                }
            }).AddTo(_disposables);

        for (var i = 0; i < 7; i++)
        {
            var oneBased = i + 1;
            var (percentageItem, widthItem, heightItem, valueBox, outputBox, comboBox) =
                GetThumbControls(oneBased);
            comboBox.SelectionChanged += delegate
            {
                var (thumbIsPercentageResized, thumbIsWidthResized, thumbIsHeightResized, saveDestination) =
                    GetUserInputtedOptions(percentageItem, widthItem, heightItem, outputBox, comboBox);

                // Parse the value from the TextBox
                if (!uint.TryParse(valueBox?.Text, out var thumbValue))
                {
                    return;
                }

                if (thumbIsPercentageResized)
                {
                    batch.Thumbs[oneBased] =
                        new BatchThumb(saveDestination, new Percentage(thumbValue));
                }

                if (thumbIsWidthResized)
                {
                    batch.Thumbs[oneBased] = new BatchThumb(saveDestination, width: thumbValue);
                }

                if (thumbIsHeightResized)
                {
                    batch.Thumbs[oneBased] = new BatchThumb(saveDestination, height: thumbValue);
                }
            };
        }

    }

    private void SetThumbValues(int i)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        var oneBased = i;
        i++;

        var (percentageItem, widthItem, heightItem, valueBox, outputBox, comboBox) =
            GetThumbControls(i);

        UpdateThumbValues();
        valueBox.TextChanged += delegate
        {
            UpdateThumbValues();
        };
        
        return;

        void UpdateThumbValues()
        {
            var (thumbIsPercentageResized, thumbIsWidthResized, thumbIsHeightResized, saveDestination) =
                GetUserInputtedOptions(percentageItem, widthItem, heightItem, outputBox, comboBox);

            // Parse the value from the TextBox
            if (!uint.TryParse(valueBox?.Text, out var thumbValue))
            {
                return;
            }

            if (thumbIsPercentageResized)
            {
                core.BatchResize.Thumbs[oneBased] = new BatchThumb(saveDestination, new Percentage(thumbValue));
            }

            if (thumbIsWidthResized)
            {
                core.BatchResize.Thumbs[oneBased] = new BatchThumb(saveDestination, width: thumbValue);
            }

            if (thumbIsHeightResized)
            {
                core.BatchResize.Thumbs[oneBased] = new BatchThumb(saveDestination, height: thumbValue);
            }
        }
    }

    private (ComboBoxItem? percentageItem, ComboBoxItem? widthItem, ComboBoxItem? heightItem, TextBox? valueBox, TextBox
        ? outputBox, ComboBox? comboBox)
        GetThumbControls(int i)
    {
        // Dynamically construct the control names
        var percentageItemName = $"Thumb{i}PercentageItem";
        var widthItemName = $"Thumb{i}WidthItem";
        var heightItemName = $"Thumb{i}HeightItem";
        var valueBoxName = $"Thumb{i}ValueBox";
        var outputBoxName = $"Thumb{i}OutputBox";
        var comboBoxName = $"Thumb{i}ComboBox";

        // Find controls based on their names
        var percentageItem = this.FindControl<ComboBoxItem>(percentageItemName);
        var widthItem = this.FindControl<ComboBoxItem>(widthItemName);
        var heightItem = this.FindControl<ComboBoxItem>(heightItemName);
        var valueBox = this.FindControl<TextBox>(valueBoxName);
        var outputBox = this.FindControl<TextBox>(outputBoxName);
        var comboBox = this.FindControl<ComboBox>(comboBoxName);

        return (percentageItem, widthItem, heightItem, valueBox, outputBox, comboBox);
    }

    private static (
        bool thumbIsPercentageResized,
        bool thumbIsWidthResized,
        bool thumbIsHeightResized,
        string? saveDestination)
        GetUserInputtedOptions(ComboBoxItem? percentageItem,
            ComboBoxItem? widthItem,
            ComboBoxItem? heightItem,
            TextBox outputBox,
            ComboBox comboBox)
    {
        // Check which resizing option is selected
        var thumbIsPercentageResized = ReferenceEquals(comboBox.SelectedItem, percentageItem);
        var thumbIsWidthResized = ReferenceEquals(comboBox.SelectedItem, widthItem);
        var thumbIsHeightResized = ReferenceEquals(comboBox.SelectedItem, heightItem);
        var saveDestination = outputBox.Text;

        return (thumbIsPercentageResized, thumbIsWidthResized, thumbIsHeightResized, saveDestination);
    }

    private void InitializeData()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        var batch = core.BatchResize;
        var tab = core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue;
        var collection = tab.ImageIterator?.Files;
        var width = tab.Model.PixelWidth;
        var height = tab.Model.PixelHeight;
        batch.SelectedFiles.Value = collection is null ? [] : new ObservableCollection<FileInfo>(collection);

        if (!string.IsNullOrWhiteSpace(tab.FileInfo?.CurrentValue?.DirectoryName))
        {
            batch.OutputFolder.Value = Path.Combine(
                tab.FileInfo?.CurrentValue.DirectoryName,
                TranslationManager.Translation.BatchResize);
        }

        batch.SingleWidthValue.Value = batch.WidthValue.Value = width;
        batch.SingleHeightValue.Value = batch.HeightValue.Value = height;

        var config = batch.Config;
        CompressionComboBox.SelectedIndex = config.WindowProperties.CompressionIndex;
        IsQualityEnabledBox.IsChecked = config.WindowProperties.IsQualityEnabled;
        ConversionComboBox.SelectedIndex = config.WindowProperties.ConvertToIndex;
        ResizeComboBox.SelectedIndex = config.WindowProperties.ResizeIndex;
        ThumbnailsComboBox.SelectedIndex = config.WindowProperties.GenerateThumbnailsIndex;
    }

    private void IsQualityEnabledBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || !IsQualityEnabledBox.IsChecked.HasValue)
        {
            return;
        }

        var isEnabled = IsQualityEnabledBox.IsChecked.Value;
        var batch = core.BatchResize;
        batch.Config.WindowProperties.IsQualityEnabled = isEnabled;
        batch.IsQualityEnabled.Value = isEnabled;
    }

    private void ResizeComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        var batch = core.BatchResize;

        batch.IsPercentageResizing.Value = false;
        batch.IsWidthAndHeightResizing.Value = false;
        batch.IsWidthResizing.Value = false;
        batch.IsHeightResizing.Value = false;

        switch (ResizeComboBox.SelectedIndex)
        {
            case (int)ResizeMode.None:
                batch.Resize.Value = ResizeMode.None;
                break;
            case (int)ResizeMode.Height:
                batch.Resize.Value = ResizeMode.Height;
                batch.IsHeightResizing.Value = true;
                break;
            case (int)ResizeMode.Percentage:
                batch.Resize.Value = ResizeMode.Percentage;
                batch.IsPercentageResizing.Value = true;
                break;
            case (int)ResizeMode.Width:
                batch.Resize.Value = ResizeMode.Width;
                batch.IsWidthResizing.Value = true;
                break;
            case (int)ResizeMode.WidthAndHeight:
                batch.Resize.Value = ResizeMode.WidthAndHeight;
                batch.IsWidthAndHeightResizing.Value = true;
                break;
            default:
                batch.Resize.Value = batch.Resize.Value;
                break;
        }
    }

    private void ShowInFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        core.PlatformService.LocateOnDisk(core.BatchResize.OutputFolder.CurrentValue);
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        _disposables.Dispose();
    }

    private void WidthBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressAspectRatioUpdate)
        {
            return;
        }
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        var batch = core.BatchResize;
        if (!batch.IsKeepingAspectRatio.Value || _aspectRatio <= 0)
        {
            return;
        }
        if (!uint.TryParse(WidthBox.Text, out var width) || width == 0)
        {
            return;
        }

        var newHeight = (uint)Math.Max(1, Math.Round(width / _aspectRatio));
        _suppressAspectRatioUpdate = true;
        try
        {
            batch.HeightValue.Value = newHeight;
        }
        finally
        {
            _suppressAspectRatioUpdate = false;
        }
    }

    private void HeightBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressAspectRatioUpdate)
        {
            return;
        }
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        var batch = core.BatchResize;
        if (!batch.IsKeepingAspectRatio.Value || _aspectRatio <= 0)
        {
            return;
        }
        if (!uint.TryParse(HeightBox.Text, out var height) || height == 0)
        {
            return;
        }

        var newWidth = (uint)Math.Max(1, Math.Round(height * _aspectRatio));
        _suppressAspectRatioUpdate = true;
        try
        {
            batch.WidthValue.Value = newWidth;
        }
        finally
        {
            _suppressAspectRatioUpdate = false;
        }
    }
}