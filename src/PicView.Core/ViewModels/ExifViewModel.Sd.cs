using PicView.Core.DebugTools;
using PicView.Core.Localization;
using PicView.Core.Metadata;
using PicView.Core.Models;
using R3;

namespace PicView.Core.ViewModels;

// The Stable Diffusion generation data of the image shown, and its edits
public partial class ExifViewModel
{
    /// <summary>The part of the generation data an edit replaces.</summary>
    public enum SdField
    {
        Prompt,
        NegativePrompt,
        Settings
    }

    /// <summary>True when the image carries Stable Diffusion generation parameters.</summary>
    public BindableReactiveProperty<bool> IsSdMetadataAvailable { get; } = new();

    /// <summary>The tool that generated the image, e.g. "Automatic1111" or "ComfyUI".</summary>
    public BindableReactiveProperty<string?> SdGenerator { get; } = new();

    public BindableReactiveProperty<string?> SdPrompt { get; } = new();

    public BindableReactiveProperty<string?> SdNegativePrompt { get; } = new();

    public BindableReactiveProperty<string?> SdSettings { get; } = new();

    /// <summary>The unparsed payload, shown so nothing is lost when parsing falls short.</summary>
    public BindableReactiveProperty<string?> SdRaw { get; } = new();

    /// <summary>Where the parameters came from, e.g. a PNG text chunk or hidden in the pixels.</summary>
    public BindableReactiveProperty<string?> SdStorage { get; } = new();

    /// <summary>True while the pixel data is being scanned for hidden parameters.</summary>
    public BindableReactiveProperty<bool> IsSdStealthScanning { get; } = new();

    /// <summary>True when the prompt, negative prompt and settings can be written back to the file.</summary>
    public BindableReactiveProperty<bool> IsSdEditable { get; } = new();

    /// <summary>
    /// False when the UserComment holds the generation data, which the generation fields already show and edit.
    /// </summary>
    public BindableReactiveProperty<bool> IsCommentShown { get; } = new(true);

    public ReactiveCommand<string> SetSdPromptCommand { get; }
    public ReactiveCommand<string> SetSdNegativePromptCommand { get; }
    public ReactiveCommand<string> SetSdSettingsCommand { get; }

    // The generation data last shown, which an edit composes the other two fields from
    private SdMetadata? _sd;

    // The image _sd was read from, which can differ from _model while another image is being read
    private ImageModel? _sdModel;

    private CancellationTokenSource? _stealthCts;

    // Runs SD commits one at a time, from reading _sd to reading the file again, so none composes from stale data
    private readonly SemaphoreSlim _sdCommitGate = new(1, 1);

    // Makes a scan's check that it is still current and its apply one step, so a cancelled scan never shows its result
    private readonly Lock _stealthLock = new();

    /// <summary>
    /// Writes <paramref name="value"/> over one part of the generation data of the image shown, keeping the other two
    /// parts as its file holds them, then shows what the file holds.
    /// </summary>
    /// <returns>True if the file was written.</returns>
    public Task<bool> CommitSdAsync(SdField field, string? value) => CommitSdAsync(_model, field, value);

