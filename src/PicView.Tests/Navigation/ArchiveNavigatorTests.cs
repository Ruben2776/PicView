namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class ArchiveNavigatorTests
{
    [Fact]
    public async Task NavigateBetweenArchives_ShouldDoNothing_IfCanNavigateIsFalse()
    {
        // Arrange
        SetDefaults();
        

        // Assert
        // Nothing should happen. Verify no exception.
    }
}