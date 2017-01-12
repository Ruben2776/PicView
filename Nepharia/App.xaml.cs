using System;
using System.Windows;

namespace Nepharia
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args != null && e.Args.Length > 0)
            {
                Properties["ArbitraryArgName"] = e.Args[0];
            }
            base.OnStartup(e);

            System.Runtime.ProfileOptimization.SetProfileRoot(AppDomain.CurrentDomain.BaseDirectory);
            System.Runtime.ProfileOptimization.StartProfile("Profile optimization");
        }
    }
}
