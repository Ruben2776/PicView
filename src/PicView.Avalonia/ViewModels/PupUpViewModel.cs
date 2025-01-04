using System.Reactive;
using ReactiveUI;

namespace PicView.Avalonia.ViewModels;

public class PopUpViewModel : ReactiveObject
{
    public bool IsOpen
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    // Command to toggle popup
    public ReactiveCommand<Unit, Unit> TogglePopupCommand { get; }

    public PopUpViewModel()
    {
        TogglePopupCommand = ReactiveCommand.Create(() =>
        {
            IsOpen = !IsOpen;
        });
    }
}