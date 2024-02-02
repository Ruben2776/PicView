using PicView.Avalonia.Models;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Navigation;
using System.Diagnostics;
using PicView.Avalonia.Services;

namespace PicView.Avalonia.Navigation
{
    public class ImageIterator(FileInfo fileInfo)
    {
        public int Index;
        public List<string> Pics { get; } = FileListHelper.RetrieveFiles(fileInfo).ToList();
        public bool Reverse;

        public PreLoader PreLoader { get; } = new();

        public async Task Preload(ImageService imageService)
        {
            await PreLoader.PreLoadAsync(Index, Pics.Count, true, Reverse, imageService, Pics).ConfigureAwait(false);
        }

        public async Task AddAsync(int index, ImageService imageService, ImageModel imageModel)
        {
            await PreLoader.AddAsync(index, imageService, Pics, imageModel).ConfigureAwait(false);
        }

        public int GetIteration(int index, NavigateTo navigateTo)
        {
            int next;
            switch (navigateTo)
            {
                case NavigateTo.Next:
                case NavigateTo.Previous:
                    var indexChange = navigateTo == NavigateTo.Next ? 1 : -1;
                    Reverse = navigateTo == NavigateTo.Previous;
                    if (SettingsHelper.Settings.UIProperties.Looping)
                    {
                        next = (index + indexChange + Pics.Count) % Pics.Count;
                    }
                    else
                    {
                        var newIndex = index + indexChange;
                        if (newIndex < 0 || newIndex >= Pics.Count)
                            return 0;
                        next = newIndex;
                    }

                    break;

                case NavigateTo.First:
                case NavigateTo.Last:
                    if (Pics.Count > PreLoader.MaxCount)
                        PreLoader.Clear();
                    next = navigateTo == NavigateTo.First ? 0 : Pics.Count - 1;
                    break;

                default: return -1;
            }
            return next;
        }
    }
}