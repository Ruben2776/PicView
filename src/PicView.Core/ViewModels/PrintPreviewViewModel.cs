using PicView.Core.Localization;
using PicView.Core.Printing;
using R3;

namespace PicView.Core.ViewModels;

public class PrintPreviewViewModel : IDisposable
{
    public readonly CompositeDisposable Disposables = new();

    public PrintPreviewViewModel()
    {
        ScaleModes.Value =
        [
            TranslationManager.Translation.Fit, 
            TranslationManager.Translation.Fill, 
            TranslationManager.Translation.Stretch,
            TranslationManager.Translation.Center
        ];

        Orientations.Value =
        [
            TranslationManager.Translation.Portrait,
            TranslationManager.Translation.Landscape
        ];

        ColorModes.Value =
        [
            TranslationManager.Translation.Auto, 
            TranslationManager.Translation.Color,
            TranslationManager.Translation.BlackAndWhite
        ];

    }

    #region Bindable Properties

    public BindableReactiveProperty<IEnumerable<string>> Printers { get; } = new();
    public BindableReactiveProperty<IEnumerable<string>> PaperSizes { get; } = new();
    public BindableReactiveProperty<IEnumerable<string?>> ScaleModes { get; } = new();
    public BindableReactiveProperty<IEnumerable<string?>> ColorModes { get; } = new();
    public BindableReactiveProperty<IEnumerable<string?>> Orientations { get; } = new();
    public BindableReactiveProperty<PrintSettings> PrintSettings { get; } = new();
    public BindableReactiveProperty<object?> PreviewImage { get; } = new();

    public BindableReactiveProperty<double> PageWidth { get; } = new();
    public BindableReactiveProperty<double> PageHeight { get; } = new();

    public BindableReactiveProperty<double> Zoom { get; } = new(1.0);

    public BindableReactiveProperty<bool> IsProcessing { get; } = new(false);
    public BindableReactiveProperty<double> Opacity { get; } = new(1.0);

    public object? GrayCache { get; set; }

    #endregion


    #region Commands

    public ReactiveCommand<Unit> PrintCommand { get; } = new();
    public ReactiveCommand<Unit> CancelCommand { get; } = new();

    #endregion

    #region Disposal

    public void Dispose()
    {
        Disposable.Dispose(Disposables,
            Printers,
            PaperSizes,
            ScaleModes,
            ColorModes,
            Orientations,
            PrintSettings,
            PreviewImage,
            PageWidth,
            PageHeight,
            Zoom,
            IsProcessing,
            Opacity,
            PrintCommand,
            CancelCommand);
    }

    #endregion
}