using PicView.Core.ViewModels;

namespace PicView.Core.IPlatform;

/// <summary>
/// What one Main Window does when a tab it shows changes, such as showing the tab's new image or loading its gallery.
/// The window's <see cref="TabOverviewViewModel"/> attaches each tab once it has been opened, and again when the tab
/// moves to the window.
/// </summary>
public interface ITabHookups
{
    /// <summary>
    /// Starts reacting to <paramref name="tab"/>'s changes in this window.
    /// </summary>
    /// <returns>What stops it, disposed when the tab leaves the window or closes.</returns>
    IDisposable Attach(TabViewModel tab);
}
