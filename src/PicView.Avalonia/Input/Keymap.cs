using System.Collections.Frozen;
using System.Text.Encodings.Web;
using System.Text.Json;
using Avalonia.Input;
using PicView.Core.DebugTools;
using PicView.Core.Keybindings;

namespace PicView.Avalonia.Input;

/// <summary>
/// The immutable assignment of <see cref="Keybind"/>s to actions, in two layers. Ordinary Keybinds have at most one
/// ordinary action each. Contextual Keybinds have at most one Contextual Action each, and a Keybind may be in both
/// layers. Which layer a binding is in follows from its action.
/// </summary>
public sealed class Keymap : IEquatable<Keymap>
{
    /// <summary>
    /// The keybindings file property holding the contextual layer. The ordinary layer is every other property.
    /// </summary>
    private const string ContextualSection = "Contextual";

    private static readonly JsonDocumentOptions ReadOptions = new() { AllowTrailingCommas = true };

    private static readonly JsonWriterOptions WriteOptions = new()
    {
        Indented = true,
        // Keep "+" in Keybinds such as "Ctrl+C" readable
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly FrozenDictionary<Keybind, string> _actions;
    private readonly FrozenDictionary<Keybind, string> _contextual;

    public static Keymap Empty { get; } =
        new(FrozenDictionary<Keybind, string>.Empty, FrozenDictionary<Keybind, string>.Empty);

    /// <summary>
    /// The ordinary action bound to each Keybind.
    /// </summary>
    public IReadOnlyDictionary<Keybind, string> Bindings => _actions;

    /// <summary>
    /// The Contextual Action bound to each contextual Keybind.
    /// </summary>
    public IReadOnlyDictionary<Keybind, string> ContextualBindings => _contextual;

    private Keymap(FrozenDictionary<Keybind, string> actions, FrozenDictionary<Keybind, string> contextual)
    {
        _actions = actions;
        _contextual = contextual;
    }

    /// <summary>
    /// Builds a keymap from the user's keybindings JSON, or from <paramref name="defaultJson"/> when there is none
    /// or it cannot be read. A user file without a contextual section keeps the default contextual Keybinds.
    /// </summary>
    public static Keymap FromJson(string? userJson, string defaultJson)
    {
        var defaults = Read(defaultJson);
        var user = Read(userJson);

        var ordinary = user?.Ordinary ?? defaults?.Ordinary ?? [];
        var contextual = user is not null ? user.Value.Contextual ?? defaults?.Contextual ?? [] : defaults?.Contextual ?? [];

        var actions = new Dictionary<Keybind, string>();
        var contextualActions = new Dictionary<Keybind, string>();
        foreach (var (key, action) in contextual)
        {
            Add(key, action, fromContextualSection: true);
        }

        foreach (var (key, action) in ordinary)
        {
            Add(key, action, fromContextualSection: false);
        }

        return new Keymap(actions.ToFrozenDictionary(), contextualActions.ToFrozenDictionary());

        void Add(string key, string action, bool fromContextualSection)
        {
            var keybind = Keybind.Parse(key);
            if (keybind is not { } parsed || (parsed.Key is Key.None && parsed.MouseButton is MouseButton.None))
            {
                DebugHelper.LogDebug(nameof(Keymap), nameof(FromJson), $"Skipping unparsable keybind '{key}'");
                return;
            }

            var canonical = KeybindCategoryDefinition.Canonicalize(action);
            var isContextual = KeybindCategoryDefinition.IsContextualAction(canonical);
            if (fromContextualSection && !isContextual)
            {
                // The contextual section only binds Contextual Actions
                DebugHelper.LogDebug(nameof(Keymap), nameof(FromJson),
                    $"Skipping '{key}': '{action}' is not a Contextual Action");
                return;
            }

            (isContextual ? contextualActions : actions)[parsed] = canonical;
        }
    }

    /// <summary>
    /// Reads the entries of a keybindings file. <see cref="KeymapFile.Contextual"/> is <c>null</c> when the file
    /// has no contextual section; the result is <c>null</c> when there is no file or it cannot be read.
    /// </summary>
    private static KeymapFile? Read(string? json)
    {
        if (json is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json, ReadOptions);
            if (document.RootElement.ValueKind is not JsonValueKind.Object)
            {
                return null;
            }

            var ordinary = new List<KeyValuePair<string, string>>();
            List<KeyValuePair<string, string>>? contextual = null;
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind is JsonValueKind.String)
                {
                    ordinary.Add(new(property.Name, property.Value.GetString()!));
                }
                else if (property.Value.ValueKind is JsonValueKind.Object &&
                         string.Equals(property.Name, ContextualSection, StringComparison.OrdinalIgnoreCase))
                {
                    contextual = [];
                    foreach (var entry in property.Value.EnumerateObject())
                    {
                        if (entry.Value.ValueKind is JsonValueKind.String)
                        {
                            contextual.Add(new(entry.Name, entry.Value.GetString()!));
                        }
                    }
                }
            }

