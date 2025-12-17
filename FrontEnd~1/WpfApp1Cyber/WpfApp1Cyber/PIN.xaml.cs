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
using System.Windows.Shapes;

namespace WpfApp1Cyber
{
    /// <summary>
    /// Interaction logic for PIN.xaml
    /// </summary>
    public partial class PIN : Window
    {
        public string poprawnyPin = "1234";
        public int zleProby = 0;
        public PIN()
        {
            InitializeComponent();
        }

        private void PINspr_Click(object sender, RoutedEventArgs e)
        {
            string wpisanyPin = pb_pin.Password;

            if (wpisanyPin == poprawnyPin)
            {
                tb_wynikp.Text = "PIN Prawidłowy. Dostęp przyznany.";
                tb_wynikp.Foreground = System.Windows.Media.Brushes.Green;
                tb_liczbap.Text = "";
                // Tutaj dalsza logika po zalogowaniu
            }
            else
            {
                zleProby++;
                tb_wynikp.Text = "Nieprawidłowy PIN!";
                tb_liczbap.Text = $"Pozostało prób: {4 - zleProby}";

                if (zleProby >= 4)
                {
                    tb_liczbap.Text = "Dostęp zablokowany! Skontaktuj się z administratorem.";
                    pb_pin.IsEnabled = false; // Blokujemy pole wpisywania
                }
            }
        }
    }
}
