using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.ProcessHandling;

namespace PicView.Core.FileAssociations;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(FileAssociationInstructions))]
[JsonSerializable(typeof(AssociationItem))]
[JsonSerializable(typeof(List<AssociationItem>))]
[JsonSerializable(typeof(List<string>))]
internal partial class FileAssociationSourceGenerationContext : JsonSerializerContext;

/// <summary>
/// Processes file associations through temporary files to handle large sets of associations.
/// </summary>
public static class FileAssociationProcessor
{
    /// <summary>
    /// Sets file associations for a collection of file type groups.
    /// On Windows, elevates permissions if necessary.
    /// </summary>
    /// <param name="groups">Collection of file type groups to process</param>
    /// <returns>True if successful, false otherwise</returns>
    public static async Task<bool> SetFileAssociations(List<FileTypeGroup> groups)
    {
        try
        
        {
            // If we're on Windows, check for admin permissions
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !IsAdministrator())
            {
                return await HandleNonAdminWindowsAssociations(groups).ConfigureAwait(false);
            }
            
            // Standard processing path (non-Windows or already has admin rights)
            return await HandleDirectAssociations(groups).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log the exception or handle it appropriately
            Debug.WriteLine($"Error in SetFileAssociations: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Processes command-line arguments for file associations.
    /// </summary>
    /// <param name="arg">The command-line argument to process</param>
    public static async Task ProcessFileAssociationArguments(string arg)
    {
        try
        {
            if (arg.StartsWith("associate:", StringComparison.OrdinalIgnoreCase))
            {
                // Process the file path after the "associate:" prefix
                var filePath = arg["associate:".Length..].Trim();
                Debug.WriteLine($"Loading association file from path: {filePath}");
                
                if (File.Exists(filePath))
                {
                    await ProcessAssociationFile(filePath).ConfigureAwait(false);
                }
                else
                {
                    Debug.WriteLine($"Association file not found: {filePath}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error processing file association arguments: {ex.Message}");
            Debug.WriteLine($"Argument was: {arg}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    #region Private Helper Methods

    private static async Task<bool> HandleNonAdminWindowsAssociations(List<FileTypeGroup> groups)
    {
        try
        {
            // Create the instructions object
            var instructions = new FileAssociationInstructions();

            foreach (var group in groups)
            {
                foreach (var fileType in group.FileTypes)
                {
                    if (!fileType.IsSelected.CurrentValue.HasValue)
                    {
                        continue; // Skip null selections
                    }

                    foreach (var extension in fileType.Extensions)
                    {
                        // Make sure to properly handle extensions that contain commas
                        var individualExtensions = extension.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);

                        foreach (var ext in individualExtensions)
                        {
                            var cleanExt = ext.Trim();
                            if (!cleanExt.StartsWith('.'))
                            {
                                cleanExt = "." + cleanExt;
                            }

                            if (fileType.IsSelected.CurrentValue.Value)
                            {
                                // Add to association list
                                instructions.ExtensionsToAssociate.Add(new AssociationItem
                                {
                                    Extension = cleanExt,
                                    Description = fileType.Description
                                });
                            }
                            else
                            {
                                // Add to unassociation list
                                instructions.ExtensionsToUnassociate.Add(cleanExt);
                            }
                        }
                    }
                }
            }

            // If nothing to do, return early
            if (instructions.ExtensionsToAssociate.Count == 0 && instructions.ExtensionsToUnassociate.Count == 0)
            {
                return true;
            }

            // Create a temporary file to store the instructions
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"PicViewAssoc_{Guid.NewGuid():N}.json");
            Debug.WriteLine($"Creating association file at path: {tempFilePath}");
            
            // Save instructions to the temp file using the AOT-compatible serializer context
            var json = JsonSerializer.Serialize(instructions, typeof(FileAssociationInstructions), 
                FileAssociationSourceGenerationContext.Default);
            await File.WriteAllTextAsync(tempFilePath, json).ConfigureAwait(false);

            // Create the command line argument
            var associateArg = $"associate:{tempFilePath}";
            Debug.WriteLine($"Launching elevated process with argument: {associateArg}");

            // Start new process with elevated permissions
            return await ProcessHelper.StartProcessWithElevatedPermissionAsync(associateArg).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error preparing file associations: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> HandleDirectAssociations(List<FileTypeGroup> groups)
    {
        foreach (var group in groups)
        {
            foreach (var fileType in group.FileTypes)
            {
                if (!fileType.IsSelected.CurrentValue.HasValue)
                {
                    continue;
                }

                foreach (var extension in fileType.Extensions)
                {
                    var individualExtensions = extension.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);

                    foreach (var ext in individualExtensions)
                    {
                        var cleanExt = ext.Trim();
                        if (!cleanExt.StartsWith('.'))
                        {
                            cleanExt = "." + cleanExt;
                        }

                        if (fileType.IsSelected.CurrentValue.Value)
                        {
                            await FileAssociationManager.AssociateFile(cleanExt, fileType.Description).ConfigureAwait(false);
                        }
                        else
                        {
                            await FileAssociationManager.UnassociateFile(cleanExt).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        return true;
    }

    private static async Task ProcessAssociationFile(string filePath)
    {
        try
        {
            Debug.WriteLine($"Reading association file: {filePath}");

            // Read the JSON file
            var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            
            // Use the source generation context for deserialization
            var instructions = JsonSerializer.Deserialize(json, typeof(FileAssociationInstructions), 
                FileAssociationSourceGenerationContext.Default) as FileAssociationInstructions;

            if (instructions == null)
            {
                Debug.WriteLine("Failed to parse association instructions from file");
                return;
            }

            Debug.WriteLine($"Processing {instructions.ExtensionsToAssociate.Count} associations and " +
                           $"{instructions.ExtensionsToUnassociate.Count} unassociations");

            // Process associations
            foreach (var item in instructions.ExtensionsToAssociate)
            {
                try
                {
                    Debug.WriteLine($"Associating {item.Extension} with description '{item.Description}'");
                    await FileAssociationManager.AssociateFile(item.Extension, item.Description).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error associating {item.Extension}: {ex.Message}");
                }
            }

            // Process unassociations
            foreach (var extension in instructions.ExtensionsToUnassociate)
            {
                try
                {
                    Debug.WriteLine($"Unassociating {extension}");
                    await FileAssociationManager.UnassociateFile(extension).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error unassociating {extension}: {ex.Message}");
                }
            }

            // Try to clean up the temp file
            try
            {
                File.Delete(filePath);
                Debug.WriteLine($"Deleted temporary file: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to delete temporary file {filePath}: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error processing association file: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private static bool IsAdministrator()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        // Check if running as administrator
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
    
    #endregion
}