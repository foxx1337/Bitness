using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Bitness
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Window window;
            string path = null;
            if (e.Args.Length == 1)
            {
                try
                {
                    path = Path.GetFullPath(e.Args[0]);
                }
                catch (Exception)
                {

                }
            }
            if (path != null)
            {
                window = new MainWindow(path);
            }
            else
            {
                window = new HelpWindow();
            }
            window.Show();
        }
    }
}
