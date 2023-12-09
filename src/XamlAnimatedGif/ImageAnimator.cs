using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using XamlAnimatedGif.Decoding;

namespace XamlAnimatedGif;

internal class ImageAnimator : Animator
{
    private readonly Image _image;

    public ImageAnimator(Stream sourceStream, Uri sourceUri, GifDataStream metadata, RepeatBehavior repeatBehavior,
        Image image) : this(sourceStream, sourceUri, metadata, repeatBehavior, image, false)
    {
    }

    public ImageAnimator(Stream sourceStream, Uri sourceUri, GifDataStream metadata, RepeatBehavior repeatBehavior,
        Image image, bool cacheFrameDataInMemory) : base(sourceStream, sourceUri, metadata, repeatBehavior,
        cacheFrameDataInMemory)
    {
        _image = image;
        OnRepeatBehaviorChanged(); // in case the value has changed during creation
    }

    protected override RepeatBehavior GetSpecifiedRepeatBehavior()
    {
        return AnimationBehavior.GetRepeatBehavior(_image);
    }

    protected override object AnimationSource => _image;

    public static Task<ImageAnimator> CreateAsync(Uri sourceUri, RepeatBehavior repeatBehavior,
        IProgress<int> progress, Image image)
    {
        return CreateAsync(sourceUri, repeatBehavior, progress, image, false);
    }

    public static Task<ImageAnimator> CreateAsync(Uri sourceUri, RepeatBehavior repeatBehavior,
        IProgress<int> progress, Image image, bool cacheFrameDataInMemory)
    {
        return CreateAsyncCore(
            sourceUri,
            progress,
            (stream, metadata) => new ImageAnimator(stream, sourceUri, metadata, repeatBehavior, image,
                cacheFrameDataInMemory));
    }

    public static Task<ImageAnimator> CreateAsync(Stream sourceStream, RepeatBehavior repeatBehavior, Image image)
    {
        return CreateAsync(sourceStream, repeatBehavior, image);
    }

    public static Task<ImageAnimator> CreateAsync(Stream sourceStream, RepeatBehavior repeatBehavior, Image image,
        bool cacheFrameDataInMemory)
    {
        return CreateAsyncCore(
            sourceStream,
            metadata => new ImageAnimator(sourceStream, null, metadata, repeatBehavior, image,
                cacheFrameDataInMemory));
    }
}