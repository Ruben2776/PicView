using System.Windows.Markup;
using XamlAnimatedGif.Properties;

[assembly: XmlnsDefinition(XmlnsInfo.XmlNamespace, "XamlAnimatedGif")]
[assembly: XmlnsPrefix(XmlnsInfo.XmlNamespace, "gif")]

namespace XamlAnimatedGif.Properties;

internal static class XmlnsInfo
{
    public const string XmlNamespace = "https://github.com/XamlAnimatedGif/XamlAnimatedGif";
}