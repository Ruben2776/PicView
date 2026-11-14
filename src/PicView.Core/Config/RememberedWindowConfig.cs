using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Core.Config;

/// <summary>
/// The serializer for every remembered window's properties. A new remembered window adds its properties type here.
/// </summary>
[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(BatchResizeWindowConfig.BatchResizeWindowProperties))]
[JsonSerializable(typeof(ConvertWindowConfig.ConvertWindowProperties))]
[JsonSerializable(typeof(EffectsWindowConfig.EffectsWindowProperties))]
[JsonSerializable(typeof(ImageInfoWindowConfig.ImageInfoWindowProperties))]
[JsonSerializable(typeof(KeybindingWindowConfig.KeybindingWindowProperties))]
[JsonSerializable(typeof(PrintWindowConfig.PrintWindowProperties))]
[JsonSerializable(typeof(ResizeWindowConfig.ResizeWindowProperties))]
[JsonSerializable(typeof(SettingsWindowConfig.SettingsWindowProperties))]
internal partial class RememberedWindowGenerationContext : JsonSerializerContext;

/// <summary>
/// The config file of a remembered Tool Window or App Window. Each window has its own file, named by its subclass.
/// </summary>
/// <typeparam name="T">The window's properties, which must be in <see cref="RememberedWindowGenerationContext" />.</typeparam>
public abstract class RememberedWindowConfig<T>(string configFileName) : ConfigFile(configFileName)
    where T : RememberedWindowProperties, new()
{
    private static readonly JsonTypeInfo<T> TypeInfo =
        (JsonTypeInfo<T>?)RememberedWindowGenerationContext.Default.GetTypeInfo(typeof(T))
        ?? throw new InvalidOperationException(
            $"{typeof(T).Name} is not in {nameof(RememberedWindowGenerationContext)}.");

    /// <summary>
    /// What the window remembered. Null until <see cref="Load" /> runs.
    /// </summary>
    public T? WindowProperties { get; private set; }

    /// <summary>
    /// Reads the file. A missing, empty or unreadable file gives the window's defaults.
    /// </summary>
    public void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var bytes = File.ReadAllBytes(FilePath);
                WindowProperties = JsonSerializer.Deserialize(bytes, TypeInfo) ?? new T();
                return;
            }
        }
        catch
        {
            // Falls through to the defaults
        }

        WindowProperties = new T();
    }

    /// <summary>
    /// Saves the file into the resolved config folder.
    /// </summary>
    public Task SaveAsync() =>
        ConfigFileManager.SaveAsync(this, WindowProperties, typeof(T), RememberedWindowGenerationContext.Default);
}
