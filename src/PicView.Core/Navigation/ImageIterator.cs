using PicView.Core.FileHistory;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation;

public class ImageIterator(IImageCache cache, IThumbnailCache thumbCache, IThumbnailLoader thumbnailLoader, TabViewModel tab) : IImageIterator
{
    public IImageCache Cache { get; } = cache ?? throw new ArgumentNullException(nameof(cache));
    public string? CurrentDirectory => Files.Count > 0 ? Files[0].DirectoryName : null;

    private readonly IThumbnailCache _thumbCache = thumbCache ?? throw new ArgumentNullException(nameof(thumbCache));
    private readonly TabViewModel _tab = tab ?? throw new ArgumentNullException(nameof(tab));
    private readonly IThumbnailLoader _thumbnailLoader = thumbnailLoader ?? throw new ArgumentNullException(nameof(thumbnailLoader));
    private DateTime _lastRepeatTime = DateTime.MinValue;

    public IReadOnlyList<FileInfo> Files { get; set; } = [];
    public int CurrentIndex { get; private set; } = -1;
    public int SecondaryCurrentIndex { get; private set; } = -1;
    public bool IsReversed { get; private set; }

    public void Initialize(IReadOnlyList<FileInfo> files, int initialIndex = 0)
    {
        Files = files ?? [];
        CurrentIndex = initialIndex;
        UpdateNavigationProperties();
    }
    
    #region Core Navigation Logic
    
    public void UpdateNavigationProperties()
        => UpdateNavigationProperties(CurrentIndex, Files.Count);

    private void UpdateNavigationProperties(int index, int count)
    {
        if (count <= 1)
        {
            _tab.CanNavigateForwards.Value = false;
            _tab.CanNavigateBackwards.Value = false;
        }
        else
        {
            var isLooping = Settings.UIProperties.Looping;
            if (Settings.ImageScaling.ShowImageSideBySide)
            {
                _tab.CanNavigateForwards.Value = isLooping || index < count - 2;
            }
            else
            {
                _tab.CanNavigateForwards.Value = isLooping || index < count - 1;
            }

            _tab.CanNavigateBackwards.Value = isLooping || index > 0;
        }
        _tab.NavigationIndex.Value = index;
        _tab.MaxIndex.Value = count;
    }

