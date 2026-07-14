using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.UI;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.Main;

public partial class SingleImageResizeView : UserControl
{
    private DisposableBag _disposables;

    public SingleImageResizeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainVm)
        {
            return;
        }

        ApplyThemeAdjustments();

        mainVm.ResizeImageViewModel.Initialize(mainVm);
        var vm = mainVm.ResizeImageViewModel;

        vm.CloseAction = SafeClose;
        vm.PickFileAction = SafePickAsync;

        RegisterEventHandlers(mainVm);
    }

    private void SafeClose()
    {
        Dispatcher.Invoke(() =>
        {
            if (TopLevel.GetTopLevel(this) is not Window window)
            {
                return;
            }
            window.Close();
        });
    }
    
    private static async ValueTask<string?> SafePickAsync(string file, string extension) 
        => await FilePicker.PickFileForSavingAsync(file, extension);

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _disposables.Dispose();
        if (DataContext is not MainWindowViewModel mainVm)
        {
            return;
        }

        mainVm.ResizeImageViewModel?.Dispose();
        mainVm.ResizeImageViewModel = null;
        
        PixelWidthTextBox.KeyDown -= TextBoxOnKeyDown;
        PixelHeightTextBox.KeyDown -= TextBoxOnKeyDown;
    }

    private void ApplyThemeAdjustments()
    {
        if (!Settings.Theme.Dark || Settings.Theme.GlassTheme)
        {
            BgPanel.Background = Brushes.Transparent;
        }

        if (!Settings.Theme.Dark)
        {
            var topBg = new SolidColorBrush(Color.FromArgb(65, 162, 162, 162));
            var bottomBg = new SolidColorBrush(Color.FromArgb(93, 162, 162, 162));
            MainBorder.Background = topBg;
            BottomBorder.Background = bottomBg;

            var noThickness = new Thickness(0);
            PixelWidthTextBox.BorderThickness = noThickness;
            PixelHeightTextBox.BorderThickness = noThickness;

            if (TryGetResource("CancelBrush", Application.Current.RequestedThemeVariant, out var cBrush) && cBrush is SolidColorBrush brush)
            {
                UIHelper.SetButtonHover(CancelButton, brush);
            }
            UIHelper.SwitchAccentHoverClass(CancelButton);
        }
    }

    private void RegisterEventHandlers(MainWindowViewModel mainVm)
    {
        var vm = mainVm.ResizeImageViewModel!;

        // VM -> UI sync
        vm.IsLoading.Subscribe(SetLoadingState).AddTo(ref _disposables);
        vm.IsKeepingAspectRatio.Subscribe(ToggleLinkChain).AddTo(ref _disposables);

        // Button clicks
        SaveButton.Click += async (_, _) => await vm.SaveImage();
        SaveAsButton.Click += async (_, _) => await vm.SaveImageAs();
        ResetButton.Click += (_, _) => vm.ResetSettings();
        CancelButton.Click += (_, _) => vm.CloseAction?.Invoke();
        LinkChainButton.Click += (_, _) => vm.ToggleAspectRatio();
        
        PixelWidthTextBox.KeyDown += TextBoxOnKeyDown;
        PixelHeightTextBox.KeyDown += TextBoxOnKeyDown;
    }

    private void TextBoxOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter || DataContext is not MainWindowViewModel mainVm)
        {
            return;
        }
        var vm = mainVm.ResizeImageViewModel;
        _ = vm.SaveImage();
    }

    private void SetLoadingState(bool isLoading)
    {
        Dispatcher.Invoke(() =>
        {
            ParentContainer.Opacity = isLoading ? 0.1 : 1;
            ParentContainer.IsHitTestVisible = !isLoading;
            SpinWaiter.IsVisible = isLoading;
        });
    }

    private void ToggleLinkChain(bool isKeepingAspectRatio)
    {
        var resourceName = isKeepingAspectRatio ? "LinkChainImage" : "UnlinkChainImage";
        if (Application.Current.TryGetResource(resourceName, Application.Current.RequestedThemeVariant, out var link) && link is DrawingImage linkImage)
        {
            LinkChainButton.Icon = linkImage;
        }
    }
}