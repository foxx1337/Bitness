using System;
using System.Collections.Generic;
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

        public MainWindow()
        {
            InitializeComponent();
        }

        public class Foo
        {
            public string Age { get; set; } = "15";
        }
    }
}
