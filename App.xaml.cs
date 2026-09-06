using System.Configuration;
using System.Data;
using System.Windows;

namespace Kuiz
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DebugSession.Initialize(e.Args);
            base.OnStartup(e);
            var window = new MainWindow();
            MainWindow = window;
            window.Loaded += async (_, _) => await window.StartDebugSessionAsync();
            window.Show();
        }
    }

}
