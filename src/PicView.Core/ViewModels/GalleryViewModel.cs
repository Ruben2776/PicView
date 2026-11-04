using ObservableCollections;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.Navigation;
using R3;

namespace PicView.Core.ViewModels;

public class GalleryViewModel : IDisposable
{
    private DisposableBag _disposables;
    private readonly Subject<Unit> _sizesChanged = new();
    private readonly BindableReactiveProperty<GalleryAppearance> _appearance;

    /// <summary>
    /// Follows the shown mode with the settings' values until <see cref="Initialize"/> brings the shared ones.
    /// </summary>
    private IDisposable? _appearanceBeforeInitialize;

    /// <param name="saveSettings">Saves the settings once the remembered docked choice changed; the settings file by default.</param>
    public GalleryViewModel(Func<ValueTask<bool>>? saveSettings = null)
    {
        Switcher = new GalleryModeSwitcher(saveSettings);
        Mode = Switcher.Mode.ToReadOnlyBindableReactiveProperty();
        ShownMode = Switcher.ShownMode.ToReadOnlyBindableReactiveProperty();
        IsDocked = Switcher.IsDocked.ToReadOnlyBindableReactiveProperty();
        _appearance = new BindableReactiveProperty<GalleryAppearance>(ComputeAppearance(
            ShownMode.Value, Settings.Gallery.DockedGalleryItemSize,
            Settings.Gallery.ExpandedGalleryItemSize, Settings.Gallery.DockedGalleryStretchMode,
            Settings.Gallery.ExpandedGalleryStretchMode, Settings.Gallery.ItemSpacing));
        Appearance = _appearance;

        // Derived from the switcher before Initialize, so a tab started docked shows it from the start
        Switcher.ShownMode.Subscribe(OnShownModeChanged).AddTo(ref _disposables);

        // A tab can start docked, and its view can load, before Initialize: the appearance follows the mode anyway
        _appearanceBeforeInitialize = Switcher.ShownMode.Subscribe(mode => _appearance.Value = ComputeAppearance(mode,
            Settings.Gallery.DockedGalleryItemSize,
            Settings.Gallery.ExpandedGalleryItemSize, Settings.Gallery.DockedGalleryStretchMode,
            Settings.Gallery.ExpandedGalleryStretchMode, Settings.Gallery.ItemSpacing), 
            DebugHelper.LogError(nameof(GalleryViewModel), nameof(ShownMode)));
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
    public ObservableList<GalleryItemViewModel> GalleryItems { get; } = [];

    /// <inheritdoc cref="GalleryModeSwitcher.Mode"/>
    public IReadOnlyBindableReactiveProperty<GalleryMode> Mode { get; }

    /// <inheritdoc cref="GalleryModeSwitcher.ShownMode"/>
    public IReadOnlyBindableReactiveProperty<GalleryMode> ShownMode { get; }

    /// <inheritdoc cref="GalleryModeSwitcher.IsDocked"/>
    public IReadOnlyBindableReactiveProperty<bool> IsDocked { get; }

    /// <summary>
    /// How this tab's gallery looks, derived from its shown mode, the dock position and the gallery settings.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<GalleryAppearance> Appearance { get; }

    /// <summary>
    /// Whether the Expanded Gallery is shown. Only this view model writes it.
    /// </summary>
    public BindableReactiveProperty<bool> IsGalleryExpanded { get; } = new();

    /// <summary>
    /// Whether the Docked Gallery is shown. Only this view model writes it.
    /// </summary>
    public BindableReactiveProperty<bool> IsDockedGalleryVisible { get; } = new();

    public BindableReactiveProperty<double> LineSpacing { get; } = new(Settings.Gallery.LineSpacing);
    public BindableReactiveProperty<int> SelectedGalleryItemIndex { get; } = new(-1);

    // Whether the Docked Gallery is shown at that edge. Only this view model writes them.
    public BindableReactiveProperty<bool> IsTopDocked { get; } = new();
    public BindableReactiveProperty<bool> IsBottomDocked { get; } = new();
    public BindableReactiveProperty<bool> IsLeftDocked { get; } = new();
    public BindableReactiveProperty<bool> IsRightDocked { get; } = new();

    public GalleryLoadingState LoadingState { get; internal set; }

    /// <summary>
    /// Raised, on the coordinator's queue, once for each batch of items that got their sizes.
    /// </summary>
    public Observable<Unit> SizesChanged => _sizesChanged;

    internal void RaiseSizesChanged() => _sizesChanged.OnNext(Unit.Default);

    public void Initialize(GallerySharedSettingsViewModel shared)
    {
        if (Settings.Gallery is not { } gallery)
        {
            return;
        }

        // The combine below takes over, with the shared inputs
        _appearanceBeforeInitialize?.Dispose();
        _appearanceBeforeInitialize = null;

        // One poll of the dock position for the dock flags and the appearance. The flags subscribe first, so they still
        // take the first value as the poll starts, and the appearance takes it from the Prepend
        var dockPositions = Observable.EveryValueChanged(gallery, g => g.DockPosition).Share();
        dockPositions
            .Subscribe(_ => UpdateDockFlags(), DebugHelper.LogError(nameof(GalleryViewModel), nameof(UpdateDockFlags)))
            .AddTo(ref _disposables);

        // The dock position and the hover bar inputs are read when the record is computed, so a dock move published
        // before the poll sees it, as DockAt does, already has its edge; the poll only says when to compute again
        Observable.CombineLatest(
                Switcher.ShownMode,
                shared.DockedGalleryItemSize,
                shared.ExpandedGalleryItemSize,
                shared.DockedGalleryStretchMode,
                shared.ExpandedGalleryStretchMode,
                dockPositions.Prepend(gallery.DockPosition),
                Observable.EveryValueChanged(gallery, g => g.ItemSpacing).Prepend(gallery.ItemSpacing),
                static (mode, dockedSize, expandedSize, dockedStretch, expandedStretch, _, spacing) =>
                    ComputeAppearance(mode, dockedSize, expandedSize, dockedStretch, expandedStretch, spacing))
            .Subscribe(x => _appearance.Value = x, DebugHelper.LogError(nameof(GalleryViewModel), nameof(Initialize)))
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

    // The dock position is read here, so every caller builds the inputs the same way
    private static GalleryAppearance ComputeAppearance(GalleryMode mode, double dockedSize, double expandedSize,
        GalleryStretchMode dockedStretch, GalleryStretchMode expandedStretch, double spacing)
    {
        return GalleryAppearance.For(mode, Settings.Gallery.DockPosition, new GalleryAppearanceInputs(dockedSize,
            expandedSize, dockedStretch, expandedStretch, spacing));
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
        _appearanceBeforeInitialize?.Dispose();
        Disposable.Dispose(
            ToggleExpandedCommand,
            ToggleDockedCommand,
            ContractCommand,
            CloseCommand,
            DockAtCommand,
            NavigateGalleryCommand,
            Mode,
            ShownMode,
            IsDocked,
            Appearance,
            IsGalleryExpanded,
            IsDockedGalleryVisible,
            LineSpacing,
            SelectedGalleryItemIndex,
            IsTopDocked,
            IsBottomDocked,
            IsLeftDocked,
            IsRightDocked,
            _sizesChanged,
            Switcher
        );
        GC.SuppressFinalize(this);
    }
}
