using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using ImageMagick;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.Main;

public partial class BatchResizeView : UserControl
{
    private readonly CompositeDisposable _disposables = new();
    public BatchResizeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        SubscribeToEvents(vm);
        InitializeNavigationData(vm);
        if (!Settings.Theme.Dark)
        {
            LightThemeAdjustments();
        }
    }

    private void LightThemeAdjustments()
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
        FolderHeaderBorder.Background = FilePanelLogBorder.Background = lightColor;

        AddFileButton.Classes.Remove("altHover");
        AddFileButton.Classes.Add("hover");

        AddFolderButton.Classes.Remove("altHover");
        AddFolderButton.Classes.Add("hover");
    }

    private void SubscribeToEvents(MainViewModel vm)
    {
        CancelButton.Click += delegate { (VisualRoot as Window)?.Close(); };
        Observable.EveryValueChanged(this, x => x.ConversionComboBox.SelectedIndex)
            .Subscribe(x =>
            {
                vm.BatchResizeViewModel.Conversion.Value = (ConversionTarget)x;
            }).AddTo(_disposables);
        Observable.EveryValueChanged(this, x => x.ThumbnailsComboBox.SelectedIndex)
            .Subscribe(x =>
            {
                vm.BatchResizeViewModel.ThumbnailAmount = x;
                for (var i = 0; i < x; i++)
                {
                    SetThumbValues(i);
                }
            }).AddTo(_disposables);
        Observable.EveryValueChanged(vm.BatchResizeViewModel.IsKeepingAspectRatio, x => x.Value)
            .Subscribe(x =>
            {
                if (x)
                {
                    LinkChainImage.IsVisible = true;
                    UnlinkChainImage.IsVisible = false;
                }
                else
                {
                    LinkChainImage.IsVisible = false;
                    UnlinkChainImage.IsVisible = true;
                }
            }).AddTo(_disposables);
    }

    private void SetThumbValues(int i)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        var x = i;
        i++;

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

        // Check which resizing option is selected
        var thumbIsPercentageResized = ReferenceEquals(comboBox.SelectedItem, percentageItem);
        var thumbIsWidthResized = ReferenceEquals(comboBox.SelectedItem, widthItem);
        var thumbIsHeightResized = ReferenceEquals(comboBox.SelectedItem, heightItem);
        var saveDestination = outputBox.Text;

        // Parse the value from the TextBox
        if (uint.TryParse(valueBox?.Text, out var thumbValue))
        {
            if (thumbIsPercentageResized)
            {
                vm.BatchResizeViewModel.Thumbs[x] = new BatchThumb(saveDestination, new Percentage(thumbValue));
            }

            if (thumbIsWidthResized)
            {
                vm.BatchResizeViewModel.Thumbs[x] = new BatchThumb(saveDestination, width: thumbValue);
            }

            if (thumbIsHeightResized)
            {
                vm.BatchResizeViewModel.Thumbs[x] = new BatchThumb(saveDestination, height: thumbValue);
            }
        }
    }

    private static void InitializeNavigationData(MainViewModel vm)
    {
        if (!NavigationManager.CanNavigate(vm))
        {
            return;
        }

        vm.BatchResizeViewModel.SelectedFiles.Value =
            new ObservableCollection<FileInfo>(NavigationManager.GetCollection);

        if (!string.IsNullOrWhiteSpace(vm.PicViewer.FileInfo?.CurrentValue.DirectoryName))
        {
            vm.BatchResizeViewModel.OutputFolder.Value = Path.Combine(
                vm.PicViewer.FileInfo?.CurrentValue.DirectoryName,
                TranslationManager.Translation.BatchResize);
        }

        vm.BatchResizeViewModel.SingleWidthValue.Value =
            vm.BatchResizeViewModel.WidthValue.Value = (uint)vm.PicViewer.PixelWidth.CurrentValue;
        vm.BatchResizeViewModel.SingleHeightValue.Value =
            vm.BatchResizeViewModel.HeightValue.Value = (uint)vm.PicViewer.PixelHeight.CurrentValue;
    }

    private void IsQualityEnabledBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || !IsQualityEnabledBox.IsChecked.HasValue)
        {
            return;
        }

        var isEnabled = IsQualityEnabledBox.IsChecked.Value;
        vm.Window.BatchResizeWindowConfig.WindowProperties.IsQualityEnabled = isEnabled;
        vm.BatchResizeViewModel.IsQualityEnabled.Value = isEnabled;
    }

    private void ResizeComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        vm.BatchResizeViewModel.IsPercentageResizing.Value = false;
        vm.BatchResizeViewModel.IsWidthAndHeightResizing.Value = false;
        vm.BatchResizeViewModel.IsWidthResizing.Value = false;
        vm.BatchResizeViewModel.IsHeightResizing.Value = false;

        switch (ResizeComboBox.SelectedIndex)
        {
            case (int)ResizeMode.None:
                vm.BatchResizeViewModel.Resize.Value = ResizeMode.None;
                break;
            case (int)ResizeMode.Height:
                vm.BatchResizeViewModel.Resize.Value = ResizeMode.Height;
                vm.BatchResizeViewModel.IsHeightResizing.Value = true;
                break;
            case (int)ResizeMode.Percentage:
                vm.BatchResizeViewModel.Resize.Value = ResizeMode.Percentage;
                vm.BatchResizeViewModel.IsPercentageResizing.Value = true;
                break;
            case (int)ResizeMode.Width:
                vm.BatchResizeViewModel.Resize.Value = ResizeMode.Width;
                vm.BatchResizeViewModel.IsWidthResizing.Value = true;
                break;
            case (int)ResizeMode.WidthAndHeight:
                vm.BatchResizeViewModel.Resize.Value = ResizeMode.WidthAndHeight;
                vm.BatchResizeViewModel.IsWidthAndHeightResizing.Value = true;
                break;
            default:
                vm.BatchResizeViewModel.Resize.Value = vm.BatchResizeViewModel.Resize.Value;
                break;
        }
    }

    private void ShowInFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        vm.PlatformService.LocateOnDisk(vm.BatchResizeViewModel.OutputFolder.CurrentValue);
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        _disposables.Dispose();
    }
}