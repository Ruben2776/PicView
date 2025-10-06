using R3;

namespace PicView.Core.ViewModels;

public class ConvertToViewModel : IDisposable
{
    public BindableReactiveProperty<bool> IsCopyButtonEnabled { get; } = new();

    public void Dispose()
    {
        Disposable.Dispose(
            IsCopyButtonEnabled);
    }
}