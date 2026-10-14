using System.Collections.Frozen;
using System.Text.Json;
using Avalonia.Input;
using PicView.Core.DebugTools;
using PicView.Core.Favorites;
using PicView.Core.Keybindings;

namespace PicView.Avalonia.Input;

/// <summary>
/// The immutable assignment of <see cref="Keybind"/>s to owners. Each Keybind has at most one owner.
/// </summary>
public sealed class Keymap
{
    private readonly FrozenDictionary<Keybind, string> _actions;
    private readonly FrozenDictionary<Keybind, string> _favorites;

    public static Keymap Empty { get; } =
        new(FrozenDictionary<Keybind, string>.Empty, FrozenDictionary<Keybind, string>.Empty);

    /// <summary>
    /// The action bound to each Keybind.
    /// </summary>
    public IReadOnlyDictionary<Keybind, string> Bindings => _actions;

    private Keymap(FrozenDictionary<Keybind, string> actions, FrozenDictionary<Keybind, string> favorites)
    {
        _actions = actions;
        _favorites = favorites;
    }

    /// <summary>
    /// Builds a keymap from the user's keybindings JSON, or from <paramref name="defaultJson"/> when there is none
    /// or it cannot be read.
    /// </summary>
    public static Keymap FromJson(string? userJson, string defaultJson)
    {
        var entries = Deserialize(userJson) ?? Deserialize(defaultJson) ?? new Dictionary<string, string>();

        var actions = new Dictionary<Keybind, string>(entries.Count);
        foreach (var (key, action) in entries)
        {
            var keybind = Keybind.Parse(key);
            if (keybind is not { } parsed || (parsed.Key is Key.None && parsed.MouseButton is MouseButton.None))
            {
                DebugHelper.LogDebug(nameof(Keymap), nameof(FromJson), $"Skipping unparsable keybind '{key}'");
                continue;
            }

            actions[parsed] = KeybindCategoryDefinition.Canonicalize(action);
        }

        return new Keymap(actions.ToFrozenDictionary(), FrozenDictionary<Keybind, string>.Empty);
    }

    private static Dictionary<string, string>? Deserialize(string? json)
    {
        if (json is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(json, SourceGenerationContext.Default.DictionaryStringString);
        }
        catch (JsonException exception)
        {
            DebugHelper.LogDebug(nameof(Keymap), nameof(Deserialize), exception);
            return null;
        }
    }

    public KeymapResolution Resolve(Keybind keybind)
    {
        if (_favorites.TryGetValue(keybind, out var path))
        {
            return KeymapResolution.Favorite(path);
        }

        return _actions.TryGetValue(keybind, out var action)
            ? KeymapResolution.Action(action)
            : KeymapResolution.None;
    }

    /// <summary>
    /// Returns a keymap whose favorite Keybinds are exactly those of <paramref name="favorites"/>.
    /// A favorite takes its Keybind from any action that holds it.
    /// </summary>
    public Keymap WithFavorites(IReadOnlyList<FavoriteEntry> favorites)
    {
        var favoriteKeybinds = new Dictionary<Keybind, string>();
        foreach (var favorite in favorites)
        {
            if (string.IsNullOrWhiteSpace(favorite.Keybind) || Keybind.Parse(favorite.Keybind) is not { } keybind)
            {
                continue;
            }

            favoriteKeybinds[keybind] = favorite.FilePath;
        }

        var actions = _actions
            .Where(kvp => !favoriteKeybinds.ContainsKey(kvp.Key))
            .ToFrozenDictionary();
        return new Keymap(actions, favoriteKeybinds.ToFrozenDictionary());
    }

    /// <summary>
    /// Returns the Keybinds bound to <paramref name="action"/>.
    /// </summary>
    public IReadOnlyList<Keybind> KeybindsFor(string action)
    {
        var canonical = KeybindCategoryDefinition.Canonicalize(action);
        return _actions.Where(kvp => kvp.Value == canonical).Select(kvp => kvp.Key).ToList();
    }

    /// <summary>
    /// Returns a keymap where <paramref name="keybind"/> is owned by <paramref name="action"/>,
    /// taking it from any previous owner.
    /// </summary>
    public Keymap Assign(Keybind keybind, string action)
    {
        var actions = new Dictionary<Keybind, string>(_actions)
        {
            [keybind] = KeybindCategoryDefinition.Canonicalize(action)
        };
        return new Keymap(actions.ToFrozenDictionary(), WithoutFavorite(keybind));
    }

    /// <summary>
    /// Returns a keymap where <paramref name="keybind"/> has no owner.
    /// </summary>
    public Keymap Unassign(Keybind keybind)
    {
        var actions = new Dictionary<Keybind, string>(_actions);
        actions.Remove(keybind);
        return new Keymap(actions.ToFrozenDictionary(), WithoutFavorite(keybind));
    }

    private FrozenDictionary<Keybind, string> WithoutFavorite(Keybind keybind) =>
        _favorites.ContainsKey(keybind)
            ? _favorites.Where(kvp => kvp.Key != keybind).ToFrozenDictionary()
            : _favorites;

    /// <summary>
    /// Serializes the action bindings in the keybindings file format.
    /// </summary>
    public string ToJson() =>
        JsonSerializer.Serialize(
                _actions.ToDictionary(kvp => kvp.Key.GetFormattedForJSON(), kvp => kvp.Value, StringComparer.OrdinalIgnoreCase),
                SourceGenerationContext.Default.DictionaryStringString)
            .Replace("\\u002B", "+", StringComparison.Ordinal); // Fix plus sign encoded to Unicode
}
