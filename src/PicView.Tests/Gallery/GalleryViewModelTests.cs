using PicView.Core.ViewModels;
using PicView.Core.Config;
using PicView.Core.Gallery;
using Xunit;
using R3;

namespace PicView.Tests.Gallery;

public class GalleryViewModelTests
{
    private readonly ManualFrameProvider _frameProvider;

    public GalleryViewModelTests()
    {
        _frameProvider = new ManualFrameProvider();
        ObservableSystem.DefaultFrameProvider = _frameProvider;
    }

    private class ManualFrameProvider : FrameProvider
    {
        private readonly List<IFrameRunnerWorkItem> _items = new();

        public override long GetFrameCount() => 0;

        public override void Register(IFrameRunnerWorkItem callback)
        {
            _items.Add(callback);
        }

        public void Tick()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (!item.MoveNext(0))
                {
                    _items.RemoveAt(i);
                }
            }
        }
    }

}