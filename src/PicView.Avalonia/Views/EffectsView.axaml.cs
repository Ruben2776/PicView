using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using ImageMagick;
using PicView.Avalonia.ImageEffects;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.ViewModels;
using PicView.Core.FileHandling;
using ReactiveUI;
using Timer = System.Timers.Timer;

namespace PicView.Avalonia.Views;

public partial class EffectsView : UserControl
{
    private readonly CompositeDisposable _disposables = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private ImageEffectConfig _config;

    private Timer? _debounceTimer;

    public EffectsView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            vm.WhenAnyValue(x => x.IsLoading)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(isLoading =>
                {
                    if (isLoading)
                    {
                        SpinWaiter.IsVisible = true;
                        ParentContainer.IsHitTestVisible = false;
                        ParentContainer.Opacity = 0.5;
                    }
                    else
                    {
                        SpinWaiter.IsVisible = false;
                        ParentContainer.IsHitTestVisible = true;
                        ParentContainer.Opacity = 1;
                    }
                }).DisposeWith(_disposables);

            PointerPressed += (_, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
                {
                    // Context menu doesn't want to be opened normally
                    ContextMenu.Open();
                }
            };

            DetachedFromLogicalTree += (_, _) => CleanUp();

            ClearEffectsItem.Click += async delegate { await RemoveEffects(vm); };
            ResetContrastBtn.Click += delegate { ContrastSlider.Value = 0; };
            ResetBrightnessBtn.Click += delegate { BrightnessSlider.Value = 0; };
            ResetPencilSketchBtn.Click += delegate { PencilSketchSlider.Value = 0; };
            ResetPencilSketchBtn.Click += delegate { PencilSketchSlider.Value = 0; };
            ResetPosterizeBtn.Click += delegate { PosterizeSlider.Value = 0; };
            ResetSolarizeBtn.Click += delegate { SolarizeSlider.Value = 0; };
            ResetBlurBtn.Click += delegate { BlurSlider.Value = 0; };

            _debounceTimer = new Timer { Interval = 300, AutoReset = false };
            _debounceTimer.Elapsed += async (_, _) => await ApplyEffectsDebounced(vm).ConfigureAwait(false);

            BrightnessSlider.ValueChanged += (_, e) =>
            {
                _config.Brightness = new Percentage(e.NewValue);
                DebounceSliderChange();
            };

            ContrastSlider.ValueChanged += (_, e) =>
            {
                _config.Contrast = new Percentage(e.NewValue);
                DebounceSliderChange();
            };

            PencilSketchSlider.ValueChanged += (_, e) =>
            {
                _config.SketchStrokeWidth = e.NewValue;
                DebounceSliderChange();
            };

            PosterizeSlider.ValueChanged += (_, e) =>
            {
                var newValue = (int)e.NewValue;
                _config.PosterizeLevel = newValue is 1 ? 2 : newValue;
                DebounceSliderChange();
            };

            SolarizeSlider.ValueChanged += (_, e) =>
            {
                _config.Solarize = new Percentage(e.NewValue);
                DebounceSliderChange();
            };

            BlurSlider.ValueChanged += (_, e) =>
            {
                _config.BlurRadius = e.NewValue;
                DebounceSliderChange();
            };

            BlackAndWhiteToggleButton.Click += async (_, _) =>
            {
                if (!BlackAndWhiteToggleButton.IsChecked.HasValue)
                {
                    return;
                }

                _config.BlackAndWhite = BlackAndWhiteToggleButton.IsChecked.Value;
                await ApplyEffectsDebounced(vm);
            };

            NegativeToggleButton.Click += async (_, _) =>
            {
                if (!NegativeToggleButton.IsChecked.HasValue)
                {
                    return;
                }

                _config.Negative = NegativeToggleButton.IsChecked.Value;
                await ApplyEffectsDebounced(vm);
            };

            OldMovieToggleButton.Click += async (_, _) =>
            {
                if (!OldMovieToggleButton.IsChecked.HasValue)
                {
                    return;
                }

                _config.OldMovie = OldMovieToggleButton.IsChecked.Value;
                await ApplyEffectsDebounced(vm);
            };
        };
    }

    private void DebounceSliderChange()
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private async Task ApplyEffectsDebounced(MainViewModel vm)
    {
        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        }

        _cancellationTokenSource = new CancellationTokenSource();
        await ApplyEffects(vm, _config, _cancellationTokenSource.Token).ConfigureAwait(false);
    }

    private static async Task ApplyEffects(MainViewModel vm, ImageEffectConfig config,
        CancellationToken cancellationToken)
    {
        vm.IsLoading = true;
        try
        {
            await Task.Run(async () =>
            {
                var fileInfo = vm.FileInfo;
                await using var filestream = FileHelper.GetOptimizedFileStream(fileInfo);
                using var magick = new MagickImage();
                if (fileInfo.Length >= 2147483648)
                {
                    // Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                    magick.Read(filestream);
                }
                else
                {
                    await magick.ReadAsync(filestream, cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();

                magick.BrightnessContrast(config.Brightness, config.Contrast);
                magick.BackgroundColor = MagickColors.Transparent;
                magick.Settings.BackgroundColor = MagickColors.Transparent;
                magick.Settings.FillColor = MagickColors.Transparent;

                if (config.Negative)
                {
                    magick.Negate();
                }

                if (config.BlackAndWhite)
                {
                    magick.Grayscale();
                }

                if (config.OldMovie)
                {
                    // 1. Apply sepia tone
                    magick.SepiaTone(new Percentage(80));

                    // 2. Add noise
                    magick.AddNoise(NoiseType.MultiplicativeGaussian);

                    var random = new Random();

                    // 3. Add vertical bands (simulate scratches)
                    for (var i = 0; i < magick.Width; i += random.Next(1, 50))
                    {
                        using var band = new MagickImage(new MagickColor("#3E382A"), (uint)random.Next(1, 3),
                            magick.Height);
                        band.Evaluate(Channels.Alpha, EvaluateOperator.Set, 0.2); // semi-transparent
                        magick.Composite(band, i, 0, CompositeOperator.Over);
                    }
                }

                if (config.SketchStrokeWidth is not 0)
                {
                    magick.Charcoal(config.SketchStrokeWidth, 3);
                }

                if (config.PosterizeLevel is not 0)
                {
                    magick.Posterize(config.PosterizeLevel);
                }

                if (config.BlurRadius is not 0)
                {
                    magick.MotionBlur(0, config.BlurRadius, 0);
                }

                if (config.Solarize.ToUInt32() is not 0)
                {
                    magick.Solarize(config.Solarize);
                }

                cancellationToken.ThrowIfCancellationRequested();

                var bitmap = magick.ToWriteableBitmap();
                vm.ImageSource = bitmap;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            //
        }
        catch (Exception e)
        {
#if DEBUG
            Console.WriteLine(e);
#endif
        }
        finally
        {
            vm.IsLoading = false;
        }
    }

    private async Task RemoveEffects(MainViewModel vm)
    {
        using var magick = new MagickImage();
        await magick.ReadAsync(vm.FileInfo.FullName).ConfigureAwait(false);
        magick.BackgroundColor = MagickColors.Transparent;
        magick.Settings.BackgroundColor = MagickColors.Transparent;
        magick.Settings.FillColor = MagickColors.Transparent;
        var bitmap = magick.ToWriteableBitmap();
        vm.ImageSource = bitmap;
        _config = new ImageEffectConfig();
    }

    ~EffectsView()
    {
        CleanUp();
    }

    private void CleanUp()
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IsLoading = false;
        }

        _debounceTimer?.Dispose();
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}