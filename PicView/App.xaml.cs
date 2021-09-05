using System.Windows;

namespace PicView
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            System.Runtime.ProfileOptimization.SetProfileRoot(FileHandling.FileFunctions.GetWritingPath());
            System.Runtime.ProfileOptimization.StartProfile("ProfileOptimization");
        }
    }
}