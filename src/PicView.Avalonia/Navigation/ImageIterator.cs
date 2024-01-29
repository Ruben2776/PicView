using PicView.Avalonia.Models;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Navigation;
using System.Diagnostics;

namespace PicView.Avalonia.Navigation
{
    public class ImageIterator(FileInfo fileInfo)
    {
        public int Index;
        public List<string> Pics { get; } = FileListHelper.RetrieveFiles(fileInfo).ToList();
        public bool Reverse;

        private PreLoader PreLoader { get; } = new();

        public async Task<ImageModel?> GetImageModelAsync(int index, NavigateTo navigateTo)
        {
            var next = GetIteration(index, navigateTo);
            if (next < 0)
                throw new InvalidOperationException("Invalid iteration");
            Index = next;
            var x = 0;

            var preLoadValue = PreLoader.Get(next, Pics);
            if (preLoadValue is null)
            {
                await GetPreload();
            }

            while (preLoadValue.IsLoading)
            {
                x++;
                await Task.Delay(20);
                if (Index != next)
                {
                    throw new TaskCanceledException();
                }

                if (x > 100)
                {
                    await GetPreload();
#if DEBUG
                    Trace.WriteLine("Loading timeout");
#endif
                    break;
                }
            }

            return preLoadValue.ImageModel;

            async Task GetPreload()
            {
                await PreLoader.AddAsync(next, Pics).ConfigureAwait(false);
                preLoadValue = PreLoader.Get(next, Pics);
                if (Index != next)
                {
                    throw new TaskCanceledException();
                }

                if (preLoadValue is null)
                {
                    throw new ArgumentNullException();
                }
            }
        }

        public async Task Preload()
        {
            await PreLoader.PreLoadAsync(Index, Pics.Count, true, Reverse, Pics).ConfigureAwait(false);
        }

        public async Task AddAsync(int index, ImageModel imageModel)
        {
            await PreLoader.AddAsync(index, Pics, imageModel).ConfigureAwait(false);
        }

        private int GetIteration(int index, NavigateTo navigateTo)
        {
            int next;
            var prev = Index;
            switch (navigateTo)
            {
                case NavigateTo.Next:
                case NavigateTo.Previous:
                    var indexChange = navigateTo == NavigateTo.Next ? 1 : -1;
                    Reverse = navigateTo == NavigateTo.Previous;
                    if (SettingsHelper.Settings.UIProperties.Looping)
                    {
                        next = (prev + indexChange + Pics.Count) % Pics.Count;
                    }
                    else
                    {
                        var newIndex = prev + indexChange;
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