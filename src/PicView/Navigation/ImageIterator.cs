using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PicView.Data.Imaging;
using PicView.Data.IO;

namespace PicView.Navigation
{
    public class ImageIterator
    {
        public int FolderIndex { get; private set; }
        public List<string>? Pics { get; }
        public bool Reverse { get; private set; }
        
        public Preloader Preloader { get; }

        public ImageIterator(FileInfo fileInfo)
        {
            Pics = FileListHelper.GetFileList(fileInfo, SortFilesBy.Name) ?? throw new Exception();;
            Preloader = new Preloader();
        }
        
        public async Task<IImage> GetPicFromFileAsync(FileInfo fileInfo)
        {
            return await ImageDecoder.GetPicAsync(fileInfo).ConfigureAwait(false)
                   ?? throw new InvalidOperationException();
        }

        public async Task PreloadAsync()
        {
            await Preloader.PreLoad(
                FolderIndex, Reverse, Pics ?? throw new Exception()).ConfigureAwait(false);
        }

        public Preloader.PreloadValue GetPicAtIndex(int index)
        {
            FolderIndex = index;
            return Preloader.Get(index) ?? throw new InvalidOperationException();
        }
    
        public Preloader.PreloadValue Next()
        {
            return GetPicAtIndex(GetImageIterateIndex(NavigateTo.Next));
        }

        public Preloader.PreloadValue Prev()
        {
            return GetPicAtIndex(GetImageIterateIndex(NavigateTo.Prev));
        }
        
        public Preloader.PreloadValue Last()
        {
            return GetPicAtIndex(GetImageIterateIndex(NavigateTo.Last));
        }

        public Preloader.PreloadValue First()
        {
            return GetPicAtIndex(GetImageIterateIndex(NavigateTo.First));
        }
        
        public int GetImageIterateIndex(NavigateTo navigateTo)
        {
#if DEBUG
            Debug.Assert(Pics != null, nameof(Pics) + " != null");
#endif
            
            var next = FolderIndex;

            switch (navigateTo)
            {
                case NavigateTo.Next:
                    // Go to next if able
                    if (FolderIndex + 1 == Pics?.Count)
                    {
                        return -1;
                    }
                    Reverse = false;
                    return next + 1;
                case NavigateTo.Prev:
                    // Go to prev if able
                    if (next - 1 < 0)
                    {
                        return -1;
                    }
                    Reverse = true;
                    return next - 1;
                case NavigateTo.First:
                    return 0;
                case NavigateTo.Last:
                    return -1;
                default:
                    throw new ArgumentOutOfRangeException(nameof(navigateTo), navigateTo, null);
            }
        }
    }
}

