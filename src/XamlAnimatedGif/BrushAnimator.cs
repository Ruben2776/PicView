using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Animation;
using XamlAnimatedGif.Decoding;

namespace XamlAnimatedGif;

public class BrushAnimator : Animator
{
    private BrushAnimator(Stream sourceStream, Uri sourceUri, GifDataStream metadata, RepeatBehavior repeatBehavior,
        bool cacheFrameDataInMemory) : base(sourceStream, sourceUri, metadata, repeatBehavior,
        cacheFrameDataInMemory)
    {
        Brush = new ImageBrush { ImageSource = Bitmap };
        RepeatBehavior = _repeatBehavior;
    }

    protected override RepeatBehavior GetSpecifiedRepeatBehavior()
    {
        return RepeatBehavior;
    }

    protected override object AnimationSource => Brush;

    public ImageBrush Brush { get; }

    private RepeatBehavior _repeatBehavior;

    public RepeatBehavior RepeatBehavior
    {
        get => _repeatBehavior;
        set
        {
            _repeatBehavior = value;
            OnRepeatBehaviorChanged();
        }
    }

    public static Task<BrushAnimator> CreateAsync(Uri sourceUri, RepeatBehavior repeatBehavior,
        IProgress<int> progress = null)
    {
        return CreateAsync(sourceUri, repeatBehavior, false, progress);
    }

    public static Task<BrushAnimator> CreateAsync(Uri sourceUri, RepeatBehavior repeatBehavior,
        bool cacheFrameDataInMemory, IProgress<int> progress = null)
    {
        return CreateAsyncCore(
            sourceUri,
            progress,
            (stream, metadata) =>
                new BrushAnimator(stream, sourceUri, metadata, repeatBehavior, cacheFrameDataInMemory));
    }

    public static Task<BrushAnimator> CreateAsync(Stream sourceStream, RepeatBehavior repeatBehavior)
    {
        return CreateAsync(sourceStream, repeatBehavior, false);
    }

    public static Task<BrushAnimator> CreateAsync(Stream sourceStream, RepeatBehavior repeatBehavior,
        bool cacheFrameDataInMemory)
    {
        return CreateAsyncCore(
            sourceStream,
            metadata => new BrushAnimator(sourceStream, null, metadata, repeatBehavior, cacheFrameDataInMemory));
    }
}