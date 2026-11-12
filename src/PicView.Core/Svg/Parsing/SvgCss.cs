using System.Text;

namespace PicView.Core.Svg.Parsing;

/// <summary>
///     The rules of a file's <c>style</c> elements, parsed once, and the declarations they give each element. Handles
///     selectors made of an element name, <c>.class</c> and <c>#id</c> (compounded without whitespace, in comma lists);
///     any other CSS makes the parse fail. Matching is case-sensitive, and each element is matched once.
/// </summary>
internal sealed class SvgCss
{
    public static SvgCss Empty { get; } = new([]);

    private static readonly char[] Whitespace = [' ', '\t', '\n', '\r', '\f'];

    private readonly int _selectorCount;

    // Each selector is kept under one name it requires: its id, else its first class, else its element name.
    private readonly Dictionary<string, List<Selector>> _byId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Selector>> _byClass = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Selector>> _byName = new(StringComparer.Ordinal);

    /// <summary>Each element's merged CSS declarations, at most one value per property.</summary>
    private readonly Dictionary<SvgElement, KeyValuePair<string, string>[]> _matched = [];

    private SvgCss(List<Selector> selectors)
    {
        _selectorCount = selectors.Count;
        foreach (var selector in selectors)
        {
            var (index, key) = selector.Ids.Length > 0 ? (_byId, selector.Ids[0])
                : selector.Classes.Length > 0 ? (_byClass, selector.Classes[0])
                : (_byName, selector.Name!);
            if (!index.TryGetValue(key, out var list))
            {
                list = [];
                index[key] = list;
            }

            list.Add(selector);
        }
    }

    /// <summary>Parses the style sheets in document order. False for CSS beyond the subset.</summary>
    public static bool TryParse(IReadOnlyList<string> sheets, out SvgCss css)
    {
        css = Empty;
        var selectors = new List<Selector>();
        var order = 0;
        foreach (var sheet in sheets)
        {
            if (!TryParseSheet(sheet, selectors, ref order))
            {
                return false;
            }
        }

        if (selectors.Count > 0)
        {
            css = new SvgCss(selectors);
        }

        return true;
    }

    /// <summary>
    ///     The declarations the rules give <paramref name="element" />, by property name, each the value of the
    ///     matching rule with the highest specificity, the later rule on a tie.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>> Match(SvgElement element)
    {
        if (_selectorCount == 0)
        {
            return [];
        }

        if (_matched.TryGetValue(element, out var cached))
        {
            return cached;
        }

        var classes = element.GetAttribute("class")?.Split(Whitespace, StringSplitOptions.RemoveEmptyEntries) ?? [];
        var candidates = new List<Selector>();
        if (element.GetAttribute("id") is { } id)
        {
            AddMatching(_byId, id, element, classes, candidates);
        }

        // A class listed twice would add its selectors twice.
        foreach (var name in classes.Distinct(StringComparer.Ordinal))
        {
            AddMatching(_byClass, name, element, classes, candidates);
        }

        AddMatching(_byName, element.Name, element, classes, candidates);

        cached = [];
        if (candidates.Count > 0)
        {
            candidates.Sort(Selector.CompareCascade);
            var merged = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var selector in candidates)
            {
                foreach (var (name, value) in selector.Declarations)
                {
                    merged[name] = value;
                }
            }

            cached = [.. merged];
        }

