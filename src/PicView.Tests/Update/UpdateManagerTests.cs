using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using PicView.Core.Update;
using PicView.Core.IPlatform;

namespace PicView.Tests.Update;

public class UpdateManagerTests
{
    private class DummyPlatformUpdate : IPlatformSpecificUpdate
    {
        public bool HandlePlatformUpdateCalled { get; private set; }

        public Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath)
        {
            HandlePlatformUpdateCalled = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task UpdateCurrentVersion_WhenVersionIsCurrent_ReturnsFalse()
    {
        UpdateManager.ForceUpdate = false;

        var dummyPlatformUpdate = new DummyPlatformUpdate();

        // Act
        var result = await UpdateManager.UpdateCurrentVersion(dummyPlatformUpdate);

        // Assert
        Assert.False(result);
        Assert.False(dummyPlatformUpdate.HandlePlatformUpdateCalled);
    }

    [Fact]
    public async Task UpdateCurrentVersion_WhenVersionIsOld_ReturnsTrue()
    {
        // Set ForceUpdate to true to simulate an old version (3.0.0.3)
        UpdateManager.ForceUpdate = true;

        try
        {
            var dummyPlatformUpdate = new DummyPlatformUpdate();

            // Act
            var result = await UpdateManager.UpdateCurrentVersion(dummyPlatformUpdate);

            // Assert
            Assert.True(result);
            Assert.True(dummyPlatformUpdate.HandlePlatformUpdateCalled);
        }
        finally
        {
            // Reset to prevent affecting other tests
            UpdateManager.ForceUpdate = false;
        }
    }
}
