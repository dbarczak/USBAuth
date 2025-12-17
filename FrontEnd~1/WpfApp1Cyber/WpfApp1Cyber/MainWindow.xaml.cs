using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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
using System.Windows.Forms;

namespace WpfApp1Cyber
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
        public string loginn = "Admin";
        public string haslo = "Admin";
        public int ZlyL = 0;
        public int ZleH = 0;

        private bool isLoginCorrect = false;
        private bool isPasswordCorrect = false;

        private void CheckAccessGranted()
        {
            if (isLoginCorrect && isPasswordCorrect)
            {
                
                WybPend.Visibility = Visibility.Visible;
                //MessageBox.Show("Poprawne uwierzytelnienie! Możesz wybrać dysk.");
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string login = tb_login.Text;
            if (login == loginn)
            {
                tb_wynik.Text = $"Podałeś prawidłowy login\nPodałeś login: {login}";
                tb_liczba.Text = " ";
                isLoginCorrect = true; 
                CheckAccessGranted();
                //Window1 WindowI = new Window1();
                //WindowI.Show();


                // this.Hide();
            }
            else
            {
                isLoginCorrect = false;
                ZlyL++;
                tb_wynik.Text = $"\aPodałeś nieprawidłowy login";
                tb_liczba.Text = $"\aPozostały ci {4 - ZlyL} próby";
                if (ZlyL >= 4)

                { tb_liczba.Text = $"\aWpisałeś nieprawidłowe hasło zbyt dużą liczbę razy!\nDostęp został zablokowany, skontaktuj się z administratorem!"; }
            }

          
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {   
            string password = tb_haslo.Text;
            if (password == haslo)
            {
                tb_wynik2.Text = $"Podałeś prawidłowe hasło\nPodałeś hasło: {password}";
                tb_liczba2.Text = " ";
                isPasswordCorrect = true; // Zaznaczamy, że hasło jest OK
                CheckAccessGranted();
            }
            else
            {
                isPasswordCorrect = false; // Resetujemy stan
                ZleH++;
                tb_wynik2.Text = $"\aPodałeś nieprawidłowe hasło";
                tb_liczba2.Text = $"\aPozostały ci {4 - ZleH} próby";
                if (ZleH >= 4)

                { tb_liczba2.Text = $"\aWpisałeś nieprawidłowe hasło zbyt dużą liczbę razy!\nDostęp został zablokowany, skontaktuj się z administratorem!"; }
            }
            }

        private void WybPend_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
            {//using ( var dialog = new System.Windows.Forms.FolderBrowserDialog() ) ;
                dialog.Description = "Wybierz podłączony dysk";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedPath = dialog.SelectedPath;
                    //MessageBox.Show($"Wybrano lokalizację: {selectedPath}");

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
