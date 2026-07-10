using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using R3;

namespace PicView.Avalonia.Linux.Views;

public partial class EffectsWindow : GenericWindow, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    public EffectsWindow(EffectsWindowConfig config)
    {
        InitializeComponent();
        if (!Settings.Theme.Dark || Settings.Theme.GlassTheme)
        {
            XEffectsView.Background = Brushes.Transparent;
        }
        Loaded += delegate
        {
            MinWidth = MaxWidth = Bounds.Width;
            Title = StringExtensions.CombineWithAppName(TranslationManager.Translation.Effects);

            ClientSizeProperty.Changed.ToObservable()
                .ObserveOn(UIHelper.GetFrameProvider)
                .Subscribe(size => { WindowResizing.HandleWindowResize(this, size); })
                .AddTo(_disposables);
            PositionChanged += (_, _) =>
            {
                config.WindowProperties.Left = Position.X;
                config.WindowProperties.Top = Position.Y;
            };
        };
        KeyDown += (_, e) =>
        {
            if (e.Key is Key.Escape)
            {
                e.Handled = true;
                MainKeyboardShortcuts.IsEscKeyEnabled = false;
                Close();
            }
        };
    }
    
    public void Dispose()
    {
        Disposable.Dispose(_disposables);
        GC.SuppressFinalize(this);
    }
}