using PeNet;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Bitness
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public PeFile File { get; set; }

        public string FileInfo
        {
            get
            {
                string ret = "";

                if (File.Is32Bit)
                {
                    ret += "32-bit / Win32 / x86\n";
                }
                else if (File.Is64Bit)
                {
                    ret += "64-bit / x64\n";
                }

                if (File.IsSigned)
                {
                    ret += "Signed ";
                }

                if (File.IsDLL)
                {
                    ret += "Library";
                }
                else if (File.IsEXE)
                {
                    ret += "Executable";
                }

                return ret;
            }
        }

        public string IconPath
        {
            get
            {
                if (File.IsDLL)
                {
                    return @"pack://application:,,,/Bitness;component/Images/dll.png";
                }
                else if (File.IsEXE)
                {
                    return @"pack://application:,,,/Bitness;component/Images/exe.png";
                }
                else
                {
                    return null;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(string path)
        {
            File = new PeFile(path);
            InitializeComponent();
        }

        private void ButtonCopyPath_Click(object sender, RoutedEventArgs e)
        {
            CopyPath();
        }

        private void WndMain_KeyUp(object sender, KeyEventArgs e)
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

        private void OpenExplorer()
        {
            Process.Start("explorer", "/select,\"" + File.FileLocation + "\"");
        }
    }
}
