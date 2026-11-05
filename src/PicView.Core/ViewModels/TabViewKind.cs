namespace PicView.Core.ViewModels;

/// <summary>
/// Which view a tab shows: the Tab View (ADR-0022). The tab derives it from its Crop, its Title Phase and whether it
/// shows anything, so nothing assigns a view into the tab.
/// </summary>
public enum TabViewKind
{
    /// <summary>The start-up menu: the tab shows nothing and is not loading.</summary>
    StartUpMenu,

    /// <summary>The image viewer: the tab shows a file or an image, or is loading or downloading one.</summary>
    ImageViewer,

    /// <summary>The crop control: the tab is in a crop.</summary>
    Crop
}
