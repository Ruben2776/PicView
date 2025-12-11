using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.ImageEffects;
using R3;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace PicView.Avalonia.Views.Main;

/// <summary>
/// User control that provides an interface for applying and managing image effects.
/// Allows users to adjust brightness, contrast, and other visual effects for displayed images.
/// </summary>
public partial class EffectsView : UserControl
{
    private readonly ImageEffectConfig _defaultEffectConfig = new();
    private readonly CompositeDisposable _disposables = new();
    private bool _reloading;
    private readonly TimeSpan _debounceTime = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Initializes a new instance of the <see cref="EffectsView"/> class.
    /// Sets up event handlers for control lifecycle events.
    /// </summary>
    public EffectsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DetachedFromLogicalTree += OnDetachedFromLogicalTree;
    }

    /// <summary>
    /// Handles the control's Loaded event.
    /// Initializes the view model, UI event handlers, and reactive subscriptions.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments.</param>
    private void OnLoaded(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        InitializeViewModel(vm);
        InitializeUIEvents(vm);
        InitializeReactiveEffects(vm);
    }

    /// <summary>
    /// Initializes and configures the view model.
    /// Sets up reactive properties and applies effect configurations.
    /// </summary>
    /// <param name="vm">The main view model to initialize.</param>
    private void InitializeViewModel(MainViewModel vm)
    {
        // Reset on file change
        Observable.EveryValueChanged(vm.PicViewer.FileInfo, x => x.CurrentValue, UIHelper.GetFrameProvider)
            .Subscribe(_ => Reset())
            .AddTo(_disposables);

        if (vm.PicViewer.EffectConfig == null)
        {
            vm.PicViewer.EffectConfig.Value = new ImageEffectConfig();
            HideResetBtn();
        }
        else if (IsDefaultEffectConfig(vm.PicViewer.EffectConfig.CurrentValue))
        {
            HideResetBtn();
        }
        else
        {
            ApplyEffectConfig(vm.PicViewer.EffectConfig.CurrentValue);
        }
    }

    /// <summary>
    /// Initializes UI event handlers for buttons. Slider events are handled reactively.
    /// </summary>
    /// <param name="vm">The main view model to use for event handling.</param>
    private void InitializeUIEvents(MainViewModel vm)
    {
        if (!Settings.Theme.Dark)
        {
            if (TryGetResource("CancelBrush",
                    Application.Current.RequestedThemeVariant, out var cBrush))
            {
                if (cBrush is SolidColorBrush brush)
                {
                    UIHelper.SetButtonHover(CancelButton, brush);
                    UIHelper.SetButtonHover(ResetButton, brush);
                }
            }
            UIHelper.SwitchAccentHoverClass(ResetButton);
            UIHelper.SwitchAccentHoverClass(CancelButton);
            LowerPanel.Background = new SolidColorBrush(Color.Parse("#5DA2A2A2"));
        }
        
        CloseItem.Click += (_, _) => (VisualRoot as Window)?.Close();
        PointerPressed += OnPointerPressed;
        ClearEffectsItem.Click += async (_, _) => await RemoveEffects();

        ResetContrastBtn.Click += (_, _) => ContrastSlider.Value = 0;
        ResetBrightnessBtn.Click += (_, _) => BrightnessSlider.Value = 0;
        ResetPencilSketchBtn.Click += (_, _) => PencilSketchSlider.Value = 0;
        ResetPosterizeBtn.Click += (_, _) => PosterizeSlider.Value = 0;
        ResetSolarizeBtn.Click += (_, _) => SolarizeSlider.Value = 0;
        ResetBlurBtn.Click += (_, _) => BlurSlider.Value = 0;

        ResetButton.Click += async (_, _) => await RemoveEffects();
        CancelButton.Click += (_, _) => (VisualRoot as Window)?.Close();

        BlackAndWhiteToggleButton.Click += async delegate
        {
            vm.PicViewer.EffectConfig.Value ??= new ImageEffectConfig();
            var isBlackAndWhite = BlackAndWhiteToggleButton.IsChecked ?? false;
            await UpdateToggleEffect(vm, config => config.BlackAndWhite = isBlackAndWhite);
            HideCancelBtn();
        };
        NegativeToggleButton.Click += async delegate
        {
            vm.PicViewer.EffectConfig.Value ??= new ImageEffectConfig();
            var isNegative = NegativeToggleButton.IsChecked ?? false;
            await UpdateToggleEffect(vm, config => config.Negative = isNegative);
            HideCancelBtn();
        };
        OldMovieToggleButton.Click += async delegate
        {
            vm.PicViewer.EffectConfig.Value ??= new ImageEffectConfig();
            var isOldMovie = OldMovieToggleButton.IsChecked ?? false;
            await UpdateToggleEffect(vm, config => config.OldMovie = isOldMovie);
            HideCancelBtn();
        };
    }

    /// <summary>
    /// Initializes reactive subscriptions for slider value changes to apply effects with a debounce.
    /// </summary>
    /// <param name="vm">The main view model.</param>
    private void InitializeReactiveEffects(MainViewModel vm)
    {
        // Create a merged stream of ValueChanged events from all sliders
        var sliderValueChanges = Observable.Merge(
            Observable.FromEventHandler<RangeBaseValueChangedEventArgs>(h => BrightnessSlider.ValueChanged += h,
                h => BrightnessSlider.ValueChanged -= h),
            Observable.FromEventHandler<RangeBaseValueChangedEventArgs>(h => ContrastSlider.ValueChanged += h,
                h => ContrastSlider.ValueChanged -= h),
            Observable.FromEventHandler<RangeBaseValueChangedEventArgs>(h => PencilSketchSlider.ValueChanged += h,
                h => PencilSketchSlider.ValueChanged -= h),
            Observable.FromEventHandler<RangeBaseValueChangedEventArgs>(h => PosterizeSlider.ValueChanged += h,
                h => PosterizeSlider.ValueChanged -= h),
            Observable.FromEventHandler<RangeBaseValueChangedEventArgs>(h => SolarizeSlider.ValueChanged += h,
                h => SolarizeSlider.ValueChanged -= h),
            Observable.FromEventHandler<RangeBaseValueChangedEventArgs>(h => BlurSlider.ValueChanged += h,
                h => BlurSlider.ValueChanged -= h)
        );

        sliderValueChanges
            .Debounce(_debounceTime)
            .ObserveOn(UIHelper.GetFrameProvider)
            .Select(_ =>
            {
                // Update the config with the latest slider values
                var config = vm.PicViewer.EffectConfig.Value ??= new ImageEffectConfig();
                config.Brightness = new Percentage(BrightnessSlider.Value);
                config.Contrast = new Percentage(ContrastSlider.Value);
                config.SketchStrokeWidth = PencilSketchSlider.Value;
                config.PosterizeLevel = (int)PosterizeSlider.Value == 1 ? 2 : (int)PosterizeSlider.Value;
                config.Solarize = new Percentage(SolarizeSlider.Value);
                config.BlurLevel = BlurSlider.Value;
                HideCancelBtn();
                return config;
            })
            // Switch to a background thread to apply effects asynchronously.
            // SelectAwait automatically handles cancellation of previous operations.
            .SelectAwait(async (config, ct) =>
            {
                if (_reloading)
                {
                    return (null, vm);
                }

                vm.MainWindow.IsLoadingIndicatorShown.Value = true;

                var magick = await ImageEffectsHelper.ApplyEffects(
                    vm.PicViewer.FileInfo.CurrentValue,
                    config,
                    ct);

                return (magick, vm);
            })
            .Subscribe(result =>
            {
                var (magick, viewModel) = result;
                if (magick is not null)
                {
                    viewModel.PicViewer.ImageSource.Value = magick.ToWriteableBitmap();
                }

                viewModel.MainWindow.IsLoadingIndicatorShown.Value = false;
            })
            .AddTo(_disposables);
    }

    /// <summary>
    /// Hides the reset button and shows the cancel button.
    /// Used when the effect configuration is at default settings.
    /// </summary>
    private void HideResetBtn()
    {
        ResetButton.IsVisible = false;
        CancelButton.IsVisible = true;
    }

    /// <summary>
    /// Shows the reset button and hides the cancel button.
    /// Used when effects have been applied and can be reset.
    /// </summary>
    private void HideCancelBtn()
    {
        if (_reloading)
        {
            return;
        }
        ResetButton.IsVisible = true;
        CancelButton.IsVisible = false;
    }

    /// <summary>
    /// Handles pointer press events to show context menu on right-click.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments containing pointer information.</param>
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            ContextMenu.Open();
        }
    }

    /// <summary>
    /// Applies image effects immediately based on the current configuration.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ApplyCurrentEffectsAsync()
    {
        if (_reloading || DataContext is not MainViewModel vm)
        {
            return;
        }

        vm.MainWindow.IsLoadingIndicatorShown.Value = true;
        try
        {
            var magick = await ImageEffectsHelper
                .ApplyEffects(vm.PicViewer.FileInfo.CurrentValue, vm.PicViewer.EffectConfig.CurrentValue,
                    CancellationToken.None)
                .ConfigureAwait(false);
            if (magick is not null)
            {
                vm.PicViewer.ImageSource.Value = magick.ToWriteableBitmap();
            }
        }
        finally
        {
           vm.MainWindow.IsLoadingIndicatorShown.Value = false;
        }
    }

    /// <summary>
    /// Removes all effects from the current image and resets the UI.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RemoveEffects()
    {
        _reloading = true;
        try
        {
            await NavigationManager.QuickReload().ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(Reset);
        }
        finally
        {
            await Task.Delay(_debounceTime); // Fixes HideCancelBtn resetting erroneously 
            _reloading = false;
        }
    }

    /// <summary>
    /// Resets all UI controls to their default values and clears effect configuration.
    /// </summary>
    private void Reset()
    {
        HideResetBtn();
        BlackAndWhiteToggleButton.IsChecked = false;
        NegativeToggleButton.IsChecked = false;
        OldMovieToggleButton.IsChecked = false;
        ContrastSlider.Value = 0;
        BrightnessSlider.Value = 0;
        PencilSketchSlider.Value = 0;
        PosterizeSlider.Value = 0;
        SolarizeSlider.Value = 0;
        BlurSlider.Value = 0;
        if (DataContext is MainViewModel vm)
        {
            vm.PicViewer.EffectConfig.Value = new ImageEffectConfig();
        }
    }

    /// <summary>
    /// Applies an effect configuration to the UI controls.
    /// </summary>
    /// <param name="config">The effect configuration to apply.</param>
    private void ApplyEffectConfig(ImageEffectConfig config)
    {
        BlackAndWhiteToggleButton.IsChecked = config.BlackAndWhite;
        OldMovieToggleButton.IsChecked = config.OldMovie;
        NegativeToggleButton.IsChecked = config.Negative;
        BrightnessSlider.Value = config.Brightness.ToInt32();
        ContrastSlider.Value = config.Contrast.ToInt32();
        PencilSketchSlider.Value = config.SketchStrokeWidth;
        PosterizeSlider.Value = config.PosterizeLevel;
        SolarizeSlider.Value = config.Solarize.ToInt32();
        BlurSlider.Value = config.BlurLevel;

        HideCancelBtn();
    }

    /// <summary>
    /// Updates a toggle effect in the effect configuration and applies the effects immediately.
    /// </summary>
    /// <param name="vm">The main view model.</param>
    /// <param name="updateAction">An action that updates the effect configuration.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task UpdateToggleEffect(MainViewModel vm, Action<ImageEffectConfig> updateAction)
    {
        updateAction(vm.PicViewer.EffectConfig.CurrentValue);
        await ApplyCurrentEffectsAsync();
    }

    /// <summary>
    /// Handles the control being detached from the logical tree.
    /// Cleans up resources to prevent memory leaks.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments.</param>
    private void OnDetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        CleanUp();
    }

    /// <summary>
    /// Finalizer that ensures resources are cleaned up.
    /// </summary>
    ~EffectsView()
    {
        CleanUp();
    }

    /// <summary>
    /// Cleans up resources used by this control.
    /// Disposes of disposables.
    /// </summary>
    private void CleanUp()
    {
        if (DataContext is MainViewModel vm)
        {
            vm.MainWindow.IsLoadingIndicatorShown.Value = false;
        }

        _disposables.Dispose();
    }

    /// <summary>
    /// Determines if an effect configuration matches the default configuration.
    /// </summary>
    /// <param name="config">The effect configuration to check.</param>
    /// <returns>True if the configuration matches the default; otherwise, false.</returns>
    private bool IsDefaultEffectConfig(ImageEffectConfig config) =>
        config.Brightness == _defaultEffectConfig.Brightness &&
        config.Contrast == _defaultEffectConfig.Contrast &&
        config.SketchStrokeWidth == _defaultEffectConfig.SketchStrokeWidth &&
        config.PosterizeLevel == _defaultEffectConfig.PosterizeLevel &&
        config.Solarize == _defaultEffectConfig.Solarize &&
        config.BlurLevel == _defaultEffectConfig.BlurLevel &&
        config.BlackAndWhite == _defaultEffectConfig.BlackAndWhite &&
        config.Negative == _defaultEffectConfig.Negative &&
        config.OldMovie == _defaultEffectConfig.OldMovie;
}