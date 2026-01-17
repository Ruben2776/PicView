using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using R3;

namespace PicView.Avalonia.ViewModels;

// TODO: Move this to Core by using interfaces
public class NavigationViewModel : IDisposable
{
    // Reload
    public ReactiveCommand ReloadCommand { get; } = new(async (_, _) =>
    {
        await ErrorHandling.ReloadAsync(UIHelper.GetMainView.DataContext as MainViewModel).ConfigureAwait(false);
    });
    
    // Next
    public ReactiveCommand NextCommand { get; } = new(Next);

    private static async ValueTask Next(Unit unit, CancellationToken token) =>
        await NavigationManager.Iterate(true, token);
    public ReactiveCommand NextFolderCommand { get; } = new(async (_, _) =>
    {
        await NavigationManager.NavigateBetweenDirectories(next: true).ConfigureAwait(false);
    });

    public ReactiveCommand NextArchiveCommand { get; } = new(async (_, _) =>
    {
        await NavigationManager.NavigateBetweenArchives(true).ConfigureAwait(false);
    });
    
    // Prev
    public ReactiveCommand PreviousCommand { get; } = new(Prev);

    private static async ValueTask Prev(Unit unit, CancellationToken token) =>
        await NavigationManager.Iterate(false, token);

    public ReactiveCommand PreviousFolderCommand { get; } = new(async (_, _) =>
    {
        await NavigationManager.NavigateBetweenDirectories(next: false).ConfigureAwait(false);
    });

    public ReactiveCommand PreviousArchiveCommand { get; } = new(async (_, _) =>
    {
        await NavigationManager.NavigateBetweenArchives(false).ConfigureAwait(false);
    });
    
    // Skip
    public ReactiveCommand FirstCommand { get; } = new(async (_, _) =>
    {
        await NavigationManager.NavigateFirstOrLast(last: false).ConfigureAwait(false);
    });
    public ReactiveCommand LastCommand { get; } = new(async (_, _) =>
    {
        await NavigationManager.NavigateFirstOrLast(last: true).ConfigureAwait(false);
    });
    public ReactiveCommand Skip10Command { get; } = new(async (_, _) =>
    {
        await NavigationManager.NavigateIncrements(next: true, true, false).ConfigureAwait(false);
    });
    public ReactiveCommand Skip100Command { get; } = new(async (_, _) =>
    {
        await NavigationManager.NavigateIncrements(next: true, false, true).ConfigureAwait(false);
    });
    public ReactiveCommand Prev10Command { get; } = new(async (_, _) =>
    {
        await NavigationManager.NavigateIncrements(next: false, true, false).ConfigureAwait(false);
    });

    public ReactiveCommand Prev100Command { get; } = new(async (_, _) =>
    {
        await NavigationManager.NavigateIncrements(next: false, false, true).ConfigureAwait(false);
    });

    public ReactiveCommand<string> LoadFileFromStringCommand { get; } = new(async (value, _) =>
    {
        await NavigationManager.LoadPicFromFile(value, UIHelper.GetMainView.DataContext as MainViewModel)
            .ConfigureAwait(false);
    });
    
    public void Dispose()
    {
        Disposable.Dispose(ReloadCommand,
            NextCommand,
            NextFolderCommand,
            PreviousCommand,
            PreviousFolderCommand,
            FirstCommand,
            LastCommand,
            Skip10Command,
            Prev10Command,
            Skip100Command,
            Prev100Command);
    }
}