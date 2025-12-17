using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;

namespace WpfApp1Cyber
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
            {//using ( var dialog = new System.Windows.Forms.FolderBrowserDialog() ) ;
            dialog.Description = "Wybierz podłączony dysk";
            dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedPath = dialog.SelectedPath;
                    MessageBox.Show($"Wybrano lokalizację: {selectedPath}");

                    // 2. Otwieramy okno wpisywania PINu
                    PIN pinWindow = new PIN();
                    pinWindow.Show();

                    // Zamykamy lub ukrywamy obecne okno
                    this.Hide();
                }
            }
        }

    }
}
