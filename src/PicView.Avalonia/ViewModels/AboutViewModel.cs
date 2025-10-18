using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Update;
using PicView.Core.Config;
using PicView.Core.Localization;
using R3;

namespace PicView.Avalonia.ViewModels;

public class AboutViewModel : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    
    public BindableReactiveProperty<bool> IsLoading { get; } = new();
    public BindableReactiveProperty<bool> IsHitTestVisible { get; } = new(true);
    public BindableReactiveProperty<double> WindowOpacity { get; } = new(1);
    public BindableReactiveProperty<string?> UpdateStatusText { get; } = new();

    public BindableReactiveProperty<bool> IsUpdateAvailable { get; } = new(true);
    public BindableReactiveProperty<string?> UpdateVersionNumber { get; } = new (VersionHelper.GetCurrentVersion());

    private readonly IPlatformSpecificUpdate _update;

    public AboutViewModel(IPlatformSpecificUpdate update)
    {
        _update = update;
        UpdateCommand = new ReactiveCommand(UpdateCurrentVersion);
        _disposables.Add(UpdateCommand);
        _disposables.Add(UpdateStatusText);
        _disposables.Add(IsUpdateAvailable);
        _disposables.Add(UpdateVersionNumber);
        _disposables.Add(IsLoading);
        _disposables.Add(IsHitTestVisible);
        _disposables.Add(WindowOpacity);
    }
    
    
    public ReactiveCommand UpdateCommand { get; }

    public async ValueTask UpdateCurrentVersion() => await UpdateCurrentVersion(unit: default, cancellationToken: CancellationToken.None);
    public async ValueTask UpdateCurrentVersion(Unit unit, CancellationToken cancellationToken)
    {
        try
        {
            IsLoading.Value = true;
            IsHitTestVisible.Value = false;
            WindowOpacity.Value = 0.2;
            IsUpdateAvailable.Value = await UpdateManager.UpdateCurrentVersion(_update);
            if (!IsUpdateAvailable.Value)
            {
                UpdateStatusText.Value = TranslationManager.Translation.NoUpdateFound;
            }
        }
        finally
        {
            IsLoading.Value = false;
            IsHitTestVisible.Value = true;
            WindowOpacity.Value = 1;
        }
    }
    
    public void Dispose()
    {
        _disposables.Dispose();
        _disposables.Clear();
    }
}