            return new KeymapFile(ordinary, contextual);
        }
        catch (JsonException exception)
        {
            DebugHelper.LogDebug(nameof(Keymap), nameof(Read), exception);
            return null;
        }
    }

    private readonly record struct KeymapFile(
        List<KeyValuePair<string, string>> Ordinary,
        List<KeyValuePair<string, string>>? Contextual);

    /// <summary>
    /// Resolves <paramref name="keybind"/> as it acts in <paramref name="mode"/>: a blocking Input Mode lets no
    /// ordinary Keybind run, and a non-blocking one disallows Actions that need a still image.
    /// </summary>
    public KeymapResolution Resolve(Keybind keybind, InputMode mode)
    {
        if (mode.IsBlocking())
        {
            return KeymapResolution.None;
        }

        var resolution = Resolve(keybind);
        return mode.DisallowsStillImageActions() && KeybindCategoryDefinition.NeedsStillImage(resolution.Value ?? string.Empty)
            ? KeymapResolution.None
            : resolution;
    }

    public KeymapResolution Resolve(Keybind keybind) =>
        _actions.TryGetValue(keybind, out var action)
            ? KeymapResolution.Action(action)
            : KeymapResolution.None;

    /// <summary>
    /// Returns the Contextual Action bound to <paramref name="keybind"/>, or <c>null</c> when it has none.
    /// </summary>
    public string? ResolveContextual(Keybind keybind) => _contextual.GetValueOrDefault(keybind);

    /// <summary>
    /// Returns the Keybinds bound to <paramref name="action"/>.
    /// </summary>
    public IReadOnlyList<Keybind> KeybindsFor(string action)
    {
        var canonical = KeybindCategoryDefinition.Canonicalize(action);
        return
        [
            .. LayerOf(canonical).Where(kvp => string.Equals(kvp.Value, canonical, StringComparison.Ordinal))
                .Select(kvp => kvp.Key)
        ];
    }

    /// <summary>
    /// Returns a keymap where <paramref name="keybind"/> is owned by <paramref name="action"/>,
    /// taking it from any previous owner in the same layer.
    /// </summary>
    public Keymap Assign(Keybind keybind, string action)
    {
        var canonical = KeybindCategoryDefinition.Canonicalize(action);
        var layer = new Dictionary<Keybind, string>(LayerOf(canonical))
        {
            [keybind] = canonical
        };
        return WithLayer(canonical, layer.ToFrozenDictionary());
    }

    /// <summary>
    /// Returns a keymap where <paramref name="keybind"/> has no ordinary owner.
    /// </summary>
    public Keymap Unassign(Keybind keybind) => Unassign(keybind, contextual: false);

    private Keymap Unassign(Keybind keybind, bool contextual)
    {
        var layer = contextual ? _contextual : _actions;
        if (!layer.ContainsKey(keybind))
        {
            return this;
        }

        var remaining = layer.Where(kvp => kvp.Key != keybind).ToFrozenDictionary();
        return contextual ? new Keymap(_actions, remaining) : new Keymap(remaining, _contextual);
    }

    /// <summary>
    /// Returns a keymap where <paramref name="action"/> has exactly <paramref name="keybinds"/>,
    /// taking them from any previous owners in the same layer.
    /// </summary>
    public Keymap WithKeybinds(string action, IReadOnlyCollection<Keybind> keybinds)
    {
        var contextual = KeybindCategoryDefinition.IsContextualAction(action);
        var keymap = this;
        foreach (var keybind in KeybindsFor(action))
        {
            if (!keybinds.Contains(keybind))
            {
                keymap = keymap.Unassign(keybind, contextual);
            }
        }

        foreach (var keybind in keybinds)
        {
            keymap = keymap.Assign(keybind, action);
        }

        return keymap;
    }

    /// <summary>
    /// Returns a keymap with the mouse Keybinds of <paramref name="defaults"/> added, in both layers, when this
    /// keymap has no mouse Keybinds at all. Otherwise returns this keymap unchanged.
    /// </summary>
    public Keymap WithDefaultMouseKeybinds(Keymap defaults)
    {
        if (HasMouseKeybind(_actions) || HasMouseKeybind(_contextual))
        {
            return this;
        }

        var actions = new Dictionary<Keybind, string>(_actions);
        var contextual = new Dictionary<Keybind, string>(_contextual);
        var added = AddMouseKeybinds(defaults._actions, actions) | AddMouseKeybinds(defaults._contextual, contextual);
        return added ? new Keymap(actions.ToFrozenDictionary(), contextual.ToFrozenDictionary()) : this;

        static bool HasMouseKeybind(FrozenDictionary<Keybind, string> layer)
        {
            foreach (var keybind in layer.Keys)
            {
                if (keybind.MouseButton is not MouseButton.None)
                {
                    return true;
                }
            }

            return false;
        }

        static bool AddMouseKeybinds(FrozenDictionary<Keybind, string> from, Dictionary<Keybind, string> to)
        {
            var added = false;
            foreach (var (keybind, action) in from)
            {
                if (keybind.MouseButton is not MouseButton.None)
                {
                    to[keybind] = action;
                    added = true;
                }
            }

            return added;
        }
    }

    private FrozenDictionary<Keybind, string> LayerOf(string canonicalAction) =>
        KeybindCategoryDefinition.IsContextualAction(canonicalAction) ? _contextual : _actions;

    private Keymap WithLayer(string canonicalAction, FrozenDictionary<Keybind, string> layer) =>
        KeybindCategoryDefinition.IsContextualAction(canonicalAction)
            ? new Keymap(_actions, layer)
            : new Keymap(layer, _contextual);

    /// <summary>
    /// Serializes the bindings in the keybindings file format: the ordinary Keybinds as properties, and the
    /// contextual Keybinds in a <c>"Contextual"</c> object.
    /// </summary>
    public string ToJson()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, WriteOptions))
        {
            writer.WriteStartObject();
            WriteEntries(writer, _actions);
            writer.WriteStartObject(ContextualSection);
            WriteEntries(writer, _contextual);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteEntries(Utf8JsonWriter writer, FrozenDictionary<Keybind, string> layer)
    {
        // Keybinds are read back case-insensitively, so write each formatted Keybind once
        var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (keybind, action) in layer)
        {
            var key = keybind.GetFormattedForJSON();
            if (written.Add(key))
            {
                writer.WriteString(key, action);
            }
        }
    }

    #region IEquatable

    public bool Equals(Keymap? other) =>
        other is not null &&
        (ReferenceEquals(this, other) ||
         (SameEntries(_actions, other._actions) && SameEntries(_contextual, other._contextual)));

    public override bool Equals(object? obj) => obj is Keymap other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_actions.Count, _contextual.Count);

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
