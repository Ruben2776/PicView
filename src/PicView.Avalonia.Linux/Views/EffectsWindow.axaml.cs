using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.WindowBehavior;
using R3;

namespace PicView.Avalonia.Linux.Views;

public partial class EffectsWindow : GenericWindow
{
    private readonly CompositeDisposable _disposables = new();
    public EffectsWindow()
    {
        InitializeComponent();
    }
}