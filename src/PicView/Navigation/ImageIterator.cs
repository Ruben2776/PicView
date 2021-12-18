using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PicView.Data.Imaging;
using PicView.Data.IO;
using PicView.Data.Sizing;

namespace PicView.Navigation
{
    public class ImageIterator
    {
        public int FolderIndex { get; set; }
        public List<string> Pics { get; }
        private bool Reverse { get; set; }
        
        private Preloader Preloader { get; }

        public class Values
        {
            public IImage? Image { get; set; }
            public double[]? Sizes { get; }
            public string[] Titles { get; }
            
            public FileInfo FileInfo { get; }

            public Values(IImage? image, double[]? sizes, string[] titles, FileInfo fileInfo)
            {
                Image = image;
                Sizes = sizes;
                Titles = titles;
                FileInfo = fileInfo;
            }
        }

        public ImageIterator(FileInfo fileInfo)
        {
            Pics = FileListHelper.GetFileList(fileInfo, SortFilesBy.Name) ?? throw new Exception();;
            Preloader = new Preloader();
        }
        
        public async Task<Values?> GetValuesFromFileAsync(FileInfo fileInfo)
        {
            var index = Pics.IndexOf(fileInfo.FullName);
            var added = await Preloader.AddAsync(index, Pics, fileInfo).ConfigureAwait(false);
            if (added is false)
            {
                // TODO error handling?
            }
            return GetValuesAtIndex(index);
        }

        public async Task PreloadAsync()
        {
            await Preloader.PreLoad(
                FolderIndex, Reverse, Pics ?? throw new Exception()).ConfigureAwait(false);
        }

        public Values? GetValuesAtIndex(int index)
        {
            FolderIndex = index;

            var preloadValue = Preloader.Get(index);
            if (Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                preloadValue?.Image is null)
            {
                return null;
            }

            var size =
                ImageSizeHelper.GetScaledImageSize(
                    preloadValue.Image.Size.Width, preloadValue.Image.Size.Height, 0,
                    desktop.MainWindow);
            var titles = Data.TextData.TitleHelper.TitleString(
                (int)preloadValue.Image.Size.Width, (int)preloadValue.Image.Size.Height, index,
                preloadValue.FileInfo, Pics);
            return new Values(preloadValue.Image, size, titles, preloadValue.FileInfo ?? new FileInfo(Pics[index]));
        }

        public Values? GetValues(NavigateTo navigateTo)
        {
            return GetValuesAtIndex(GetIteration(navigateTo));
        }
        
        private int GetIteration(NavigateTo navigateTo)
        {
#if DEBUG
            Debug.Assert(Pics != null, nameof(Pics) + " != null");
#endif
            switch (navigateTo)
            {
                case NavigateTo.Next:
                    Reverse = false;
                    
                    if (true) // TODO replace with looping settings
                    {
                        return FolderIndex == Pics.Count - 1 ? 0 : FolderIndex + 1;
                    }
                    if (FolderIndex + 1 >= Pics?.Count)
                    {
                        return Pics.Count - 1;
                    }
                    return FolderIndex + 1;
                case NavigateTo.Prev:
                    Reverse = true;
                    
                    if (true) // TODO replace with looping settings
                    {
                        return FolderIndex == 0 ? Pics.Count - 1 : FolderIndex - 1;
                    }
                    if (FolderIndex - 1 < 0)
                    {
                        return 0;
                    }
                    return FolderIndex - 1;
                case NavigateTo.First:
                    return 0;
                case NavigateTo.Last:
                    return Pics.Count - 1;
                default:
                    throw new ArgumentOutOfRangeException(nameof(navigateTo), navigateTo, null);
            }
        }
    }
}

