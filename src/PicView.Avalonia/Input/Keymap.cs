using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Input;
using PicView.Core.DebugTools;
using PicView.Core.Keybindings;

namespace PicView.Avalonia.Input;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class SourceGenerationContext : JsonSerializerContext;

/// <summary>
/// The immutable assignment of <see cref="Keybind"/>s to actions. Each Keybind has at most one action.
/// </summary>
public sealed class Keymap : IEquatable<Keymap>
{
    private readonly FrozenDictionary<Keybind, string> _actions;

    public static Keymap Empty { get; } = new(FrozenDictionary<Keybind, string>.Empty);

    /// <summary>
    /// The action bound to each Keybind.
    /// </summary>
    public IReadOnlyDictionary<Keybind, string> Bindings => _actions;

    private Keymap(FrozenDictionary<Keybind, string> actions)
    {
        _actions = actions;
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

        return new Keymap(actions.ToFrozenDictionary());
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

    public KeymapResolution Resolve(Keybind keybind) =>
        _actions.TryGetValue(keybind, out var action)
            ? KeymapResolution.Action(action)
            : KeymapResolution.None;

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
        return new Keymap(actions.ToFrozenDictionary());
    }

    /// <summary>
    /// Returns a keymap where <paramref name="keybind"/> has no owner.
    /// </summary>
    public Keymap Unassign(Keybind keybind)
    {
        if (!_actions.ContainsKey(keybind))
        {
            return this;
        }

        var actions = new Dictionary<Keybind, string>(_actions);
        actions.Remove(keybind);
        return new Keymap(actions.ToFrozenDictionary());
    }

    /// <summary>
    /// Returns a keymap where <paramref name="action"/> has exactly <paramref name="keybinds"/>,
    /// taking them from any previous owners.
    /// </summary>
    public Keymap WithKeybinds(string action, IReadOnlyCollection<Keybind> keybinds)
    {
        var keymap = this;
        foreach (var keybind in KeybindsFor(action))
        {
            if (!keybinds.Contains(keybind))
            {
                keymap = keymap.Unassign(keybind);
            }
        }

        foreach (var keybind in keybinds)
        {
            keymap = keymap.Assign(keybind, action);
        }

        return keymap;
    }

    /// <summary>
    /// Serializes the action bindings in the keybindings file format.
    /// </summary>
    public string ToJson() =>
        JsonSerializer.Serialize(
                _actions.ToDictionary(kvp => kvp.Key.GetFormattedForJSON(), kvp => kvp.Value, StringComparer.OrdinalIgnoreCase),
                SourceGenerationContext.Default.DictionaryStringString)
            .Replace("\\u002B", "+", StringComparison.Ordinal); // Fix plus sign encoded to Unicode

    #region IEquatable

    public bool Equals(Keymap? other) =>
        other is not null &&
        (ReferenceEquals(this, other) || SameEntries(_actions, other._actions));

    public override bool Equals(object? obj) => obj is Keymap other && Equals(other);

    public override int GetHashCode() => _actions.Count;

    private static bool SameEntries(FrozenDictionary<Keybind, string> left, FrozenDictionary<Keybind, string> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var (keybind, owner) in left)
        {
            if (!right.TryGetValue(keybind, out var otherOwner) || !string.Equals(owner, otherOwner, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    #endregion
}
