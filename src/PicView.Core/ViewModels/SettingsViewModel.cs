using R3;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace PicView.Core.ViewModels;

public class SettingsViewModel : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    public BindableReactiveProperty<bool> IsShowingRecycleDialog { get; } =
        new(Settings.UIProperties.ShowRecycleConfirmation);

    public BindableReactiveProperty<bool> IsShowingPermanentDeletionDialog { get; } =
        new(Settings.UIProperties.ShowPermanentDeletionConfirmation);

    public BindableReactiveProperty<bool> IsBottomGalleryShownInHiddenUI { get; } =
        new(Settings.Gallery.ShowBottomGalleryInHiddenUI);

    public BindableReactiveProperty<bool> IsOpeningInSameWindow { get; } = new(Settings.UIProperties.OpenInSameWindow);

    public BindableReactiveProperty<bool> IsShowingConfirmationOnEsc { get; } =
        new(Settings.UIProperties.ShowConfirmationOnEsc);

    public BindableReactiveProperty<bool> IsStayingCentered { get; } = new(Settings.WindowProperties.KeepCentered);

    public BindableReactiveProperty<bool> IsUsingTouchpad { get; } = new(Settings.Zoom.IsUsingTouchPad);

    public BindableReactiveProperty<bool> IsConstrainingBackgroundColor { get; } =
        new(Settings.UIProperties.IsConstrainBackgroundColorEnabled);

    public BindableReactiveProperty<bool> IsAvoidingZoomingOut { get; } = new(Settings.Zoom.AvoidZoomingOut);

    public BindableReactiveProperty<bool> IsZoomAnimated { get; } = new(Settings.Zoom.IsZoomAnimated);

    public BindableReactiveProperty<bool> IsShowingZoomPercentagePopup { get; } =
        new(Settings.Zoom.IsShowingZoomPercentagePopup);

    public BindableReactiveProperty<double> WindowMargin { get; } = new(Settings.WindowProperties.Margin);

    public BindableReactiveProperty<double> NavSpeed { get; } = new(Settings.UIProperties.NavSpeed);
    public BindableReactiveProperty<double> GetNavSpeed { get; } = new();

    public BindableReactiveProperty<double> ZoomSpeed { get; } = new(Settings.Zoom.ZoomSpeed);
    public BindableReactiveProperty<double> GetZoomSpeed { get; } = new();

    public BindableReactiveProperty<double> SlideshowSpeed { get; } = new(Settings.UIProperties.SlideShowTimer);
    public BindableReactiveProperty<double> GetSlideshowSpeed { get; } = new();

    public void Dispose()
    {
        Disposable.Dispose(_disposables,
            GetNavSpeed,
            GetSlideshowSpeed,
            GetZoomSpeed,
            GoBackCommand,
            GoForwardCommand,
            IsAvoidingZoomingOut,
            IsBackButtonEnabled,
            IsBottomGalleryShownInHiddenUI,
            IsConstrainingBackgroundColor,
            IsForwardButtonEnabled,
            IsOpeningInSameWindow,
            IsShowingConfirmationOnEsc,
            IsShowingPermanentDeletionDialog,
            IsShowingRecycleDialog,
            IsStayingCentered,
            IsUsingTouchpad,
            NavSpeed,
            SlideshowSpeed,
            WindowMargin,
            ZoomSpeed);
    }

    /// <summary>
    /// Subscribes to settings properties changes and updates relevant application settings and UI state.
    /// This method binds observable properties in the <see cref="SettingsViewModel"/> to the corresponding
    /// settings in the application's configuration, ensuring real-time updates.
    /// </summary>
    public void SubscriptionSettingsUpdate()
    {
        Observable.EveryValueChanged(this, x => x.NavSpeed.CurrentValue)
            .Subscribe(x =>
            {
                Settings.UIProperties.NavSpeed = x;
                GetNavSpeed.Value = Math.Round(Settings.UIProperties.NavSpeed, 2);
            }).AddTo(_disposables);
        Observable.EveryValueChanged(this, x => x.ZoomSpeed.CurrentValue)
            .Subscribe(x =>
            {
                Settings.Zoom.ZoomSpeed = x;
                GetZoomSpeed.Value = Math.Round(Settings.Zoom.ZoomSpeed, 2);
            }).AddTo(_disposables);
        Observable.EveryValueChanged(this, x => x.SlideshowSpeed.CurrentValue)
            .Subscribe(x =>
            {
                var roundedValue = Math.Round(x, 2);
                Settings.UIProperties.SlideShowTimer = roundedValue;
                GetSlideshowSpeed.Value = roundedValue;
            }).AddTo(_disposables);
        
        Observable.EveryValueChanged(this, x => x.IsShowingPermanentDeletionDialog.CurrentValue)
            .SubscribeAwait(async (x, _) =>
            {
                Settings.UIProperties.ShowPermanentDeletionConfirmation = x;
                await SaveSettingsAsync();
            }).AddTo(_disposables);
        
        Observable.EveryValueChanged(this, x => x.IsShowingRecycleDialog.CurrentValue)
            .SubscribeAwait(async (x, _) =>
            {
                Settings.UIProperties.ShowRecycleConfirmation = x;
                await SaveSettingsAsync();
            }).AddTo(_disposables);


        Observable.EveryValueChanged(this, x => x.IsAvoidingZoomingOut.CurrentValue)
            .Subscribe(x => Settings.Zoom.AvoidZoomingOut = x).AddTo(_disposables);

        Observable.EveryValueChanged(this, x => x.IsZoomAnimated.CurrentValue)
            .Subscribe(x => Settings.Zoom.IsZoomAnimated = x).AddTo(_disposables);

        Observable.EveryValueChanged(this, x => x.IsShowingZoomPercentagePopup.CurrentValue)
            .Subscribe(x => Settings.Zoom.IsShowingZoomPercentagePopup = x).AddTo(_disposables);

        Observable.EveryValueChanged(this, x => x.IsUsingTouchpad.CurrentValue)
            .Subscribe(x => Settings.Zoom.IsUsingTouchPad = x).AddTo(_disposables);

        Observable.EveryValueChanged(this, x => x.IsStayingCentered.CurrentValue)
            .Subscribe(x => Settings.WindowProperties.KeepCentered = x).AddTo(_disposables);

        Observable.EveryValueChanged(this, x => x.IsShowingConfirmationOnEsc.CurrentValue)
            .Subscribe(x => Settings.UIProperties.ShowConfirmationOnEsc = x).AddTo(_disposables);

        Observable.EveryValueChanged(this, x => x.IsOpeningInSameWindow.CurrentValue)
            .Subscribe(x => Settings.UIProperties.OpenInSameWindow = x).AddTo(_disposables);
        
        Observable.EveryValueChanged(this, x => x.WindowMargin.CurrentValue)
            .Subscribe(x => Settings.WindowProperties.Margin = x).AddTo(_disposables);
    }

    #region Tab history navigation

    public BindableReactiveProperty<bool> IsBackButtonEnabled { get; } = new();

    public BindableReactiveProperty<bool> IsForwardButtonEnabled { get; } = new();
    public ReactiveCommand? GoForwardCommand { get; set; }

    public ReactiveCommand? GoBackCommand { get; set; }

    public void InitializeNavigation(Action goBack, Action goForward)
    {
        GoForwardCommand = IsForwardButtonEnabled.ToReactiveCommand(_ => { goForward(); });
        GoBackCommand = IsBackButtonEnabled.ToReactiveCommand(_ => { goBack(); });
    }

    #endregion
}