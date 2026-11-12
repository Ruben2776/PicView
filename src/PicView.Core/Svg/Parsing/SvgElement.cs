using System.Xml;

namespace PicView.Core.Svg.Parsing;

/// <summary>An element of the light tree: an SVG-namespace element with its unprefixed attributes.</summary>
internal sealed class SvgElement(string name, Dictionary<string, string> attributes)
{
    public string Name { get; } = name;

    public IReadOnlyDictionary<string, string> Attributes { get; } = attributes;

    public List<SvgElement> Children { get; } = [];

    public string? GetAttribute(string attributeName)
    {
        return Attributes.GetValueOrDefault(attributeName);
    }
}

/// <summary>The element tree of an SVG file, with its elements by id.</summary>
internal sealed class SvgDocument
{
    public const string SvgNamespace = "http://www.w3.org/2000/svg";

    /// <summary>Elements nested deeper than this make the file unsupported.</summary>
    public const int MaxDepth = 256;

    /// <summary>SVG elements left out of the tree with their content: they never render.</summary>
    private static readonly HashSet<string> IgnoredElements = new(StringComparer.Ordinal)
    {
        "title", "desc", "metadata", "animate", "set", "animateTransform", "animateMotion"
    };

    /// <summary>SVG elements the resolver handles (other than the root <c>svg</c>).</summary>
    private static readonly HashSet<string> KnownElements = new(StringComparer.Ordinal)
    {
        "g", "path", "rect", "circle", "ellipse", "line", "polyline", "polygon"
    };

    private SvgDocument(SvgElement root, Dictionary<string, SvgElement> byId)
    {
        Root = root;
        ById = byId;
    }

    public SvgElement Root { get; }

    /// <summary>Elements by <c>id</c>; the first in document order wins.</summary>
    public IReadOnlyDictionary<string, SvgElement> ById { get; }

    /// <summary>
    ///     Reads the document into the element tree without recursion. Non-SVG elements and attributes are left out,
    ///     as are the ignored elements. Gives a reason for malformed XML, an unknown element or too deep nesting.
    /// </summary>
    public static bool TryRead(string svgText, out SvgDocument? document, out string? reason)
    {
        document = null;
        try
        {
            return TryReadCore(svgText, out document, out reason);
        }
        catch (XmlException)
        {
            reason = "xml";
            return false;
        }
    }

    private static bool TryReadCore(string svgText, out SvgDocument? document, out string? reason)
    {
        document = null;
        reason = null;
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true
        };

        using var text = new StringReader(svgText);
        using var reader = XmlReader.Create(text, settings);
        var byId = new Dictionary<string, SvgElement>(StringComparer.Ordinal);
        var open = new Stack<SvgElement>();
        SvgElement? root = null;

        var read = reader.Read();
        while (read)
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                open.Pop();
                read = reader.Read();
                continue;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                read = reader.Read();
                continue;
            }

            if (reader.Depth >= MaxDepth)
            {
                reason = "depth";
                return false;
            }

            var name = reader.LocalName;
            var inSvgNamespace = string.Equals(reader.NamespaceURI, SvgNamespace, StringComparison.Ordinal);
            if (root is null)
            {
                if (!inSvgNamespace || !string.Equals(name, "svg", StringComparison.Ordinal))
                {
                    reason = "xml";
                    return false;
                }
            }
            else if (!inSvgNamespace || IgnoredElements.Contains(name))
            {
                // Skip moves past the whole element, so it is not followed by Read.
                reader.Skip();
                read = !reader.EOF;
                continue;
            }
            else if (!KnownElements.Contains(name))
            {
                reason = "element:" + name;
                return false;
            }

            var isEmpty = reader.IsEmptyElement;
            var element = new SvgElement(name, ReadAttributes(reader));
            if (root is null)
            {
                root = element;
            }
            else
            {
                open.Peek().Children.Add(element);
            }

            if (element.GetAttribute("id") is { } id)
            {
                byId.TryAdd(id, element);
            }

            if (!isEmpty)
            {
                open.Push(element);
            }

            read = reader.Read();
        }

        if (root is null)
        {
            reason = "xml";
            return false;
        }

        document = new SvgDocument(root, byId);
        return true;
    }

    private static Dictionary<string, string> ReadAttributes(XmlReader reader)
    {
        var attributes = new Dictionary<string, string>(reader.AttributeCount, StringComparer.Ordinal);
        if (reader.MoveToFirstAttribute())
        {
            do
            {
                if (reader.NamespaceURI.Length == 0)
                {
                    attributes[reader.LocalName] = reader.Value;
                }
            } while (reader.MoveToNextAttribute());

            reader.MoveToElement();
        }

        return attributes;
    }
}
