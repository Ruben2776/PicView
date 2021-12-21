

using System.Collections.Concurrent;
using System.Diagnostics;
using Avalonia.Media;
using PicView.Data.Imaging;

namespace PicView.Navigation
{
    /// <summary>
    /// Used for containing a list of BitmapSources
    /// </summary>
    public class Preloader
    {
        public Preloader()
        {
            _sources = new ConcurrentDictionary<int, PreloadValue>();
        }
        public class PreloadValue
        {
            internal IImage? Image;
            internal bool IsLoading;
            internal FileInfo? FileInfo;

            public PreloadValue(IImage? image, bool loading, FileInfo? fileInfo)
            {
                Image = image;
                IsLoading = loading;
                FileInfo = fileInfo;
            }
        }

        /// <summary>
        /// Preloader list of IImages
        /// </summary>
        private readonly ConcurrentDictionary<int, PreloadValue> _sources;

        /// <summary>
        /// Add file to preloader from index. Returns true if new value added.
        /// </summary>
        /// <param name="i">Index of Pics</param>
        /// <param name="pics"></param>
        /// <param name="fileInfo"></param>
        /// <param name="image"></param>
        public async Task<bool> AddAsync(int i, List<string> pics, FileInfo? fileInfo = null, IImage? image = null)
        {
            if (i >= pics.Count) { return false; }

            if (i < 0)
            {
                i = Math.Abs(i);
            }

            var preloadValue = new PreloadValue(null, true, null);

            if (!_sources.TryAdd(i, preloadValue)) { return false; }
            
            await Task.Run(async () =>
            {
                fileInfo ??= new FileInfo(pics[i]);
                image ??= await ImageDecoder.GetPicAsync(fileInfo).ConfigureAwait(false);
                preloadValue.Image = image;
                preloadValue.IsLoading = false;
                preloadValue.FileInfo = fileInfo;
            }).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Removes the key, after checking if it exists
        /// </summary>
        /// <param name="key"></param>
        /// <param name="pics"></param>
        public void Remove(int key, List<string> pics)
        {
            if (key < 0) // Make it load at start of folder, when looping is on
            {
                key = Math.Abs(key);
            }

            else if (key >= pics.Count)
            {
#if DEBUG
                Trace.WriteLine("Preloader " + nameof(Remove) + " key null, " + key);
#endif
                return;
            }

            if (!Contains(key))
            {
                return;
            }

            try
            {
                _ = _sources[key];
#if DEBUG
                if (!_sources.TryRemove(key, out _))
                {
                    Trace.WriteLine($"Failed to Remove {key} from Preloader, index {pics[key]}");
                }
#else
            _sources.TryRemove(key, out _);
#endif
            }
#if DEBUG
            catch (Exception e)
            {
                Trace.WriteLine("Preloader " + nameof(Remove) + "exception" + Environment.NewLine + e.Message);
            }
#else
            catch (Exception)
            {
                
            }
#endif

        }

        public bool Rename(int key, string name)
        {
            if (name == null) { throw new Exception(); }

            return _sources.TryRemove(key, out var preloadValue) && _sources.TryAdd(key, preloadValue);
        }

        /// <summary>
        /// Removes all keys
        /// </summary>
        public void Clear()
        {
            if (_sources.IsEmpty)
            {
                return;
            }

            _sources.Clear();
#if DEBUG
            Trace.WriteLine("Cleared Preloader");
#endif
        }

        /// <summary>
        /// Returns the specified BitmapSource.
        /// Returns null if key not found.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public PreloadValue? Get(int key)
        {
            return !Contains(key) ? null : _sources[key];
        }

        /// <summary>
        /// Checks if the specified key exists
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Contains(int key)
        {
            return !_sources.IsEmpty && _sources.ContainsKey(key);
        }

        /// <summary>
        /// Starts decoding images into memory,
        /// based on current index and if reverse or not
        /// </summary>
        /// <param name="index"></param>
        /// <param name="reverse"></param>
        /// <param name="pics"></param>
        public Task PreLoad(int index, bool reverse, List<string> pics) => Task.Run(async() =>
        {
            var loadInfront = pics.Count >= 10 ? 4 : 2;
            var loadBehind = pics.Count >= 10 ? 3 : 2;

            int endPoint;
            if (reverse)
            {
                endPoint = index - 1 - loadInfront;
                // Add first elements behind
                for (var i = index - 1; i > endPoint; i--)
                {
                    if (pics.Count == 0 || pics.Count == _sources.Count) { return; }
                    await AddAsync(i % pics.Count, pics).ConfigureAwait(false);
                }

                // Add second elements
                for (var i = index + 1; i < (index + 1) + loadBehind; i++)
                {
                    if (pics.Count == 0 || pics.Count == _sources.Count) { return; }
                    await AddAsync(i % pics.Count, pics).ConfigureAwait(false);
                }

                //Clean up in front
                for (var i = (index + 1) + loadBehind; i < (index + 1) + loadInfront; i++)
                {
                    if (pics.Count == 0 || pics.Count == _sources.Count) { return; }
                    Remove(i % pics.Count, pics);
                }
            }
            else
            {
                endPoint = (index - 1) - loadBehind;
                // Add first elements
                for (var i = index + 1; i < (index + 1) + loadInfront; i++)
                {
                    if (pics.Count == 0 || pics.Count == _sources.Count) { return; }
                    await AddAsync(i % pics.Count, pics).ConfigureAwait(false);
                }
                // Add second elements behind
                for (var i = index - 1; i > endPoint; i--)
                {
                    if (pics.Count == 0 || pics.Count == _sources.Count) { return; }
                    await AddAsync(i % pics.Count, pics).ConfigureAwait(false);
                }

                //Clean up behind
                for (var i = index - loadInfront; i <= endPoint; i++)
                {
                    if (pics.Count == 0 || pics.Count == _sources.Count) { return; }
                    Remove(i % pics.Count, pics);
                }
            }
        });
    }
}