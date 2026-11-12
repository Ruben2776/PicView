using System.Text;
using System.Xml;

namespace PicView.Core.Svg.Parsing;

/// <summary>
///     An element of the light tree: an SVG-namespace element with its unprefixed attributes (and <c>xlink:href</c>
///     as <c>href</c>).
/// </summary>
internal sealed class SvgElement(string name, Dictionary<string, string> attributes)
{
    private readonly List<SvgElement> _children = [];

    public string Name { get; } = name;

    public IReadOnlyDictionary<string, string> Attributes { get; } = attributes;

    public IReadOnlyList<SvgElement> Children => _children;

    /// <summary>The parent element; null for the root.</summary>
    public SvgElement? Parent { get; private set; }

    public string? GetAttribute(string attributeName)
    {
        return Attributes.GetValueOrDefault(attributeName);
    }

    public void AddChild(SvgElement child)
    {
        child.Parent = this;
        _children.Add(child);
    }
}

/// <summary>The element tree of an SVG file, with its elements by id.</summary>
internal sealed class SvgDocument
{
    public const string SvgNamespace = "http://www.w3.org/2000/svg";

    public const string XLinkNamespace = "http://www.w3.org/1999/xlink";

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
        "g", "path", "rect", "circle", "ellipse", "line", "polyline", "polygon", "defs", "use", "linearGradient",
        "radialGradient", "stop", "clipPath", "style"
    };

    /// <summary>
    ///     Conditional processing attributes: a browser draws the element only when the condition holds for it, which
    ///     depends on the browser and the user's languages.
    /// </summary>
    private static readonly string[] ConditionalAttributes =
        ["systemLanguage", "requiredExtensions", "requiredFeatures"];

    /// <summary>The basic shapes and <c>path</c>.</summary>
    public static readonly HashSet<string> ShapeElements = new(StringComparer.Ordinal)
    {
        "path", "rect", "circle", "ellipse", "line", "polyline", "polygon"
    };

    private SvgDocument(SvgElement root, Dictionary<string, SvgElement> byId, SvgCss css)
    {
        Root = root;
        ById = byId;
        Css = css;
    }

    public SvgElement Root { get; }

    /// <summary>Elements by <c>id</c>; the first in document order wins.</summary>
    public IReadOnlyDictionary<string, SvgElement> ById { get; }

    /// <summary>The rules of all <c>style</c> elements, wherever they are in the document.</summary>
    public SvgCss Css { get; }

    /// <summary>
    ///     Reads the document into the element tree without recursion. Non-SVG elements and attributes are left out,
    ///     as are the ignored elements. The text of <c>style</c> elements is parsed as CSS. Gives a reason for
    ///     malformed XML, an unknown element, too deep nesting or CSS beyond the subset.
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
            // Read so an xml-stylesheet instruction, a link to another file's CSS, is seen.
            IgnoreProcessingInstructions = false,
            // Kept so whitespace between text and CDATA in a style element still separates CSS tokens.
            IgnoreWhitespace = false
        };

        using var text = new StringReader(svgText);
        using var reader = XmlReader.Create(text, settings);
        var byId = new Dictionary<string, SvgElement>(StringComparer.Ordinal);
        var open = new Stack<SvgElement>();
        var sheets = new List<string>();
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

            if (IsStyleSheetLink(reader))
            {
                reason = "css";
                return false;
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
                // A browser applies a style sheet wherever it stands, even in content that is never drawn.
                if (!TrySkipIgnored(reader))
                {
                    reason = "css";
                    return false;
                }

                read = reader.Read();
                continue;
            }
            else if (string.Equals(open.Peek().Name, "clipPath", StringComparison.Ordinal) &&
                     !ShapeElements.Contains(name) && !string.Equals(name, "use", StringComparison.Ordinal))
            {
                // A clipPath holds only shapes and use of shapes.
                reason = "element:clipPath";
                return false;
            }
            else if (!KnownElements.Contains(name))
            {
                reason = "element:" + name;
                return false;
            }

            var isEmpty = reader.IsEmptyElement;
            var element = new SvgElement(name, ReadAttributes(reader));
            foreach (var condition in ConditionalAttributes)
            {
                if (element.GetAttribute(condition) is not null)
                {
                    reason = "property:" + condition;
                    return false;
                }
            }

            if (root is null)
            {
                root = element;
            }
            else
            {
                open.Peek().AddChild(element);
            }

            if (element.GetAttribute("id") is { } id)
            {
                byId.TryAdd(id, element);
            }

            if (string.Equals(name, "style", StringComparison.Ordinal))
            {
                // The reader ends on the style element's end tag (or on the empty element itself).
                if (!TryReadStyleSheet(reader, element, isEmpty, sheets))
                {
                    reason = "css";
                    return false;
                }
            }
            else if (!isEmpty)
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

        if (!SvgCss.TryParse(sheets, out var css))
        {
            reason = "css";
            return false;
        }

        document = new SvgDocument(root, byId, css);
        return true;
    }

    /// <summary>Whether the reader is on an <c>xml-stylesheet</c> processing instruction.</summary>
    private static bool IsStyleSheetLink(XmlReader reader)
    {
        return reader.NodeType == XmlNodeType.ProcessingInstruction &&
               string.Equals(reader.Name, "xml-stylesheet", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Moves to the end of the element the reader is on, without recursion. False when the element is, or holds, a
    ///     <c>style</c> element in any namespace or an <c>xml-stylesheet</c> instruction.
    /// </summary>
    private static bool TrySkipIgnored(XmlReader reader)
    {
        if (string.Equals(reader.LocalName, "style", StringComparison.Ordinal))
        {
            return false;
        }

        if (reader.IsEmptyElement)
        {
            return true;
        }

        var depth = reader.Depth;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                return true;
            }

            if (IsStyleSheetLink(reader) || (reader.NodeType == XmlNodeType.Element &&
                                             string.Equals(reader.LocalName, "style", StringComparison.Ordinal)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Reads a <c>style</c> element's text and CDATA as one sheet. False for a <c>type</c> other than
    ///     <c>text/css</c>, a <c>media</c> other than <c>all</c>, a <c>title</c> (which makes the sheet one a browser
    ///     may switch off), or an element inside it.
    /// </summary>
    private static bool TryReadStyleSheet(XmlReader reader, SvgElement style, bool isEmpty, List<string> sheets)
    {
        if (style.GetAttribute("title") is { Length: > 0 })
        {
            return false;
        }

        if (style.GetAttribute("type") is { } type &&
            !type.Trim().Equals("text/css", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (style.GetAttribute("media") is { } media && media.Trim() is { Length: > 0 } trimmed &&
            !trimmed.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (isEmpty)
        {
            return true;
        }

        var sheet = new StringBuilder();
        while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.Whitespace
                    or XmlNodeType.SignificantWhitespace:
                    sheet.Append(reader.Value);
                    break;
                case XmlNodeType.Element:
                    return false;
                case XmlNodeType.ProcessingInstruction when IsStyleSheetLink(reader):
                    return false;
            }
        }

        sheets.Add(sheet.ToString());
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
                else if (string.Equals(reader.NamespaceURI, XLinkNamespace, StringComparison.Ordinal) &&
                         string.Equals(reader.LocalName, "href", StringComparison.Ordinal))
                {
                    // A plain href wins over xlink:href, whichever comes first.
                    attributes.TryAdd("href", reader.Value);
                }
            } while (reader.MoveToNextAttribute());

            reader.MoveToElement();
        }

        return attributes;
    }
}
