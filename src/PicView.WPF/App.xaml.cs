using System.IO;
using System.Runtime;
using System.Windows;
using System.Windows.Threading;

namespace PicView.WPF;

public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Task.Run((() =>
        {
            try
            {
                ProfileOptimization.SetProfileRoot(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/"));
                ProfileOptimization.StartProfile("ProfileOptimization");
            }
            catch (Exception)
            {
                ProfileOptimization.SetProfileRoot(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ruben2776/PicView/Config/"));
                ProfileOptimization.StartProfile("ProfileOptimization");
            }
        }));

        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.ToString());
    }
}