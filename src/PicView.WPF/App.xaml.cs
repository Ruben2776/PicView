using System.Runtime;
using System.Windows;
using System.Windows.Threading;

namespace PicView.WPF;

public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ProfileOptimization.SetProfileRoot(AppDomain.CurrentDomain.BaseDirectory);
        ProfileOptimization.StartProfile("ProfileOptimization");
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.ToString());
    }
}