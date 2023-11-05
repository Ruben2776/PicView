using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Animation;
using XamlAnimatedGif.Decoding;

namespace XamlAnimatedGif
{
    public class BrushAnimator : Animator
    {
        private BrushAnimator(Stream sourceStream, Uri sourceUri, GifDataStream metadata, RepeatBehavior repeatBehavior, bool cacheFrameDataInMemory, CancellationToken cancellationToken) : base(sourceStream, sourceUri, metadata, repeatBehavior, cacheFrameDataInMemory, cancellationToken)
        {
            Brush = new ImageBrush { ImageSource = Bitmap };
            RepeatBehavior = _repeatBehavior;
        }

        protected override RepeatBehavior GetSpecifiedRepeatBehavior() => RepeatBehavior;

        protected override object AnimationSource => Brush;

        public ImageBrush Brush { get; }

        private RepeatBehavior _repeatBehavior;

        public RepeatBehavior RepeatBehavior
        {
            get { return _repeatBehavior; }
            set
            {
                _repeatBehavior = value;
                OnRepeatBehaviorChanged();
            }
        }

        public static Task<BrushAnimator> CreateAsync(Uri sourceUri, RepeatBehavior repeatBehavior, IProgress<int> progress = null)
        {
            return CreateAsync(sourceUri, repeatBehavior, false, CancellationToken.None, progress);
        }

        public static Task<BrushAnimator> CreateAsync(Uri sourceUri, RepeatBehavior repeatBehavior, bool cacheFrameDataInMemory, CancellationToken cancellationToken, IProgress<int> progress = null)
        {
            return CreateAsyncCore(
                sourceUri,
                progress,
                (stream, metadata) => new BrushAnimator(stream, sourceUri, metadata, repeatBehavior, cacheFrameDataInMemory, cancellationToken));
        }

        public static Task<BrushAnimator> CreateAsync(Stream sourceStream, RepeatBehavior repeatBehavior)
        {
            return CreateAsync(sourceStream, repeatBehavior, false, CancellationToken.None);
        }

        public static Task<BrushAnimator> CreateAsync(Stream sourceStream, RepeatBehavior repeatBehavior, bool cacheFrameDataInMemory, CancellationToken cancellationToken)
        {
            return CreateAsyncCore(
                sourceStream,
                metadata => new BrushAnimator(sourceStream, null, metadata, repeatBehavior, cacheFrameDataInMemory, cancellationToken));
        }
    }
}