    public async ValueTask NavigateAsync(NavigateTo navigateTo, SkipAmount skipAmount, CancellationTokenSource ct)
    {
        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            var (currentIndex, secondaryIndex, isReversed) = IterationHelper.GetIterations(CurrentIndex, Files.Count, navigateTo, skipAmount);
            IsReversed = isReversed;
            await IterateToIndicesAsync(currentIndex, secondaryIndex, ct).ConfigureAwait(false);
        }
        else
        {
            var (iteration, isReversed) = IterationHelper.GetIteration(CurrentIndex, Files.Count, navigateTo, skipAmount);
            IsReversed = isReversed;
            await IterateToIndexAsync(iteration, ct).ConfigureAwait(false);
        }
    }

    public async ValueTask IterateToIndexAsync(int index, CancellationTokenSource ct)
    {
        if (index < 0 || index >= Files.Count)
        {
            return;
        }
        
        
        // Handle internal TIFF navigation
        if (_tab.Model?.TiffNavigation is not null && ShouldNavigateTiffEntry(_tab.Model, IsReversed))
        {
            return;
        }

        CurrentIndex = index;
        var targetFile = Files[index];
        if (Cache.TryGet(targetFile, out var preLoadValue))
        {
            if (preLoadValue is { IsLoading: false, ImageModel.Image: not null })
            {
                // Is in cache
                UpdateModel(preLoadValue.ImageModel);
            }
            else
            {
                // Is loading in cache, show thumbnail while loading
                await Task.Run(async () =>
                {
                    var thumb = _thumbCache.TryGet(targetFile.FullName, out var cachedThumb)
                        ? cachedThumb
                        : _thumbnailLoader.GetExifThumbnail(targetFile);

                    _tab.Image.Value = thumb;
                    _tab.SetLoading();

                    // Wait for loading complete
                    var successfullyLoaded = await Cache
                        .WaitForLoadingCompleteAsync(_tab.Id, index, _tab.ImageIterator.Files, ct.Token)
                        .ConfigureAwait(false);
                    if (successfullyLoaded && index == CurrentIndex)
                    {
                        if (preLoadValue.ImageModel.Image is null)
                        {
                            await AttemptManualLoad().ConfigureAwait(false);
                        }
                        else
                        {
                            UpdateModel(preLoadValue.ImageModel);
                        }
                    }
                    else
                    {
                        if (index != CurrentIndex)
                        {
                            await IterateToIndexAsync(CurrentIndex, ct).ConfigureAwait(false);
                            return;
                        }

                        TriggerPreload();
                    }
                }, ct.Token).ConfigureAwait(false);
            }
        }
        else
        {
            // Not in cache
            await Task.Run(AttemptManualLoad).ConfigureAwait(false);
        }
        
        return;
        
        async Task AttemptManualLoad()
        {
            var manuallyLoaded = await Cache.LoadAsync(_tab.Id, index, Files, ct.Token).ConfigureAwait(false);
            if (index == CurrentIndex && manuallyLoaded is not null)
            {
                UpdateModel(manuallyLoaded);
            }
            else
            {
                TriggerPreload();
            }
        }
    }
    
    public async ValueTask IterateToIndicesAsync(int index, int secondaryIndex, CancellationTokenSource ct)
    {
        if (index < 0 || index >= Files.Count)
        {
            return;
        }

        // Handle internal TIFF navigation
        // TODO: Figure out how to handle multi-page TIFF files when side-by-side is enabled
        if (_tab.Model.TiffNavigation is not null && ShouldNavigateTiffEntry(_tab.Model, IsReversed))
        {
            return;
        }

        CurrentIndex = index;
        SecondaryCurrentIndex = secondaryIndex;
        var firstFile = Files[index];
        var secondaryFile = Files[secondaryIndex];
        ImageModel? firstModel = null, secondModel = null;
        if (Cache.TryGet(firstFile, out var preLoadValue))
        {
            if (preLoadValue is { IsLoading: false, ImageModel.Image: not null })
            {
                // Is in cache
                firstModel = preLoadValue.ImageModel;
            }
            else
            {
                // Wait for loading complete
                await Task.Run(LoadFirstModelAsync, ct.Token).ConfigureAwait(false);
            }
        }
        else
        {
            // Not in cache
            var manuallyLoaded = await Cache.LoadAsync(_tab.Id, index, Files, ct.Token).ConfigureAwait(false);
            if (index == CurrentIndex && manuallyLoaded is not null)
            {
                firstModel = manuallyLoaded;
            }
            else
            {
                TriggerPreload();
                return;
            }
        }
        
        if (Cache.TryGet(secondaryFile, out var secondaryPreLoadValue))
        {
            if (secondaryPreLoadValue is { IsLoading: false, ImageModel.Image: not null })
            {
                // Is in cache
                secondModel = secondaryPreLoadValue.ImageModel;
            }
            else
            {
                // Wait for loading complete
                await Task.Run(LoadSecondModelAsync, ct.Token).ConfigureAwait(false);
            }
        }
        else
        {
            // Not in cache
            var manuallyLoaded = await Cache.LoadAsync(_tab.Id, secondaryIndex, Files, ct.Token).ConfigureAwait(false);
            if (index == CurrentIndex && manuallyLoaded is not null)
            {
                secondModel = manuallyLoaded;
            }
            else
            {
                TriggerPreload();
                return;
            }
        }

        if (firstModel is null)
        {
            if (index != CurrentIndex || secondaryIndex != SecondaryCurrentIndex)
            {
                return;
            }
            await Task.Run(LoadFirstModelAsync, ct.Token).ConfigureAwait(false);
            if (firstModel is null)
            {
                return;
            }
        }
        if (secondModel is null)
        {
            if (index != CurrentIndex || secondaryIndex != SecondaryCurrentIndex)
            {
                return;
            }
            await Task.Run(LoadSecondModelAsync, ct.Token).ConfigureAwait(false);
            if (secondModel is null)
            {
                return;
            }
        }

        // We need to update the secondary model first, because updating the first model will trigger reactive subscription,
        // where the secondary model need to be valid beforehand.
        _tab.SecondaryModel = secondModel;
        _tab.Model = firstModel;
        UpdateNavigationProperties();
        TriggerPreload();
        
        FileHistoryManager.Add(firstModel.FileInfo.FullName);
        FileHistoryManager.Add(secondModel.FileInfo.FullName);
        
        return;
        
        async Task LoadFirstModelAsync()
        {
            var thumb = _thumbCache.TryGet(firstFile.FullName, out var cachedThumb) ? cachedThumb 
                : _thumbnailLoader.GetExifThumbnail(firstFile);
                
            _tab.Image.Value = thumb;
            _tab.SetLoading();

            // Wait for loading complete
            var successfullyLoaded = await Cache.WaitForLoadingCompleteAsync(_tab.Id, index, _tab.ImageIterator.Files, ct.Token).ConfigureAwait(false);
            if (successfullyLoaded && index == CurrentIndex && secondaryIndex == SecondaryCurrentIndex)
            {
                if (preLoadValue.ImageModel.Image is null)
                {
                    var manuallyLoaded = await Cache.LoadAsync(_tab.Id, index, Files, ct.Token).ConfigureAwait(false);
                    if (index == CurrentIndex && secondaryIndex == SecondaryCurrentIndex && manuallyLoaded is not null)
                    {
                        firstModel = manuallyLoaded;
                    }
                    else
                    {
                        TriggerPreload();
                    }
                }
                else
                {
                    firstModel = preLoadValue.ImageModel;
                }
            }
            else
            {
                if (index != CurrentIndex && secondaryIndex == SecondaryCurrentIndex)
                {
                    await IterateToIndexAsync(CurrentIndex, ct).ConfigureAwait(false);
                    return;
                }
                TriggerPreload();
            }
        }

        async Task LoadSecondModelAsync()
        {
            var thumb = _thumbCache.TryGet(secondaryFile.FullName, out var cachedThumb) ? cachedThumb 
                : _thumbnailLoader.GetExifThumbnail(secondaryFile);
                
            _tab.Image.Value = thumb;
            _tab.SetLoading();

            // Wait for loading complete
            var successfullyLoaded = await Cache.WaitForLoadingCompleteAsync(_tab.Id, secondaryIndex, _tab.ImageIterator.Files, ct.Token).ConfigureAwait(false);
            if (successfullyLoaded && index == CurrentIndex && secondaryIndex == SecondaryCurrentIndex)
            {
                if (secondaryPreLoadValue.ImageModel.Image is null)
                {
                    var manuallyLoaded = await Cache.LoadAsync(_tab.Id, secondaryIndex, Files, ct.Token).ConfigureAwait(false);
                    if (index == CurrentIndex && secondaryIndex == SecondaryCurrentIndex && manuallyLoaded is not null)
                    {
                        secondModel = manuallyLoaded;
                    }
                    else
                    {
                        TriggerPreload();
                    }
                }
                else
                {
                    secondModel = secondaryPreLoadValue.ImageModel;
                }
            }
            else
            {
                if (index != CurrentIndex && secondaryIndex == SecondaryCurrentIndex)
                {
                    await IterateToIndexAsync(CurrentIndex, ct).ConfigureAwait(false);
                    return;
                }
                TriggerPreload();
            }
        }
        
    }

    public async ValueTask SkipToIndexAsync(int index, CancellationTokenSource ct)
    {
        if (index < 0 || index >= Files.Count) return;

        var file = Files[index];

        if (!Cache.TryGet(file, out var preLoadValue) || preLoadValue?.ImageModel?.Image == null)
        {
            Cache.Clear(_tab.Id);
        }

        await IterateToIndexAsync(index, ct).ConfigureAwait(false);
    }

    public async ValueTask NavigateByIncrementsAsync(SkipAmount skipAmount, bool forwards, CancellationTokenSource ct)
    {
        var (iteration, isReversed) = IterationHelper.GetIteration(CurrentIndex, Files.Count, forwards ? NavigateTo.Next : NavigateTo.Previous, skipAmount);
        IsReversed = isReversed;
        await SkipToIndexAsync(iteration, ct).ConfigureAwait(false);
    }

    public void SetCurrentIndex(int index)
    {
        CurrentIndex = index;
        UpdateNavigationProperties();
    }

    public async ValueTask ReloadAsync(bool clearCache = true)
    {
        if (clearCache)
        {
            Cache.Clear(_tab.Id);
        }
        var ct = _tab.GetTabCancellation();
        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            // Need to update SecondaryCurrentIndex
            var (_, nextIteration, _) = IterationHelper.GetIterations(CurrentIndex, Files.Count, NavigateTo.Next, SkipAmount.None);
            SecondaryCurrentIndex = nextIteration;
            await IterateToIndicesAsync(CurrentIndex, SecondaryCurrentIndex, ct).ConfigureAwait(false);
        }
        else
        {
            await IterateToIndexAsync(CurrentIndex, ct).ConfigureAwait(false);
        }
    }

    #endregion

    #region Repeated Navigation (Throttling)

    public async ValueTask RepeatNavigateAsync(NavigateTo to, TimeSpan repeatInterval, CancellationTokenSource ct)
    {
        var now = DateTime.UtcNow;
        if (now - _lastRepeatTime < repeatInterval)
        {
            return;
        }

        _lastRepeatTime = now;

        var (iteration, isReversed) = IterationHelper.GetIteration(CurrentIndex, Files.Count, to, SkipAmount.One);
        IsReversed = isReversed;
        await IterateToIndexAsync(iteration, ct).ConfigureAwait(false);
    }

    public void StopRepeatedNavigation()
    {
        // Reset the throttle so the next click reacts instantly.
        _lastRepeatTime = DateTime.MinValue; 
    }

    #endregion

    #region Update model & Loading

    private void UpdateModel(ImageModel newModel)
    {
        if (newModel.TiffNavigation is not null)
        {
            newModel.TiffNavigation.CurrentPage = IsReversed ? newModel.TiffNavigation.PageCount - 1 : 0;
            UpdateImageFromPage(newModel);
        }

        _tab.Image.Value = newModel.Image;
        _tab.FileInfo.Value = newModel.FileInfo;
        _tab.Model = newModel;
        UpdateNavigationProperties();
        TriggerPreload();
        
        // Update the file history
        FileHistoryManager.Add(newModel.FileInfo.FullName);
    }

    private void TriggerPreload()
    {
        Cache.Preload(_tab.Id, CurrentIndex, IsReversed, Files, _tab.GetTabCancellation().Token);
    }

    #endregion

    #region TIFF Handling & Helpers

    private static bool ShouldNavigateTiffEntry(ImageModel model, bool isPrevious)
    {
        if (model.TiffNavigation is null)
        {
            return false;
        }
        if (isPrevious)
        {
            var prev = model.TiffNavigation.CurrentPage - 1;
            if (prev < 0)
            {
                return false;
            }
            model.TiffNavigation.CurrentPage = prev;
        }
        else
        {
            var next = model.TiffNavigation.CurrentPage + 1;
            if (next >= model.TiffNavigation.PageCount)
            {
                return false;
            }
            model.TiffNavigation.CurrentPage = next;
        }

        UpdateImageFromPage(model);
        return true;
    }

    private static void UpdateImageFromPage(ImageModel model)
    {
        if (model.TiffNavigation is { Pages: not null, CurrentPage: >= 0 } &&
            model.TiffNavigation.CurrentPage < model.TiffNavigation.Pages.Length)
        {
            model.Image = model.TiffNavigation.Pages[model.TiffNavigation.CurrentPage];
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Cache.Clear(_tab, CurrentDirectory);
        Files = [];
        GC.SuppressFinalize(this);
    }

    #endregion
}