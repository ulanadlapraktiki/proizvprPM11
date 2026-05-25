using CartridgeAccounting.Windows;
using System.Configuration;
using System.Data;
using System.Windows;

namespace CartridgeAccounting
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LoginWindow login = new LoginWindow();
            login.ShowDialog();
        }
    }
}