        _matched[element] = cached;
        return cached;
    }

    private static void AddMatching(Dictionary<string, List<Selector>> index, string key, SvgElement element,
        string[] classes, List<Selector> into)
    {
        if (!index.TryGetValue(key, out var selectors))
        {
            return;
        }

        foreach (var selector in selectors)
        {
            if (selector.Matches(element, classes))
            {
                into.Add(selector);
            }
        }
    }

    private static bool TryParseSheet(string text, List<Selector> into, ref int order)
    {
        var pos = 0;
        var prelude = new StringBuilder();
        var block = new StringBuilder();
        while (true)
        {
            if (!TrySkipBetweenRules(text, ref pos))
            {
                return false;
            }

            if (pos == text.Length)
            {
                return true;
            }

            // At-rules, @charset and @import included, are not handled.
            if (text[pos] == '@')
            {
                return false;
            }

            prelude.Clear();
            block.Clear();
            if (!TryReadUntil(text, ref pos, '{', false, prelude) ||
                !TryReadUntil(text, ref pos, '}', true, block) ||
                !TryParseDeclarations(block.ToString(), out var declarations) ||
                !TryParseSelectorList(prelude.ToString(), declarations, order++, into))
            {
                return false;
            }
        }
    }

    /// <summary>Skips whitespace, comments and the legacy <c>&lt;!--</c> and <c>--&gt;</c> tokens.</summary>
    private static bool TrySkipBetweenRules(string text, ref int pos)
    {
        while (pos < text.Length)
        {
            if (IsWhitespace(text[pos]))
            {
                pos++;
            }
            else if (string.CompareOrdinal(text, pos, "/*", 0, 2) == 0)
            {
                if (!TrySkipComment(text, ref pos))
                {
                    return false;
                }
            }
            else if (string.CompareOrdinal(text, pos, "<!--", 0, 4) == 0)
            {
                pos += 4;
            }
            else if (string.CompareOrdinal(text, pos, "-->", 0, 3) == 0)
            {
                pos += 3;
            }
            else
            {
                return true;
            }
        }

        return true;
    }

    /// <summary>Moves past the comment at <paramref name="pos" />; false when it is not closed.</summary>
    private static bool TrySkipComment(string text, ref int pos)
    {
        var close = text.IndexOf("*/", pos + 2, StringComparison.Ordinal);
        if (close < 0)
        {
            return false;
        }

        pos = close + 2;
        return true;
    }

    /// <summary>
    ///     Reads up to <paramref name="end" /> and moves past it, leaving comments out of a selector and putting one
    ///     space for each in a declaration block. A selector may not hold a
    ///     string, a brace or a semicolon; a declaration block may hold strings (kept with their quotes) but no nested
    ///     block or <c>!</c>. Escapes are not handled anywhere. False when the text ends first.
    /// </summary>
    private static bool TryReadUntil(string text, ref int pos, char end, bool inBlock, StringBuilder into)
    {
        while (pos < text.Length)
        {
            var c = text[pos];
            if (c == end)
            {
                pos++;
                return true;
            }

            switch (c)
            {
                case '/' when pos + 1 < text.Length && text[pos + 1] == '*':
                    if (!TrySkipComment(text, ref pos))
                    {
                        return false;
                    }

                    // In a declaration a comment separates tokens, so 1/**/2 is two numbers.
                    if (inBlock)
                    {
                        into.Append(' ');
                    }

                    continue;
                case '"' or '\'' when inBlock:
                    var close = FindStringEnd(text, pos);
                    if (close < 0)
                    {
                        return false;
                    }

                    into.Append(text, pos, close + 1 - pos);
                    pos = close + 1;
                    continue;
                case '\\' or '"' or '\'' or '{' or '}' or '!':
                case ';' when !inBlock:
                    return false;
                default:
                    into.Append(c);
                    pos++;
                    continue;
            }
        }

        return false;
    }

    /// <summary>The position of the quote that closes the string opened at <paramref name="start" />, or -1.</summary>
    private static int FindStringEnd(string text, int start)
    {
        for (var i = start + 1; i < text.Length; i++)
        {
            var c = text[i];
            if (c == text[start])
            {
                return i;
            }

            if (c is '\\' or '\n' or '\r' or '\f')
            {
                return -1;
            }
        }

        return -1;
    }

    /// <summary>
    ///     Splits a block into declarations the way inline <c>style</c> is read: names lower-case, values trimmed.
    ///     Only properties the style code reads are kept; it ignores the others in inline style too.
    /// </summary>
    private static bool TryParseDeclarations(string block, out KeyValuePair<string, string>[] declarations)
    {
        var list = new List<KeyValuePair<string, string>>();
        declarations = [];
        var start = 0;
        var colon = -1;
        var quote = '\0';
        for (var i = 0; i <= block.Length; i++)
        {
            var c = i < block.Length ? block[i] : ';';
            if (quote != '\0')
            {
                if (c == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            if (c is '"' or '\'')
            {
                quote = c;
            }
            else if (c == ':' && colon < 0)
            {
                colon = i;
            }
            else if (c == ';')
            {
                var part = block.AsSpan(start, i - start);
                if (!part.Trim(Whitespace).IsEmpty)
                {
                    if (colon < 0)
                    {
                        return false;
                    }

                    var name = block[start..colon].Trim(Whitespace).ToLowerInvariant();
                    if (name.Length == 0)
                    {
                        return false;
                    }

                    if (SvgStyle.IsStyleProperty(name))
                    {
                        list.Add(new KeyValuePair<string, string>(name, block[(colon + 1)..i].Trim()));
                    }
                }

                start = i + 1;
                colon = -1;
            }
        }

        declarations = [.. list];
        return true;
    }

    private static bool TryParseSelectorList(string prelude, KeyValuePair<string, string>[] declarations,
        int order, List<Selector> into)
    {
        foreach (var part in prelude.Split(','))
        {
            if (!TryParseSelector(part.Trim(Whitespace), declarations, order, out var selector))
            {
                return false;
            }

            into.Add(selector);
        }

        return true;
    }

    /// <summary>A compound of an optional element name followed by classes and ids, with nothing else.</summary>
    private static bool TryParseSelector(string text, KeyValuePair<string, string>[] declarations, int order,
        out Selector selector)
    {
        selector = null!;
        var pos = 0;
        string? name = null;
        var ids = new List<string>();
        var classes = new List<string>();
        if (TryReadIdent(text, ref pos, out var ident))
        {
            name = ident;
        }

        while (pos < text.Length)
        {
            var list = text[pos] switch
            {
                '.' => classes,
                '#' => ids,
                _ => null
            };
            pos++;
            if (list is null || !TryReadIdent(text, ref pos, out ident))
            {
                return false;
            }

            list.Add(ident);
        }

        if (name is null && ids.Count == 0 && classes.Count == 0)
        {
            return false;
        }

        selector = new Selector(name, [.. ids], [.. classes], order, declarations);
        return true;
    }

    /// <summary>A CSS identifier without escapes.</summary>
    private static bool TryReadIdent(string text, ref int pos, out string ident)
    {
        ident = string.Empty;
        var start = pos;
        var i = pos;
        if (i < text.Length && text[i] == '-')
        {
            i++;
        }

        if (i >= text.Length || !(IsNameStart(text[i]) || text[i] == '-'))
        {
            return false;
        }

        i++;
        while (i < text.Length && (IsNameStart(text[i]) || char.IsAsciiDigit(text[i]) || text[i] == '-'))
        {
            i++;
        }

        ident = text[start..i];
        pos = i;
        return true;
    }

    private static bool IsNameStart(char c)
    {
        return char.IsAsciiLetter(c) || c == '_' || c >= 0x80;
    }

    private static bool IsWhitespace(char c)
    {
        return c is ' ' or '\t' or '\n' or '\r' or '\f';
    }

    /// <summary>One selector of a rule, with the rule's declarations and its place among the rules.</summary>
    private sealed class Selector(
        string? name,
        string[] ids,
        string[] classes,
        int order,
        KeyValuePair<string, string>[] declarations)
    {
        public string? Name { get; } = name;
        public string[] Ids { get; } = ids;
        public string[] Classes { get; } = classes;
        public KeyValuePair<string, string>[] Declarations { get; } = declarations;
        private int Order { get; } = order;

        /// <summary>Lowest precedence first: by ids, then classes, then element names, then source order.</summary>
        public static int CompareCascade(Selector a, Selector b)
        {
            var result = a.Ids.Length.CompareTo(b.Ids.Length);
            if (result == 0)
            {
                result = a.Classes.Length.CompareTo(b.Classes.Length);
            }

            if (result == 0)
            {
                result = (a.Name is null ? 0 : 1).CompareTo(b.Name is null ? 0 : 1);
            }

            return result != 0 ? result : a.Order.CompareTo(b.Order);
        }

        public bool Matches(SvgElement element, string[] elementClasses)
        {
            if (Name is not null && !string.Equals(Name, element.Name, StringComparison.Ordinal))
            {
                return false;
            }

            var id = element.GetAttribute("id");
            foreach (var selectorId in Ids)
            {
                if (!string.Equals(selectorId, id, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            foreach (var selectorClass in Classes)
            {
                if (Array.IndexOf(elementClasses, selectorClass) < 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
