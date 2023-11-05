using System.Windows;

namespace XamlAnimatedGif
{
    public delegate void DownloadProgressEventHandler(DependencyObject d, DownloadProgressEventArgs e);

    public class DownloadProgressEventArgs : RoutedEventArgs
    {
        public int Progress { get; set; }

        public DownloadProgressEventArgs(object source, int progress) : base(AnimationBehavior.DownloadProgressEvent, source)
        {
            Progress = progress;
        }
    }
}
