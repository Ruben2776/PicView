using PicView.Core.DebugTools;
using PicView.Core.Localization;
using PicView.Core.Metadata;
using R3;

namespace PicView.Core.ViewModels;

// The Stable Diffusion generation data of the image shown, and its edits
public partial class ExifViewModel
{
    /// <summary>The part of the generation data an edit replaces.</summary>
    internal enum SdField
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

    private CancellationTokenSource? _stealthCts;

    // Makes a scan's check that it is still current and its apply one step, so a cancelled scan never shows its result
    private readonly Lock _stealthLock = new();

    /// <summary>
    /// Writes <paramref name="value"/> over one part of the generation data shown, keeping the other two parts as
    /// the file holds them, then shows what the file holds. Data that is not editable, a value equal to what is
    /// shown, or one that would not read back as typed, writes nothing.
    /// </summary>
    /// <returns>True if the file was written.</returns>
    internal Task<bool> CommitSdAsync(SdField field, string? value)
    {
        var sd = _sd;
        var current = sd is null ? null : GetField(sd, field);
        if (sd is not { IsEditable: true } ||
            SdMetadataParser.Normalize(value) == SdMetadataParser.Normalize(current))
        {
            GetBindable(field).Value = current ?? string.Empty;
            return Task.FromResult(false);
        }

        var prompt = field == SdField.Prompt ? value : sd.Prompt;
        var negativePrompt = field == SdField.NegativePrompt ? value : sd.NegativePrompt;
        var settings = field == SdField.Settings ? value : sd.Settings;
        var text = SdMetadataParser.ComposeA1111(prompt, negativePrompt, settings);

        return CommitAsync(f => SdMetadataReader.ReadsBackAs(sd.Source, text, prompt, negativePrompt, settings)
            ? SdMetadataWriter.WriteAsync(f, sd, text)
            : Task.FromResult(false));
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
    private void UpdateSdMetadataValues(FileInfo fileInfo, string? userComment, string? imageComment)
    {
        CancelStealthScan();

        var sdMetadata = SdMetadataReader.Read(fileInfo, userComment, imageComment);
        if (sdMetadata is not null)
        {
            ApplySdMetadata(sdMetadata);
            return;
        }

        ClearSdMetadataValues();
        StartStealthScan(fileInfo);
    }

    internal void ApplySdMetadata(SdMetadata sdMetadata)
    {
        _sd = sdMetadata;
        SdGenerator.Value = sdMetadata.Generator;
        SdPrompt.Value = sdMetadata.Prompt ?? string.Empty;
        SdNegativePrompt.Value = sdMetadata.NegativePrompt ?? string.Empty;
        SdSettings.Value = sdMetadata.Settings ?? string.Empty;
        SdRaw.Value = sdMetadata.Raw;
        SdStorage.Value = GetStorageLabel(sdMetadata);
        IsSdEditable.Value = sdMetadata.IsEditable;
        IsCommentShown.Value = sdMetadata.Source != SdSource.ExifUserComment;
        IsSdMetadataAvailable.Value = true;
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
    private void StartStealthScan(FileInfo fileInfo)
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
                    if (!token.IsCancellationRequested)
                    {
                        ApplySdMetadata(hidden);
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
