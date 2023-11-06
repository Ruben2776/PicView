using System.Windows.Media.Imaging;

namespace XamlAnimatedGif.Extensions;

internal static class WritableBitmapExtensions
{
    public static IDisposable LockInScope(this WriteableBitmap bitmap)
    {
        return new WriteableBitmapLock(bitmap);
    }

    private class WriteableBitmapLock : IDisposable
    {
        private readonly WriteableBitmap _bitmap;

        public WriteableBitmapLock(WriteableBitmap bitmap)
        {
            _bitmap = bitmap;
            _bitmap.Lock();
        }

        public void Dispose()
        {
            _bitmap.Unlock();
        }
    }
}