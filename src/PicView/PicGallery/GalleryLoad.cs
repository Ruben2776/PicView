using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.UILogic;
using PicView.Views.UserControls.Gallery;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PicView.PicGallery
{
    internal static class GalleryLoad
    {
        /// <summary>
        /// Gets or sets a value indicating whether the gallery is currently loading.
        /// </summary>
        internal static bool IsLoading { get; set; }

        /// <summary>
        /// Event handler for the PicGallery Loaded event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        internal static void PicGallery_Loaded(object sender, RoutedEventArgs e)
        {
            // Add events and set fields when it's loaded.
            UC.GetPicGallery.grid.MouseLeftButtonDown += (_, _) => ConfigureWindows.GetMainWindow.Focus();
            UC.GetPicGallery.x2.MouseLeftButtonDown += (_, _) => GalleryToggle.CloseHorizontalGallery();
        }

        /// <summary>
        /// Represents the data for a gallery thumbnail.
        /// </summary>
        internal class GalleryThumbHolder
        {
            /// <summary>
            /// Gets or sets the file location of the thumbnail.
            /// </summary>
            internal string FileLocation { get; set; }

            /// <summary>
            /// Gets or sets the file name of the thumbnail.
            /// </summary>
            internal string FileName { get; set; }

            /// <summary>
            /// Gets or sets the file size of the thumbnail.
            /// </summary>
            internal string FileSize { get; set; }

            /// <summary>
            /// Gets or sets the file date of the thumbnail.
            /// </summary>
            internal string FileDate { get; set; }

            /// <summary>
            /// Gets or sets the bitmap source of the thumbnail.
            /// </summary>
            internal BitmapSource BitmapSource { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="GalleryThumbHolder"/> class.
            /// </summary>
            /// <param name="fileLocation">The file location of the thumbnail.</param>
            /// <param name="fileName">The file name of the thumbnail.</param>
            /// <param name="fileSize">The file size of the thumbnail.</param>
            /// <param name="fileDate">The file date of the thumbnail.</param>
            /// <param name="bitmapSource">The bitmap source of the thumbnail.</param>
            internal GalleryThumbHolder(string fileLocation, string fileName, string fileSize, string fileDate,
                BitmapSource bitmapSource)
            {
                FileLocation = fileLocation;
                FileName = fileName;
                FileSize = fileSize;
                FileDate = fileDate;
                BitmapSource = bitmapSource;
            }

            /// <summary>
            /// Gets thumbnail data for the specified index.
            /// </summary>
            /// <param name="index">The index of the thumbnail.</param>
            /// <returns>The <see cref="GalleryThumbHolder"/> instance containing thumbnail data.</returns>
            internal static GalleryThumbHolder GetThumbData(int index)
            {
                var fileInfo = new FileInfo(Navigation.Pics[index]);
                var bitmapSource = Thumbnails.GetBitmapSourceThumb(Navigation.Pics[index],
                    (int)GalleryNavigation.PicGalleryItemSize, fileInfo);
                var fileLocation = fileInfo.FullName;
                var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                var getFileSizeResource = Application.Current?.TryFindResource("FileSize");
                var getFileDateResource = Application.Current?.TryFindResource("Modified");
                var fileSize = "";
                var fileDate = "";
                if (getFileSizeResource != null)
                {
                    fileSize = $"{getFileSizeResource}: {fileInfo.Length.GetReadableFileSize()}";
                }

                if (getFileDateResource != null)
                {
                    fileDate =
                        $"{getFileDateResource}: {fileInfo.LastWriteTimeUtc.ToString(CultureInfo.CurrentCulture)}";
                }

                return new GalleryThumbHolder(fileLocation, fileName, fileSize, fileDate, bitmapSource);
            }
        }

        /// <summary>
        /// Asynchronously loads the gallery.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        internal static async Task LoadAsync()
        {
            if (UC.GetPicGallery is null || IsLoading)
            {
                return;
            }

            IsLoading = true;
            var source = new CancellationTokenSource();

            await Task.Run(async () =>
            {
                for (var i = 0; i < Navigation.Pics.Count; i++)
                {
                    try
                    {
                        var i1 = i;
                        await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
                        {
                            if (!IsLoading)
                            {
                                source.Cancel();
                                source.Dispose();
                                return;
                            }

                            Add(i1);
                        }, DispatcherPriority.DataBind, source.Token);
                    }
                    catch (Exception exception)
                    {
#if DEBUG
                        Trace.WriteLine($"{nameof(LoadAsync)}  exception:\n{exception.Message}");
#endif
                        GalleryFunctions.Clear();
                        return;
                    }
                }

                if (source.IsCancellationRequested)
                {
                    return;
                }

                var startPosition = 0;
                var updates = 0;

                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    if (GalleryNavigation.HorizontalItems is 0 || Navigation.Pics.Count is 0)
                    {
                        source.Cancel();
                        source.Dispose();
                        return;
                    }

                    GalleryNavigation.SetSelected(Navigation.FolderIndex, true);
                    GalleryNavigation.SelectedGalleryItem = Navigation.FolderIndex;
                    GalleryNavigation.ScrollToGalleryCenter();

                    startPosition = (Navigation.FolderIndex - GalleryNavigation.HorizontalItems) %
                                    Navigation.Pics.Count;
                    startPosition = startPosition < 0 ? 0 : startPosition;
                }, DispatcherPriority.DataBind, source.Token);

                try
                {
                    _ = Task.Run(async () =>
                    {
                        ParallelOptions options = new()
                        {
                            CancellationToken = source.Token,
                            MaxDegreeOfParallelism = Environment.ProcessorCount - 1 < 1 ? 1 : Environment.ProcessorCount - 1
                        };

                        await Parallel.ForAsync(startPosition, Navigation.Pics.Count, options, async (i, loopState) =>
                        {
                            updates++;
                            try
                            {
                                if (!IsLoading || Navigation.Pics.Count < Navigation.FolderIndex || Navigation.Pics.Count < 1 || i > Navigation.Pics.Count)
                                {
                                    IsLoading = false;
                                    await source.CancelAsync().ConfigureAwait(false);
                                    source.Dispose();
                                    loopState.ThrowIfCancellationRequested();
                                    return;
                                }

                                if (!source.IsCancellationRequested)
                                {
                                    await UpdateThumbAsync(i, updates, source.Token).ConfigureAwait(false);
                                }
                            }
                            catch (ObjectDisposedException exception)
                            {
#if DEBUG
                                Trace.WriteLine($"{nameof(LoadAsync)}  exception:\n{exception.Message}");
#endif
                                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                                if (ConfigureWindows.GetMainWindow is null)
                                {
                                    Environment.Exit(0);
                                }
                                return;
                            }
                            catch (Exception exception)
                            {
#if DEBUG
                                Trace.WriteLine($"{nameof(LoadAsync)}  exception:\n{exception.Message}");
#endif
                                return;
                            }
                        });
                    }, source.Token).ConfigureAwait(false);

                    _ = Task.Run(async () =>
                    {
                        for (var x = startPosition - 1; x >= 0; x--)
                        {
                            updates++;
                            try
                            {
                                var i = x;

                                if (!IsLoading || Navigation.Pics.Count < Navigation.FolderIndex || Navigation.Pics.Count < 1 || i > Navigation.Pics.Count)
                                {
                                    IsLoading = false;
                                    await source.CancelAsync().ConfigureAwait(false);
                                    source.Dispose();
                                    return;
                                }

                                if (!source.IsCancellationRequested)
                                {
                                    await UpdateThumbAsync(i, updates, source.Token).ConfigureAwait(false);
                                }
                            }
                            catch (ObjectDisposedException exception)
                            {
#if DEBUG
                                Trace.WriteLine($"{nameof(LoadAsync)}  exception:\n{exception.Message}");
#endif
                                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                                if (ConfigureWindows.GetMainWindow is null)
                                {
                                    Environment.Exit(0);
                                }
                                return;
                            }
                            catch (Exception exception)
                            {
#if DEBUG
                                Trace.WriteLine($"{nameof(LoadAsync)}  exception:\n{exception.Message}");
#endif
                                return;
                            }
                        }
                    }, source.Token).ConfigureAwait(false);
                }
                catch (ObjectDisposedException exception)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(LoadAsync)}  exception:\n{exception.Message}");
#endif
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (ConfigureWindows.GetMainWindow is null)
                    {
                        Environment.Exit(0);
                    }
                    return;
                }
                catch (Exception exception)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(LoadAsync)}  exception:\n{exception.Message}");
