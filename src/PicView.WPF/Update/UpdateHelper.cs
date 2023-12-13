using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using AutoUpdaterDotNET;

namespace PicView.WPF.Update
{
    /// <summary>
    /// Helper class for managing application updates using AutoUpdaterDotNET library.
    /// </summary>
    internal static class UpdateHelper
    {
        /// <summary>
        /// Initiates the application update process.
        /// </summary>
        /// <param name="window">The owner window for the update process (optional).</param>
        internal static void Update(Window? window)
        {
            // Get the current directory information.
            var currentDirectory = new DirectoryInfo(Application.Current.StartupUri.AbsolutePath);

            // Set the installation path for AutoUpdater.
            if (currentDirectory.Parent != null)
            {
                AutoUpdater.InstallationPath = currentDirectory.Parent.FullName;
            }

            // Configure AutoUpdater settings.
            AutoUpdater.ShowRemindLaterButton = false;
            AutoUpdater.ReportErrors = true;
            AutoUpdater.ShowSkipButton = false;

            // Set the provided window as the owner window if available.
            if (window is not null)
            {
                AutoUpdater.SetOwner(window);
            }

            // Check administrator privileges and configure AutoUpdater accordingly.
            try
            {
                currentDirectory.GetAccessControl();
                AutoUpdater.RunUpdateAsAdmin = false;
            }
            catch (Exception)
            {
                AutoUpdater.RunUpdateAsAdmin = true;
            }

            // Check if the application is self-contained.
            var isSelfContained = File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "netstandard.dll"));

            // Determine the architecture and select the appropriate update source URL.
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86: // Not supported
                    break;

                case Architecture.X64:
                    AutoUpdater.Start(isSelfContained
                        ? "https://picview.org/update-x64.xml"
                        : "https://picview.org/update-x64.net-required.xml");
                    break;

                case Architecture.Arm: // Not supported
                    break;

                case Architecture.Arm64:
                    AutoUpdater.Start(isSelfContained
                        ? "https://picview.org/update-arm64.xml"
                        : "https://picview.org/update-arm64.net-required.xml");
                    break;

                case Architecture.Wasm: // Not supported
                case Architecture.S390x: // Not supported
                case Architecture.LoongArch64: // Not supported
                case Architecture.Armv6: // Not supported
                case Architecture.Ppc64le: // Not supported
                    // Additional architectures are not supported.
                    break;

                default: // Download exe installer, but (probably) won't get hit...
                    AutoUpdater.Start("https://picview.org/update.xml");
                    break;
            }
        }
    }
}