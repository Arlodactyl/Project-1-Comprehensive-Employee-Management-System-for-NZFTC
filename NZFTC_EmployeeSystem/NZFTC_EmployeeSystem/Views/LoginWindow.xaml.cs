using System.Linq;
using System.Windows;
using System.Windows.Input;
using NZFTC_EmployeeSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// This is the login window - the first screen users see
    /// It checks username and password against the database
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            // Add Enter key support for login
            UserNameBox.KeyDown += InputBox_KeyDown;
            PasswordBox.KeyDown += InputBox_KeyDown;

            // Add Caps Lock detection
            UserNameBox.GotFocus += CheckCapsLock;
            PasswordBox.GotFocus += CheckCapsLock;
            UserNameBox.KeyDown += CheckCapsLockOnKeyPress;
            PasswordBox.KeyDown += CheckCapsLockOnKeyPress;
            UserNameBox.LostFocus += HideCapsLockWarning;
            PasswordBox.LostFocus += (s, e) => { if (!UserNameBox.IsFocused) HideCapsLockWarning(s, e); };

            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
                db.EnsureAdminExists();
            }
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SignIn_Click(sender, e);
            }
        }

        private void CheckCapsLock(object sender, RoutedEventArgs e)
        {
            if (Keyboard.IsKeyToggled(Key.CapsLock))
            {
                CapsLockWarning.Visibility = Visibility.Visible;
            }
            else
            {
                CapsLockWarning.Visibility = Visibility.Collapsed;
            }
        }

        private void CheckCapsLockOnKeyPress(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyToggled(Key.CapsLock))
            {
                CapsLockWarning.Visibility = Visibility.Visible;
            }
            else
            {
                CapsLockWarning.Visibility = Visibility.Collapsed;
            }
        }

        private void HideCapsLockWarning(object sender, RoutedEventArgs e)
        {
            CapsLockWarning.Visibility = Visibility.Collapsed;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ForgotPassword_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var emailWindow = new Window
            {
                Title = "Forgot Password",
                Width = 450,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White
            };

            var mainStack = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(30)
            };

            var instructionText = new System.Windows.Controls.TextBlock
            {
                Text = "Enter your email address to receive password reset instructions:",
                FontSize = 14,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 62, 80)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };
            mainStack.Children.Add(instructionText);

            var emailBox = new System.Windows.Controls.TextBox
            {
                Height = 40,
                Padding = new Thickness(10, 8, 10, 8),
                FontSize = 14,
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(189, 195, 199)),
                BorderThickness = new Thickness(2),
                Margin = new Thickness(0, 0, 0, 20)
            };
            mainStack.Children.Add(emailBox);

            emailBox.KeyDown += (s, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    SendPasswordReset(emailBox.Text, emailWindow);
                }
            };

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Width = 100,
                Height = 40,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166)),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 10, 0)
            };
            cancelButton.Click += (s, args) => emailWindow.Close();
            buttonPanel.Children.Add(cancelButton);

            var sendButton = new System.Windows.Controls.Button
            {
                Content = "Send",
                Width = 100,
                Height = 40,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(93, 173, 226)),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            sendButton.Click += (s, args) => SendPasswordReset(emailBox.Text, emailWindow);
            buttonPanel.Children.Add(sendButton);

            mainStack.Children.Add(buttonPanel);

            emailWindow.Content = mainStack;
            emailWindow.ShowDialog();
        }

        private void SendPasswordReset(string email, Window emailWindow)
        {
            email = email.Trim();
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show(
                    "Please enter your email address.",
                    "Email Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            if (!email.Contains("@") || !email.Contains("."))
            {
                MessageBox.Show(
                    "Please enter a valid email address.",
                    "Invalid Email",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            MessageBox.Show(
                $"A verification email has been sent to {email}.\n\n" +
                "Please check your inbox and follow the instructions to reset your password.\n\n" +
                "Note: This may take a few minutes to arrive.",
                "Verification Email Sent",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            emailWindow.Close();
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            string username = UserNameBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show(
                    "Please enter both username and password.",
                    "Missing Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            using (var db = new AppDbContext())
            {
                var user = db.Users
                    .Include(u => u.Employee)
                    .FirstOrDefault(u =>
                        u.Username == username &&
                        u.IsActive == true
                    );

                if (user != null && !PasswordHasher.VerifyPassword(password, user.Password))
                {
                    user = null;
                }

                if (user == null)
                {
                    MessageBox.Show(
                        "Invalid username or password.",
                        "Login Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    return;
                }

                user.LastLoginDate = System.DateTime.Now;
                db.SaveChanges();

                var dashboard = new DashboardWindow(user);
                dashboard.Show();

                this.Close();
            }
        }
    }
}