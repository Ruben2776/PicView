using Avalonia.Media;
using Avalonia.Media.Imaging;
using PicView.Data.Imaging;
using PicView.Data.IO;

namespace PicView.Navigation
{
    public class ImageIterator
    {
        public static int FolderIndex { get; private set; }
        static List<string> Pics { get; set; }
        
        public ImageIterator()
        {
            Pics = new List<string>();
        }

        public ImageIterator(FileInfo fileInfo)
        {
            Pics = FileListHelper.GetFileList(fileInfo, SortFilesBy.Name) ?? throw new NotImplementedException();;
        }
        
        public async Task<IImage> GetPicFromFileAsync(FileInfo fileInfo)
        {
            return await ImageDecoder.GetPicAsync(fileInfo).ConfigureAwait(false) ?? throw new NotImplementedException();;
        }

        public async Task<IImage> GetPicAtIndex(int index)
        {
            FolderIndex = index;
            return await ImageDecoder.GetPicAsync(new FileInfo(Pics[index])).ConfigureAwait(false) ?? throw new NotImplementedException();;
        }
    
        public async Task<IImage>  Next()
        {
            return await GetPicAtIndex(GetImageIterateIndex()).ConfigureAwait(false);
        }

        public async Task<IImage>  Prev()
        {
            return await GetPicAtIndex(GetImageIterateIndex(false)).ConfigureAwait(false);
        }
        
        public async Task<IImage>  Last()
        {
            return await GetPicAtIndex(GetImageIterateIndex(false, true)).ConfigureAwait(false);
        }

        public async Task<IImage>  First()
        {
            return await GetPicAtIndex(GetImageIterateIndex(true, true)).ConfigureAwait(false);
        }
        
        private static int GetImageIterateIndex(bool forward = true, bool end = false)
        {
            var next = FolderIndex;

            if (end) // Go to first or last
            {
                next = forward ? Pics.Count - 1 : 0;
            }
            else // Go to next or previous
            {
                if (forward)
                {
                    // Go to next if able
                    if (FolderIndex + 1 == Pics?.Count)
                    {
                        return -1;
                    }

                    next++;
                }
                else
                {
                    // Go to prev if able
                    if (next - 1 < 0)
                    {
                        return -1;
                    }

                    next--;
                }
            }

            return next;
        }
    }
}