    /// <summary>
    /// Writes <paramref name="value"/> over one part of the generation data of <paramref name="model"/>, keeping the
    /// other two parts as its file holds them, then shows what the file holds. Commits run one at a time, so each one
    /// composes from what the one before it wrote. Nothing is written when <paramref name="model"/> is no longer the
    /// image shown, or the data shown is not yet its own; nor for data that is not editable, a value equal to what is
    /// shown, a Settings value that is not a settings line, or one that would not read back as typed. The box then
    /// goes back to the file's value.
    /// </summary>
    /// <returns>True if the file was written.</returns>
    public async Task<bool> CommitSdAsync(ImageModel? model, SdField field, string? value)
    {
        await _sdCommitGate.WaitAsync().ConfigureAwait(false);
        try
        {
            // Another image is shown, or is being read: the edit is dropped, and that image's values are left alone
            if (model is null || !ReferenceEquals(_model, model) || !ReferenceEquals(_sdModel, model) ||
                _sd is not { } sd)
            {
                return false;
            }

            if (!sd.IsEditable ||
                SdMetadataParser.Normalize(value) == SdMetadataParser.Normalize(GetField(sd, field)) ||
                (field == SdField.Settings && !IsSettingsValue(value)))
            {
                ShowFileValue(model, field);
                return false;
            }

            var prompt = field == SdField.Prompt ? value : sd.Prompt;
            var negativePrompt = field == SdField.NegativePrompt ? value : sd.NegativePrompt;
            var settings = field == SdField.Settings ? value : sd.Settings;
            var text = SdMetadataParser.ComposeA1111(prompt, negativePrompt, settings);

            var written = await CommitAsync(model, f =>
                SdMetadataReader.ReadsBackAs(sd.Source, text, prompt, negativePrompt, settings)
                    ? SdMetadataWriter.WriteAsync(f, sd, text)
                    : Task.FromResult(false)).ConfigureAwait(false);

            // Reading the file again leaves a box whose field did not change alone, so a refused edit is undone here
            ShowFileValue(model, field);
            return written;
        }
        finally
        {
            _sdCommitGate.Release();
        }
    }

    /// <summary>Puts the box of <paramref name="field"/> back to what the file of <paramref name="model"/> holds.</summary>
    private void ShowFileValue(ImageModel model, SdField field)
    {
        if (ReferenceEquals(_sdModel, model) && _sd is { } sd)
        {
            GetBindable(field).Value = GetField(sd, field) ?? string.Empty;
        }
    }

    /// <summary>
    /// True when <paramref name="value"/> starts with a settings line. A cleared or free-text Settings box would compose
    /// a text that can still read back, only without its settings line, so the seed, sampler and model would be lost.
    /// </summary>
    private static bool IsSettingsValue(string? value)
    {
        var normalized = SdMetadataParser.Normalize(value);
        var lineEnd = normalized.IndexOf('\n');
        return SdMetadataParser.IsSettingsLine(lineEnd < 0 ? normalized : normalized[..lineEnd]);
    }

    private static string? GetField(SdMetadata sd, SdField field) => field switch
    {
        SdField.Prompt => sd.Prompt,
        SdField.NegativePrompt => sd.NegativePrompt,
        _ => sd.Settings
    };

    private BindableReactiveProperty<string?> GetBindable(SdField field) => field switch
    {
        SdField.Prompt => SdPrompt,
        SdField.NegativePrompt => SdNegativePrompt,
        _ => SdSettings
    };

    /// <summary>
    /// Fills the Stable Diffusion fields from the image's generation parameters, and clears
    /// them when it has none.
    /// <para>
    /// When no parameters are stored in metadata, a scan for stealth pnginfo is started in the
    /// background. That one decodes the pixels, so it must not hold up the rest of the window;
    /// it is cancelled as soon as another image is shown.
    /// </para>
    /// </summary>
    private void UpdateSdMetadataValues(ImageModel model, FileInfo fileInfo, string? userComment, string? imageComment)
    {
        CancelStealthScan();

        var sdMetadata = SdMetadataReader.Read(fileInfo, userComment, imageComment);
        if (sdMetadata is not null)
        {
            ApplySdMetadata(sdMetadata, model);
            return;
        }

        ClearSdMetadataValues();
        StartStealthScan(model, fileInfo);
    }

    /// <summary>
    /// Shows <paramref name="sdMetadata"/>, read from the file of <paramref name="model"/>. When the same image is read
    /// again, such as after PicView's own write, a box is set only when its field changed in the file, so text being
    /// typed in another box is kept.
    /// </summary>
    internal void ApplySdMetadata(SdMetadata sdMetadata, ImageModel? model)
    {
        var previous = model is not null && ReferenceEquals(_sdModel, model) ? _sd : null;
        _sd = sdMetadata;
        _sdModel = model;
        SdGenerator.Value = sdMetadata.Generator;
        ShowField(SdPrompt, previous, previous?.Prompt, sdMetadata.Prompt);
        ShowField(SdNegativePrompt, previous, previous?.NegativePrompt, sdMetadata.NegativePrompt);
        ShowField(SdSettings, previous, previous?.Settings, sdMetadata.Settings);
        SdRaw.Value = sdMetadata.Raw;
        SdStorage.Value = GetStorageLabel(sdMetadata);
        IsSdEditable.Value = sdMetadata.IsEditable;
        IsCommentShown.Value = sdMetadata.Source != SdSource.ExifUserComment;
        IsSdMetadataAvailable.Value = true;
    }

