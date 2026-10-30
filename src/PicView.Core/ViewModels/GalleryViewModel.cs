using ObservableCollections;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.Navigation;
using R3;

namespace PicView.Core.ViewModels;

public class GalleryViewModel : IDisposable
{
    private DisposableBag _disposables;

    /// <param name="saveSettings">Saves the settings once the remembered docked choice changed; the settings file by default.</param>
    public GalleryViewModel(Func<ValueTask<bool>>? saveSettings = null)
    {
        Switcher = new GalleryModeSwitcher(saveSettings);
        Mode = Switcher.Mode.ToReadOnlyBindableReactiveProperty();
        ShownMode = Switcher.ShownMode.ToReadOnlyBindableReactiveProperty();
        IsDocked = Switcher.IsDocked.ToReadOnlyBindableReactiveProperty();

        // Derived from the switcher before Initialize, so a tab started docked shows it from the start
        Switcher.ShownMode.Subscribe(OnShownModeChanged).AddTo(ref _disposables);
    }

    /// <summary>
    /// Changes this tab's Gallery Mode; nothing else does (ADR-0011).
    /// </summary>
    public GalleryModeSwitcher Switcher { get; }

    public ReactiveCommand<Unit> ToggleExpandedCommand { get; } = new();
    public ReactiveCommand<Unit> ToggleDockedCommand { get; } = new();
    public ReactiveCommand<Unit> ContractCommand { get; } = new();
    public ReactiveCommand<Unit> CloseCommand { get; } = new();
    public ReactiveCommand<GalleryDockPosition> DockAtCommand { get; } = new();
    public ReactiveCommand<NavigateTo> NavigateGalleryCommand { get; } = new();
    public ReactiveCommand<int> OpenSelectedItemCommand { get; } = new();

    public ObservableList<GalleryItemViewModel> GalleryItems { get; } = new([]);

    /// <inheritdoc cref="GalleryModeSwitcher.Mode"/>
    public IReadOnlyBindableReactiveProperty<GalleryMode> Mode { get; }

    /// <inheritdoc cref="GalleryModeSwitcher.ShownMode"/>
    public IReadOnlyBindableReactiveProperty<GalleryMode> ShownMode { get; }

    /// <inheritdoc cref="GalleryModeSwitcher.IsDocked"/>
    public IReadOnlyBindableReactiveProperty<bool> IsDocked { get; }

    /// <summary>
    /// Whether the Expanded Gallery is shown. Only this view model writes it.
    /// </summary>
    public BindableReactiveProperty<bool> IsGalleryExpanded { get; } = new();

    /// <summary>
    /// Whether the Docked Gallery is shown. Only this view model writes it.
    /// </summary>
    public BindableReactiveProperty<bool> IsDockedGalleryVisible { get; } = new();

    public BindableReactiveProperty<double> ItemSpacing { get; } = new(Settings.Gallery.ItemSpacing);
    public BindableReactiveProperty<double> LineSpacing { get; } = new(Settings.Gallery.LineSpacing);
    public BindableReactiveProperty<int> SelectedGalleryItemIndex { get; } = new(-1);

    // Whether the Docked Gallery is shown at that edge. Only this view model writes them.
    public BindableReactiveProperty<bool> IsTopDocked { get; } = new();
    public BindableReactiveProperty<bool> IsBottomDocked { get; } = new();
    public BindableReactiveProperty<bool> IsLeftDocked { get; } = new();
    public BindableReactiveProperty<bool> IsRightDocked { get; } = new();

    public GalleryLoadingState LoadingState { get; internal set; }

    public void Initialize()
    {
        if (Settings.Gallery is not { } gallery)
        {
            return;
        }

        Observable.EveryValueChanged(gallery, g => g.DockPosition)
            .Subscribe(_ => UpdateDockFlags(), DebugHelper.LogError(nameof(GalleryViewModel), nameof(UpdateDockFlags)))
            .AddTo(ref _disposables);

        Observable.EveryValueChanged(gallery, g => g.ItemSpacing)
            .Subscribe(x =>
            {
                ItemSpacing.Value = x;
            }, DebugHelper.LogError(nameof(GalleryViewModel), nameof(Initialize)))
            .AddTo(ref _disposables);

        Observable.EveryValueChanged(gallery, g => g.LineSpacing)
            .Subscribe(x =>
            {
                LineSpacing.Value = x;
            }, DebugHelper.LogError(nameof(GalleryViewModel), nameof(Initialize)))
            .AddTo(ref _disposables);

        ToggleExpandedCommand.Subscribe(_ => Switcher.ToggleExpanded(),
                DebugHelper.LogError(nameof(GalleryViewModel), nameof(ToggleExpandedCommand)))
            .AddTo(ref _disposables);

        ToggleDockedCommand.SubscribeAwait(async (_, _) => await Switcher.ToggleDocked().ConfigureAwait(false),
                DebugHelper.LogError(nameof(GalleryViewModel), nameof(ToggleDockedCommand)))
            .AddTo(ref _disposables);

        ContractCommand.SubscribeAwait(async (_, _) => await Switcher.Contract().ConfigureAwait(false),
                DebugHelper.LogError(nameof(GalleryViewModel), nameof(ContractCommand)))
            .AddTo(ref _disposables);

        CloseCommand.SubscribeAwait(async (_, _) => await Switcher.Close().ConfigureAwait(false),
                DebugHelper.LogError(nameof(GalleryViewModel), nameof(CloseCommand)))
            .AddTo(ref _disposables);

        DockAtCommand.SubscribeAwait(async (position, _) => await Switcher.DockAt(position).ConfigureAwait(false),
                DebugHelper.LogError(nameof(GalleryViewModel), nameof(DockAtCommand)))
            .AddTo(ref _disposables);
    }

    public void Navigate(NavigateTo direction)
    {
        NavigateGalleryCommand.Execute(direction);
    }

    private void OnShownModeChanged(GalleryMode mode)
    {
        IsGalleryExpanded.Value = mode == GalleryMode.Expanded;
        IsDockedGalleryVisible.Value = mode == GalleryMode.Docked;
        if (mode != GalleryMode.Expanded)
        {
            SelectedGalleryItemIndex.Value = -1;
        }

        UpdateDockFlags();
    }

    private void UpdateDockFlags()
    {
        var shown = IsDockedGalleryVisible.Value;
        var position = Settings.Gallery.DockPosition;
        IsTopDocked.Value = shown && position == GalleryDockPosition.Top;
        IsBottomDocked.Value = shown && position == GalleryDockPosition.Bottom;
        IsLeftDocked.Value = shown && position == GalleryDockPosition.Left;
        IsRightDocked.Value = shown && position == GalleryDockPosition.Right;
    }

    public void Dispose()
    {
        _disposables.Dispose();
        Disposable.Dispose(
            ToggleExpandedCommand,
            ToggleDockedCommand,
            ContractCommand,
            CloseCommand,
            DockAtCommand,
            NavigateGalleryCommand,
            OpenSelectedItemCommand,
            Mode,
            ShownMode,
            IsDocked,
            IsGalleryExpanded,
            IsDockedGalleryVisible,
            ItemSpacing,
            LineSpacing,
            SelectedGalleryItemIndex,
            IsTopDocked,
            IsBottomDocked,
            IsLeftDocked,
            IsRightDocked,
            Switcher
        );
        GC.SuppressFinalize(this);
    }
}
