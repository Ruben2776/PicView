using PicView.FileHandling;
using System.Runtime;
using System.Windows;
using System.Windows.Threading;

namespace PicView;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ProfileOptimization.SetProfileRoot(FileFunctions.GetWritingPath());
        ProfileOptimization.StartProfile("ProfileOptimization");
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.ToString());
    }
}