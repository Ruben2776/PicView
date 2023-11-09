using System.IO;
using System.IO.Packaging;
using System.Windows;

namespace XamlAnimatedGif;

internal class UriLoader
{
    public static Task<Stream> GetStreamFromUriAsync(Uri uri)
    {
        return GetStreamFromUriCoreAsync(uri);
    }

    private static Task<Stream> GetStreamFromUriCoreAsync(Uri uri)
    {
        if (uri.Scheme == PackUriHelper.UriSchemePack)
        {
            var sri = uri.Authority == "siteoforigin:,,,"
                ? Application.GetRemoteStream(uri)
                : Application.GetResourceStream(uri);

            if (sri != null)
                return Task.FromResult(sri.Stream);

            throw new FileNotFoundException("Cannot find file with the specified URI");
        }

        if (uri.Scheme == Uri.UriSchemeFile)
        {
            return Task.FromResult<Stream>(File.OpenRead(uri.LocalPath));
        }

        throw new NotSupportedException("Only pack:, file:, http: and https: URIs are supported");
    }
}