#endif
                    return;
                }
            }, source.Token);
            return;

            async Task UpdateThumbAsync(int i, int updates, CancellationToken token)
            {
                var galleryThumbHolderItem =
                    await Task.FromResult(GalleryThumbHolder.GetThumbData(i)).ConfigureAwait(false);
                await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
                {
                    UpdatePic(i, galleryThumbHolderItem.BitmapSource, galleryThumbHolderItem.FileLocation,
                        galleryThumbHolderItem.FileName, galleryThumbHolderItem.FileSize,
                        galleryThumbHolderItem.FileDate);
                }, DispatcherPriority.Background, token);
                if (updates >= Navigation.Pics.Count)
                    IsLoading = false;
            }
        }

        internal static async Task ReloadGalleryAsync()
        {
            while (IsLoading)
            {
                await Task.Delay(200).ConfigureAwait(false);
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
                {
                    if (UC.GetPicGallery.Container.Children.Count is 0)
                    {
                        IsLoading = false;
                    }
                });
            }

            await LoadAsync().ConfigureAwait(false);
        }

        internal static void Add(int i)
        {
            var selected = i == Navigation.FolderIndex;
            var item = new PicGalleryItem(null, i, selected);
            item.MouseLeftButtonUp += async delegate { await GalleryClick.ClickAsync(i).ConfigureAwait(false); };

            UC.GetPicGallery.Container.Children.Add(item);
        }

        internal static void UpdatePic(int i, BitmapSource? pic, string fileLocation, string fileName, string fileSize,
            string fileDate)
        {
            try
            {
                if (Navigation.Pics?.Count < Navigation.FolderIndex || Navigation.Pics?.Count < 1 ||
                    i >= UC.GetPicGallery.Container.Children.Count)
                {
                    GalleryFunctions.Clear();
                    return;
                }

                var item = (PicGalleryItem)UC.GetPicGallery.Container.Children[i];
                item.ThumbImage.Source = pic;
                item.MouseEnter += delegate { item.Popup.IsOpen = true; };
                item.MouseLeave += delegate { item.Popup.IsOpen = false; };
                item.ThumbFileLocation.Text = fileLocation;
                item.ThumbFileName.Text = fileName;
                item.ThumbFileSize.Text = fileSize;
                item.ThumbFileDate.Text = fileDate;

                item.AddContextMenu();
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e.Message);
#endif
                // Suppress task cancellation
            }
        }
    }
}