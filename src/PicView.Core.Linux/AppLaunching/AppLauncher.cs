namespace PicView.Core.Linux.AppLaunching;

public static class AppLauncher
{
    /// <summary>
    /// Launches the specified application with the given file
    /// </summary>
    /// <param name="appPath">Full path to the application</param>
    /// <param name="filePath">Path to the file to open</param>
    /// <returns>True if the app was launched successfully</returns>
    public static Task<bool> LaunchAppWithFileAsync(string appPath, string filePath)
    {
        throw new NotImplementedException(); //TODO: Add Linux support
    }

    /// <summary>
    /// Launches the default application for the file type
    /// </summary>
    /// <param name="filePath">Path to the file to open</param>
    /// <returns>True if the file was opened successfully</returns>
    public static Task<bool> OpenWithDefaultAppAsync(string filePath)
    {
        throw new NotImplementedException(); //TODO: Add Linux support
    }
}
