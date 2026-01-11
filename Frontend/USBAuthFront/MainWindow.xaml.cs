using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace USBAuthFront
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string? _currentToken;
        private DateTime? _currentExpiresAt;
        private string? _currentDeviceId;
        private System.Windows.Threading.DispatcherTimer? _sessionTimer;
        private bool _sessionCheckInProgress = false;

        public MainWindow()
        {
            InitializeComponent();
            ShowStart();
            RefreshUsbLists();
        }

        // ====== Widoki ======

        private void HideAllViews()
        {
            StartView.Visibility = Visibility.Collapsed;
            RegisterView.Visibility = Visibility.Collapsed;
            LoginView.Visibility = Visibility.Collapsed;
            LoggedInView.Visibility = Visibility.Collapsed;
        }

        private void ShowStart()
        {
            HideAllViews();
            ClearRegisterFields();
            ClearLoginFields();
            StartView.Visibility = Visibility.Visible;
            if (!string.IsNullOrWhiteSpace(_currentToken))
                StatusText.Text = "Status: ekran startowy (jesteś zalogowany).";
            else
                StatusText.Text = "Status: ekran startowy.";
        }

        private void ShowRegister()
        {
            HideAllViews();
            ClearRegisterFields();
            RegisterView.Visibility = Visibility.Visible;
            StatusText.Text = "Status: rejestracja urządzenia.";
            RefreshUsbLists();
        }

        private void ShowLogin()
        {
            HideAllViews();
            ClearLoginFields();
            LoginView.Visibility = Visibility.Visible;
            StatusText.Text = "Status: logowanie.";
            RefreshUsbLists();
        }
        private void ShowLoggedIn(LoginResultDto result)
        {
            HideAllViews();

            LoggedInView.Visibility = Visibility.Visible;
            LoggedInDeviceIdText.Text = result.DeviceId;
            LoggedInExpiresText.Text = result.ExpiresAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

            StatusText.Text = "Status: zalogowano.";
        }

        private void ClearRegisterFields()
        {
            RegPinBox.Clear();
            RegOwnerNameBox.Clear();
            RegSecretBox.Clear();
        }

        private void ClearLoginFields()
        {
            LoginPinBox.Clear();
        }

        private void ClearLoggedInFields()
        {
            LoggedInDeviceIdText.Text = "-";
            LoggedInExpiresText.Text = "-";
            PingResultText.Text = "-";
        }

        // ====== Kliknięcia z ekranu startowego ======
        private void GoToRegister_Click(object sender, RoutedEventArgs e) => ShowRegister();

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_currentToken))
            {
                HideAllViews();
                LoggedInView.Visibility = Visibility.Visible;
                StatusText.Text = "Status: nadal zalogowany.";
                return;
            }

            ShowLogin();
        }

        private void BackToStart_Click(object sender, RoutedEventArgs e) => ShowStart();

        // ====== Pendrive listy ======
        private void RefreshUsb_Click(object sender, RoutedEventArgs e)
        {
            RefreshUsbLists();
        }

        private void RefreshUsbLists()
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Removable && d.IsReady)
                .Select(d => d.RootDirectory.FullName)
                .ToList();

            RegUsbCombo.ItemsSource = drives;
            if (drives.Count > 0) RegUsbCombo.SelectedIndex = 0;

            LoginUsbCombo.ItemsSource = drives;
            if (drives.Count > 0) LoginUsbCombo.SelectedIndex = 0;

            StatusText.Text = drives.Count > 0
                ? "Status: wykryto pendrive."
                : "Status: nie wykryto pendrive.";
        }

        // ====== Rejestracja ======
        private async void RegisterNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RegUsbCombo.SelectedItem == null)
                {
                    StatusText.Text = "Status: wybierz pendrive.";
                    return;
                }

                string usbRoot = RegUsbCombo.SelectedItem.ToString() ?? "";
                string pin = RegPinBox.Password;
                string owner = RegOwnerNameBox.Text;
                string secret = RegSecretBox.Password;

                var service = new USBAuthFront.UsbRegistrationService("https://localhost:7153");

                DoRegisterButtonEnabled(false);
                StatusText.Text = "Status: rejestracja w toku...";

                await service.RegisterAsync(usbRoot, owner, pin, secret);

                StatusText.Text = "Status: rejestracja OK. Zapisano keyblob na pendrive i dodano urządzenie do bazy.";
                ClearRegisterFields();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Błąd: " + ex.Message;
            }
            finally
            {
                DoRegisterButtonEnabled(true);
            }
        }

        private void DoRegisterButtonEnabled(bool enabled)
        {
            RegisterNowButton.IsEnabled = enabled;
        }

        // ====== Logowanie ======
        private async void LoginNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LoginUsbCombo.SelectedItem == null)
                {
                    StatusText.Text = "Status: wybierz pendrive.";
                    return;
                }

                string usbRoot = LoginUsbCombo.SelectedItem.ToString() ?? "";
                string pin = LoginPinBox.Password;

                StatusText.Text = "Status: logowanie w toku...";
                LoginNowButton.IsEnabled = false;

                var loginService = new UsbLoginService("https://localhost:7153");

                var result = await loginService.LoginAsync(usbRoot, pin);
                _currentToken = result.Token;
                _currentExpiresAt = result.ExpiresAt;
                _currentDeviceId = result.DeviceId;
                StartSessionTimer();
                ShowLoggedIn(result);

                LoginPinBox.Clear();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Błąd: " + ex.Message;
            }
            finally
            {
                LoginNowButton.IsEnabled = true;
            }
        }

        // ====== Wylogowanie ======
        private async void Logout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Status: wylogowywanie...";

                if (!string.IsNullOrWhiteSpace(_currentToken))
                {
                    var logoutService = new UsbLogoutService("https://localhost:7153");
                    await logoutService.LogoutAsync(_currentToken);
                }

                _currentToken = null;
                _currentDeviceId = null;
                _currentExpiresAt = null;
                StopSessionTimer();
                ClearLoggedInFields();
                StatusText.Text = "Status: wylogowano.";
                ShowStart();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Błąd wylogowania: " + ex.Message;
                _currentToken = null;
                _currentDeviceId = null;
                _currentExpiresAt = null;
                ClearLoggedInFields();
                ShowStart();
            }
        }

        private void ForceLogout(string reason)
        {
            StopSessionTimer();
            MessageBox.Show(reason, "Sesja wygasła", MessageBoxButton.OK, MessageBoxImage.Warning);

            _currentToken = null;
            _currentDeviceId = null;
            _currentExpiresAt = null;
            ClearLoggedInFields();

            ShowStart();
        }

        // ====== Ping ======

        private async void PingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PingButton.IsEnabled = false;
                PingResultText.Text = "Sprawdzam...";

                var api = new ProtectedApiService("https://localhost:7153");
                var result = await api.PingAsync(_currentToken ?? "");
                
                if (result.StartsWith("HTTP 401") || result.StartsWith("HTTP 403"))
                {
                    ForceLogout("Token jest nieprawidłowy lub wygasł. Nastąpi wylogowanie.");
                    return;
                }

                using var doc = JsonDocument.Parse(result);
                PingResultText.Text = doc.RootElement.GetProperty("message").GetString();
                StatusText.Text = "Status: ping wykonany.";
            }
            catch (Exception ex)
            {
                PingResultText.Text = "Błąd: " + ex.Message;
                StatusText.Text = "Status: błąd pinga.";
            }
            finally
            {
                PingButton.IsEnabled = true;
            }
        }

        // ====== Watermarki dla pól ======

        private void RegPinBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            RegPinWatermark.Visibility = RegPinBox.Password.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RegSecretBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            RegSecretWatermark.Visibility = RegSecretBox.Password.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoginPinBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            LoginPinWatermark.Visibility = LoginPinBox.Password.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RegOwnerNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RegOwnerWatermark.Visibility = string.IsNullOrEmpty(RegOwnerNameBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // ====== Zarządzanie sesją ======

        private void StartSessionTimer()
        {
            if (_sessionTimer != null)
                return;

            _sessionTimer = new System.Windows.Threading.DispatcherTimer();
            _sessionTimer.Interval = TimeSpan.FromMinutes(10);
            _sessionTimer.Tick += SessionTimer_Tick;
            _sessionTimer.Start();
        }

        private void StopSessionTimer()
        {
            if (_sessionTimer == null)
                return;

            _sessionTimer.Stop();
            _sessionTimer.Tick -= SessionTimer_Tick;
            _sessionTimer = null;
        }

        private async void SessionTimer_Tick(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentToken))
            {
                StopSessionTimer();
                return;
            }

            if (_sessionCheckInProgress)
                return;

            _sessionCheckInProgress = true;

            try
            {
                var api = new ProtectedApiService("https://localhost:7153");
                var result = await api.PingAsync(_currentToken);

                if (result.StartsWith("HTTP 401") || result.StartsWith("HTTP 403"))
                {
                    StopSessionTimer();
                    ForceLogout("Sesja wygasła (sprawdzenie okresowe). Nastąpi wylogowanie.");
                    return;
                }

                StatusText.Text = "Status: sesja aktywna (auto-check).";
            }
            catch
            {
                StatusText.Text = "Status: nie udało się sprawdzić sesji (auto-check).";
            }
            finally
            {
                _sessionCheckInProgress = false;
            }
        }
    }
}