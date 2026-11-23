using System.Text;
using System.Text.Json;
using PicView.Core.DebugTools;

namespace PicView.Core.Metadata;

/// <summary>
/// Decodes the payloads written by the common Stable Diffusion front-ends into
/// prompt / negative prompt / settings.
/// </summary>
internal static class SdMetadataParser
{
    /// <summary>
    /// Parses the plain-text block written by Automatic1111, Forge and SD.Next:
    /// <code>
    /// a prompt, possibly several lines
    /// Negative prompt: things to avoid
    /// Steps: 20, Sampler: DPM++ 2M, CFG scale: 7, Seed: 1234, Model: someModel
    /// </code>
    /// </summary>
    public static SdMetadata ParseA1111(string text, string generator = "Automatic1111")
    {
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        // The settings line is the last key: value line; everything above it is prompt text,
        // which may itself contain colons and commas.
        var settingsIndex = -1;
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            if (!IsSettingsLine(lines[i]))
            {
                continue;
            }

            settingsIndex = i;
            break;
        }

        var promptEnd = settingsIndex < 0 ? lines.Length : settingsIndex;

        const string negativeMarker = "Negative prompt:";
        var negativeIndex = -1;
        for (var i = 0; i < promptEnd; i++)
        {
            if (!lines[i].StartsWith(negativeMarker, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            negativeIndex = i;
            break;
        }

        string? negative = null;
        if (negativeIndex >= 0)
        {
            var negativeLines = lines[negativeIndex..promptEnd];
            negativeLines[0] = negativeLines[0][negativeMarker.Length..];
            negative = string.Join('\n', negativeLines).Trim();
        }

        var prompt = string.Join('\n', lines[..(negativeIndex >= 0 ? negativeIndex : promptEnd)]).Trim();
        var settings = settingsIndex < 0 ? null : string.Join('\n', lines[settingsIndex..]).Trim();

        return new SdMetadata
        {
            Generator = generator,
            Format = SdFormat.A1111,
            Prompt = NullIfEmpty(prompt),
            NegativePrompt = NullIfEmpty(negative),
            Settings = NullIfEmpty(settings),
            Raw = text
        };
    }

    /// <summary>
    /// A settings line is a comma separated run of "Key: value" pairs. Prompt lines can contain
    /// both colons and commas (prompt weighting, LoRA syntax), so at least two well-formed pairs
    /// are required before a line is taken for settings rather than prompt text.
    /// </summary>
    private static bool IsSettingsLine(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || !char.IsLetter(trimmed[0]))
        {
            return false;
        }

        if (trimmed.StartsWith("Steps:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var pairs = 0;
        foreach (var segment in trimmed.Split(", ", StringSplitOptions.TrimEntries))
        {
            if (IsKeyValuePair(segment) && ++pairs >= 2)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Matches "Some key: value", where the key is a short run of plain words.</summary>
    private static bool IsKeyValuePair(string segment)
    {
        var colon = segment.IndexOf(": ", StringComparison.Ordinal);
        if (colon <= 0 || colon > 32 || !char.IsLetter(segment[0]))
        {
            return false;
        }

        foreach (var c in segment.AsSpan(0, colon))
        {
            if (!char.IsLetterOrDigit(c) && c is not (' ' or '_' or '-' or '/' or '+'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Parses ComfyUI's API-format prompt graph: a map of node id to
    /// <c>{ "class_type": ..., "inputs": { ... } }</c>. The prompts are found by following the
    /// sampler node's positive and negative conditioning links back to their text encoders.
    /// </summary>
    public static SdMetadata? ParseComfyUi(string promptJson, string? workflowJson)
    {
        try
        {
            using var document = JsonDocument.Parse(promptJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var nodes = document.RootElement;
            var sampler = FindSamplerNode(nodes);

            string? prompt = null;
            string? negative = null;
            var settings = new List<string>();

            if (sampler.HasValue && sampler.Value.TryGetProperty("inputs", out var samplerInputs))
            {
                prompt = ResolveConditioningText(nodes, samplerInputs, "positive");
                negative = ResolveConditioningText(nodes, samplerInputs, "negative");

                AppendSetting(settings, samplerInputs, "steps", "Steps");
                AppendSetting(settings, samplerInputs, "cfg", "CFG scale");
                AppendSetting(settings, samplerInputs, "sampler_name", "Sampler");
                AppendSetting(settings, samplerInputs, "scheduler", "Scheduler");
                AppendSetting(settings, samplerInputs, "denoise", "Denoise");
                AppendSetting(settings, samplerInputs, "seed", "Seed");
                AppendSetting(settings, samplerInputs, "noise_seed", "Seed");
            }

            if (prompt is null)
            {
                // No usable sampler link: fall back to every text encoder in the graph.
                prompt = NullIfEmpty(string.Join('\n', CollectTextEncoderPrompts(nodes)));
            }

            var model = FindModelName(nodes);
            if (model is not null)
            {
                settings.Add($"Model: {model}");
            }

            if (prompt is null && negative is null && settings.Count == 0)
            {
                // Valid JSON, but nothing that looks like a generation graph.
                return null;
            }

            return new SdMetadata
            {
                Generator = "ComfyUI",
                Format = SdFormat.ComfyUi,
                Prompt = prompt,
                NegativePrompt = negative,
                Settings = settings.Count == 0 ? null : string.Join(", ", settings),
                Raw = Prettify(workflowJson ?? promptJson)
            };
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(SdMetadataParser), nameof(ParseComfyUi), e);
            return null;
        }
    }

    private static JsonElement? FindSamplerNode(JsonElement nodes)
    {
        foreach (var node in nodes.EnumerateObject())
        {
            if (node.Value.ValueKind != JsonValueKind.Object ||
                !node.Value.TryGetProperty("class_type", out var classType) ||
                classType.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var name = classType.GetString();
            if (name is null)
            {
                continue;
            }

            if (name.Contains("KSampler", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("SamplerCustom", StringComparison.OrdinalIgnoreCase))
            {
                return node.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Follows a <c>["nodeId", outputIndex]</c> conditioning link to the text it encodes.
    /// Conditioning is often chained through combine or control nodes, so the link is walked
    /// until a node with a literal text input is reached.
    /// </summary>
    private static string? ResolveConditioningText(JsonElement nodes, JsonElement inputs, string inputName)
    {
        var nodeId = GetLinkedNodeId(inputs, inputName);

        for (var depth = 0; depth < 10 && nodeId is not null; depth++)
        {
            if (!nodes.TryGetProperty(nodeId, out var node) ||
                !node.TryGetProperty("inputs", out var nodeInputs))
            {
                return null;
            }

            if (nodeInputs.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
            {
                return NullIfEmpty(text.GetString());
            }

            // Keep walking: prefer an explicit conditioning input, else the first link found.
            nodeId = GetLinkedNodeId(nodeInputs, "conditioning") ??
                     GetLinkedNodeId(nodeInputs, "conditioning_1") ??
                     FirstLinkedNodeId(nodeInputs);
        }

        return null;
    }

    private static string? GetLinkedNodeId(JsonElement inputs, string inputName)
    {
        if (inputs.ValueKind != JsonValueKind.Object ||
            !inputs.TryGetProperty(inputName, out var link) ||
            link.ValueKind != JsonValueKind.Array ||
            link.GetArrayLength() == 0)
        {
            return null;
        }

        var target = link[0];
        return target.ValueKind switch
        {
            JsonValueKind.String => target.GetString(),
            JsonValueKind.Number => target.GetRawText(),
            _ => null
        };
    }

    private static string? FirstLinkedNodeId(JsonElement inputs)
    {
        if (inputs.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var input in inputs.EnumerateObject())
        {
            var id = GetLinkedNodeId(inputs, input.Name);
            if (id is not null)
            {
                return id;
            }
        }

        return null;
    }

    private static List<string> CollectTextEncoderPrompts(JsonElement nodes)
    {
        var prompts = new List<string>();
        foreach (var node in nodes.EnumerateObject())
        {
            if (node.Value.ValueKind != JsonValueKind.Object ||
                !node.Value.TryGetProperty("class_type", out var classType) ||
                classType.ValueKind != JsonValueKind.String ||
                classType.GetString()?.Contains("CLIPTextEncode", StringComparison.OrdinalIgnoreCase) != true)
            {
                continue;
            }

            if (node.Value.TryGetProperty("inputs", out var inputs) &&
                inputs.TryGetProperty("text", out var text) &&
                text.ValueKind == JsonValueKind.String)
            {
                var value = text.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    prompts.Add(value);
                }
            }
        }

        return prompts;
    }

    private static string? FindModelName(JsonElement nodes)
    {
        foreach (var node in nodes.EnumerateObject())
        {
            if (node.Value.ValueKind != JsonValueKind.Object ||
                !node.Value.TryGetProperty("inputs", out var inputs) ||
                inputs.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var name in ModelInputNames)
            {
                if (inputs.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }
            }
        }

        return null;
    }

    private static readonly string[] ModelInputNames = ["ckpt_name", "unet_name", "model_name"];

    /// <summary>
    /// Parses NovelAI's split metadata: the prompt sits in the Description chunk and the
    /// remaining parameters, including the negative prompt as "uc", in the Comment chunk.
    /// </summary>
    public static SdMetadata? ParseNovelAi(string? description, string? commentJson)
    {
        try
        {
            string? negative = null;
            var settings = new List<string>();
            var prompt = NullIfEmpty(description);

            if (!string.IsNullOrWhiteSpace(commentJson) && commentJson.TrimStart().StartsWith('{'))
            {
                using var document = JsonDocument.Parse(commentJson);
                var root = document.RootElement;

                // V4+ keeps the base prompt and each character's prompt separately; the Description
                // chunk and "prompt" only hold the base, so the V4 captions take precedence.
                prompt = JoinV4Caption(root, "v4_prompt") ?? prompt ?? GetString(root, "prompt");
                negative = JoinV4Caption(root, "v4_negative_prompt") ?? GetString(root, "uc");

                AppendSetting(settings, root, "steps", "Steps");
                AppendSetting(settings, root, "scale", "CFG scale");
                AppendSetting(settings, root, "sampler", "Sampler");
                AppendSetting(settings, root, "seed", "Seed");
                AppendSetting(settings, root, "strength", "Strength");
                AppendSetting(settings, root, "noise", "Noise");
            }

            if (prompt is null && negative is null && settings.Count == 0)
            {
                return null;
            }

            return new SdMetadata
            {
                Generator = "NovelAI",
                Format = SdFormat.NovelAi,
                Prompt = prompt,
                NegativePrompt = negative,
                Settings = settings.Count == 0 ? null : string.Join(", ", settings),
                Raw = commentJson is null ? description ?? string.Empty : Prettify(commentJson)
            };
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(SdMetadataParser), nameof(ParseNovelAi), e);
            return null;
        }
    }

    /// <summary>
    /// Joins a NovelAI V4 caption, <c>{ "caption": { "base_caption": ..., "char_captions":
    /// [{ "char_caption": ... }] } }</c>, into one prompt with the base and each character on its own line.
    /// </summary>
    private static string? JoinV4Caption(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var v4) || v4.ValueKind != JsonValueKind.Object ||
            !v4.TryGetProperty("caption", out var caption) || caption.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var parts = new List<string>();
        var baseCaption = GetString(caption, "base_caption");
        if (baseCaption is not null)
        {
            parts.Add(baseCaption.Trim());
        }

        if (caption.TryGetProperty("char_captions", out var characters) &&
            characters.ValueKind == JsonValueKind.Array)
        {
            foreach (var character in characters.EnumerateArray())
            {
                var text = character.ValueKind == JsonValueKind.Object ? GetString(character, "char_caption") : null;
                if (text is not null)
                {
                    parts.Add(text.Trim());
                }
            }
        }

        return parts.Count == 0 ? null : string.Join('\n', parts);
    }

    /// <summary>Parses InvokeAI's flat metadata object.</summary>
    public static SdMetadata? ParseInvokeAi(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var settings = new List<string>();
            AppendSetting(settings, root, "steps", "Steps");
            AppendSetting(settings, root, "cfg_scale", "CFG scale");
            AppendSetting(settings, root, "scheduler", "Sampler");
            AppendSetting(settings, root, "seed", "Seed");
            AppendSetting(settings, root, "model", "Model");

            var prompt = GetString(root, "positive_prompt");
            var negative = GetString(root, "negative_prompt");

            if (prompt is null && negative is null && settings.Count == 0)
            {
                return null;
            }

            return new SdMetadata
            {
                Generator = "InvokeAI",
                Format = SdFormat.InvokeAi,
                Prompt = prompt,
                NegativePrompt = negative,
                Settings = settings.Count == 0 ? null : string.Join(", ", settings),
                Raw = Prettify(json)
            };
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(SdMetadataParser), nameof(ParseInvokeAi), e);
            return null;
        }
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? NullIfEmpty(value.GetString())
            : null;

    private static void AppendSetting(List<string> settings, JsonElement element, string name, string label)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return;
        }

        var text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Object when value.TryGetProperty("model_name", out var modelName) => modelName.GetString(),
            _ => null
        };

        if (string.IsNullOrWhiteSpace(text) ||
            settings.Exists(s => s.StartsWith($"{label}:", StringComparison.Ordinal)))
        {
            return;
        }

        settings.Add($"{label}: {text}");
    }

    /// <summary>
    /// Re-indents JSON so the raw payload is readable; returns the input untouched if it will not parse.
    /// </summary>
    internal static string Prettify(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
            {
                document.WriteTo(writer);
            }

            return Encoding.UTF8.GetString(buffer.ToArray());
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
