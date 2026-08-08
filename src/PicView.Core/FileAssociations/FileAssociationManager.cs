using PicView.Core.DebugTools;

namespace PicView.Core.FileAssociations;

/// <summary>
/// Platform-agnostic manager for file associations that delegates to platform-specific implementations
/// </summary>
public static class FileAssociationManager
{
    private static IFileAssociationService? _service;
    
    /// <summary>
    /// Initializes the FileAssociationManager with the appropriate platform-specific implementation
    /// </summary>
    /// <param name="service">Platform-specific implementation of IFileAssociationService</param>
    public static void Initialize(IFileAssociationService? service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>
    /// Associates a single file extension with the application
    /// </summary>
    /// <param name="fileExtension">The file extension to associate</param>
    /// <param name="description">Optional custom description for the file type</param>
    /// <returns>True if successful, false otherwise</returns>
    public static async Task<bool> AssociateFile(string fileExtension, string? description = null)
    {
        if (!EnsureInitialized())
        {
            return false;
        }
        // Use provided description or generate a default one
        var fileDescription = description ?? $"{fileExtension.TrimStart('.')} Image File";
        return await _service.RegisterFileAssociation(fileExtension, fileDescription).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Unassociates a single file extension
    /// </summary>
    public static async Task<bool> UnassociateFile(string fileExtension)
    {
        var isAssociated = await IsFileAssociated(fileExtension).ConfigureAwait(false);
        if (isAssociated)
        {
            return await _service.UnregisterFileAssociation(fileExtension).ConfigureAwait(false);
        }
        
        return false;
    }
    
    /// <summary>
    /// Checks if a file extension is associated with the application
    /// </summary>
    public static async Task<bool> IsFileAssociated(string fileExtension)
    {
        if (!EnsureInitialized())
        {
            return false;
        }
        return await _service.IsFileAssociated(fileExtension).ConfigureAwait(false);
    }
    
    private static bool EnsureInitialized()
    {
        if (_service != null)
        {
            return true;
        }

        var ex = new InvalidOperationException(
            "FileAssociationManager has not been initialized. Call Initialize() with an appropriate implementation before using this class.");
        DebugHelper.LogDebug(nameof(FileAssociationManager), nameof(EnsureInitialized), ex);
        return false;
    }
}