using PicView.FileHandling;
using System.Runtime;
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
            ProfileOptimization.SetProfileRoot(FileFunctions.GetWritingPath());
            ProfileOptimization.StartProfile("ProfileOptimization");
        }
    }
}