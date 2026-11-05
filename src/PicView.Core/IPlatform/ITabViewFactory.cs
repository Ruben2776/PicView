using PicView.Core.ViewModels;
using R3;

namespace PicView.Core.IPlatform;

/// <summary>
/// How one Main Window makes the control each of its tabs shows for its <see cref="TabViewKind"/>. The window's
/// <see cref="TabOverviewViewModel"/> attaches each tab when the tab is created in the window or moves to it.
/// </summary>
public interface ITabViewFactory
{
    /// <summary>
    /// Makes the views <paramref name="tab"/> shows in this window.
    /// </summary>
    /// <returns>The views, disposed when the tab leaves the window or closes.</returns>
    ITabViews Attach(TabViewModel tab);
}

/// <summary>
/// The views one Main Window makes for one of its tabs, and the control the tab shows now.
/// </summary>
public interface ITabViews : IDisposable
{
    /// <summary>
    /// The control the tab shows for its current <see cref="TabViewKind"/>, or null while it shows none. Core holds it
    /// as an object, since the control is a UI type.
    /// </summary>
    IReadOnlyBindableReactiveProperty<object?> View { get; }
}
