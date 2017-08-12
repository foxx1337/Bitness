using PeNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Bitness
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Foo TheFoo { get; set; } = new Foo();

        public string TheBar { get; set; } = "this is bar";

        public PeFile File { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(string path)
        {
            File = new PeFile(path);
            InitializeComponent();
        }

        public class Foo
        {
            public string Age { get; set; } = "15";
        }

        private void ButtonCopyPath_Click(object sender, RoutedEventArgs e)
        {
            CopyPath();
        }

        private void wndMain_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.C:
                    CopyPath();
                    break;
                case Key.E:
                case Key.X:
                    OpenExplorer();
                    break;
                case Key.Escape:
                    Close();
                    break;
            }
        }

        private void ButtonOpenExplorer_Click(object sender, RoutedEventArgs e)
        {
            OpenExplorer();
        }

        private void CopyPath()
        {
            Clipboard.SetText(File.FileLocation);
        }

        private void  OpenExplorer()
        {
            Process.Start("explorer", "/select,\"" + File.FileLocation + "\"");
        }
    }
}