    private static void ShowField(BindableReactiveProperty<string?> box, SdMetadata? previous, string? previousValue,
        string? value)
    {
        if (previous is null || !string.Equals(previousValue, value, StringComparison.Ordinal))
        {
            box.Value = value ?? string.Empty;
        }
    }

    private static string GetStorageLabel(SdMetadata sdMetadata)
    {
        var translation = TranslationManager.Translation;
        return sdMetadata.Source switch
        {
            SdSource.PngText => translation.StoredInPngText ?? string.Empty,
            SdSource.ExifUserComment => translation.StoredInUserComment ?? string.Empty,
            SdSource.ImageComment => translation.StoredInImageComment ?? string.Empty,
            SdSource.Pixels when !string.IsNullOrEmpty(sdMetadata.StealthDetail) =>
                $"{translation.StoredInPixels} ({sdMetadata.StealthDetail})",
            SdSource.Pixels => translation.StoredInPixels ?? string.Empty,
            _ => string.Empty
        };
    }

    /// <summary>
    /// Scans the pixel data for hidden parameters, off the calling thread. Only images that
    /// could plausibly carry them are scanned, and only while the info window is showing this
    /// image -- <see cref="UpdateSdMetadataValues"/> is reached only from the info window.
    /// </summary>
    private void StartStealthScan(ImageModel model, FileInfo fileInfo)
    {
        if (!IsStealthCandidate(fileInfo))
        {
            return;
        }

        var cts = new CancellationTokenSource();
        _stealthCts = cts;
        var token = cts.Token;

        IsSdStealthScanning.Value = true;

        _ = Task.Run(() =>
        {
            try
            {
                var hidden = SdMetadataReader.ReadStealth(fileInfo, token);
                if (hidden is null)
                {
                    return;
                }

                lock (_stealthLock)
                {
                    // The bindables fire while _stealthLock is held. That is safe only because Avalonia marshals a
                    // binding update from another thread with Post, so no subscriber may block on the UI thread here
                    if (!token.IsCancellationRequested)
                    {
                        ApplySdMetadata(hidden, model);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Another image was shown; its own read owns the fields now
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(ExifViewModel), nameof(StartStealthScan), e);
            }
            finally
            {
                lock (_stealthLock)
                {
                    if (!token.IsCancellationRequested)
                    {
                        IsSdStealthScanning.Value = false;
                    }
                }

                cts.Dispose();
            }
        }, token);
    }

    /// <summary>
    /// Stealth pnginfo only survives in lossless formats -- a JPEG re-encode destroys the low
    /// bits it lives in -- so scanning anything else is wasted work.
    /// </summary>
    private static bool IsStealthCandidate(FileInfo fileInfo) =>
        fileInfo.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
        fileInfo.Extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);

    private void CancelStealthScan()
    {
        lock (_stealthLock)
        {
            var cts = _stealthCts;
            _stealthCts = null;
            if (cts is null)
            {
                return;
            }

            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The scan finished and disposed it first
            }
        }

        IsSdStealthScanning.Value = false;
    }

    private void ClearSdMetadataValues()
    {
        _sd = null;
        _sdModel = null;
        IsSdMetadataAvailable.Value = false;
        IsSdEditable.Value = false;
        IsCommentShown.Value = true;
        SdGenerator.Value =
            SdPrompt.Value =
                SdNegativePrompt.Value =
                    SdSettings.Value =
                        SdStorage.Value =
                            SdRaw.Value = string.Empty;
    }
}
