using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;

namespace PicView.Tests.Navigation;

public class ArchiveNavigatorTests
{
    [Fact]
    public async Task NavigateBetweenArchives_ShouldDoNothing_IfCanNavigateIsFalse()
    {
        // Arrange
        SetDefaults();

        // We need to mock NavigationManager.CanNavigate, but it's static.
        // So we just rely on vm state that makes CanNavigate return false.
        // CanNavigate checks ImageIterator.ImagePaths > 0, etc.

        var vm = new MainViewModel();
        // Ensure ImageIterator is null or empty
        await NavigationManager.DisposeImageIteratorAsync();

        // Act
        await ArchiveNavigator.NavigateBetweenArchives(true, vm);

        // Assert
        // Nothing should happen. Verify no exception.
    }
}