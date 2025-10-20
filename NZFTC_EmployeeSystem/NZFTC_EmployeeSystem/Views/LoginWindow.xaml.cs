using System.Windows;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// This section controls the login behaviour.
    /// [DB HOOK] Later you'll check username/password against a Users table.
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow() => InitializeComponent();

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            var user = UserNameBox.Text.Trim();
            var pass = PasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Please enter username and password.",
                    "Sign in", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // [DB HOOK] Example later:
            // using var db = new Data.AppDbContext();
            // var ok = db.Users.Any(u => u.Username == user && Verify(pass, u.PasswordHash));
            // if (!ok) { MessageBox.Show("Invalid credentials"); return; }

            var shell = new MainWindow();  // go to the main window
            shell.Show();
            Close();
        }
    }
}
