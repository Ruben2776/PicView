using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ReactiveUI;
using System.Reactive.Concurrency;

namespace PicView.Avalonia.Views;

public partial class ExifView : UserControl
{
    public ExifView()
    {
        InitializeComponent();

        Loaded += delegate
        {
        };
    }
}