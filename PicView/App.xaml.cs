using System;
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
            if (e != null)
            {
                if (e.Args != null && e.Args.Length > 0)
                {
                    Properties["ArbitraryArgName"] = e.Args[0];
                }
                base.OnStartup(e);
            }

            // Set language choice
            Current.Resources.MergedDictionaries.Add(
                new ResourceDictionary
                {
                    Source = new Uri(@"/PicView;component/Translations/en-US.xaml", UriKind.Relative)
                }
            );

            System.Runtime.ProfileOptimization.SetProfileRoot(Library.Utilities.GetWritingPath());
            System.Runtime.ProfileOptimization.StartProfile("ProfileOptimization");
        }
    }
}