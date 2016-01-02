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

namespace ARPer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void attackMe_GotFocus(object sender, RoutedEventArgs e)
        {
            attackMe.Foreground = Brushes.Black;
            if (attackMe.Text == "IP to attack")
            {
                attackMe.Text = "";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            attackMe.Focus();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (isIpValid(attackMe.Text))
            {
                sendArp(attackMe.Text); 
            }
            else
            {
                attackMe.Focus();
                attackMe.SelectAll();
            }
        }

        private bool isIpValid(string text)
        {
            //throw new NotImplementedException();
            var quads = text.Split('.');

            // if we do not have 4 quads, return false
            if (!(quads.Length == 4)) return false;

            // for each quad
            foreach (var quad in quads)
            {
                int q;
                // if parse fails 
                // or length of parsed int != length of quad string (i.e.; '1' vs '001')
                // or parsed int < 0
                // or parsed int > 255
                // return false
                if (!Int32.TryParse(quad, out q)
                    || !q.ToString().Length.Equals(quad.Length)
                    || q < 0
                    || q > 255) { return false; }
            }
            return true;
        }

        private void sendArp(string text)
        {
            throw new NotImplementedException();
        }
    }
}
