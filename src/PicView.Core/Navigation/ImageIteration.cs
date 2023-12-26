using PicView.Core.Config;

namespace PicView.Core.Navigation;

public enum NavigateTo
{
    Next,
    Previous,
    First,
    Last,
}

public static class ImageIteration
{
    public static int GetNextIndex(NavigateTo navigateTo, bool loop, List<string> list, int index)
    {
        switch (navigateTo)
        {
            case NavigateTo.Next:
            case NavigateTo.Previous:
                var indexChange = navigateTo == NavigateTo.Next ? 1 : -1;

                if (SettingsHelper.Settings.UIProperties.Looping || loop)
                {
                    return (index + indexChange + list.Count) % list.Count;
                }

                var newIndex = index + indexChange;
                if (newIndex < 0 || newIndex >= list.Count)
                    return -1;
                return newIndex;

            case NavigateTo.First:
                return 0;

            case NavigateTo.Last:
                return list.Count - 1;

            default: return -1;
        }
    }
}