using System.Linq;
using System.Windows;
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

            // ========================================
            // ENSURE DATABASE AND ADMIN ACCOUNT EXIST
            // This runs every time the login window opens
            // ========================================
            using (var db = new AppDbContext())
            {
                // Create database if it doesn't exist
                db.Database.EnsureCreated();

                // Ensure admin account exists (creates it if missing)
                db.EnsureAdminExists();
            }
        }

        // ========================================
        // CANCEL BUTTON - Close the window
        // ========================================
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close(); // This closes the login window and exits the app
        }

        // ========================================
        // FORGOT PASSWORD LINK - Show email input popup
        // This is a fake feature that doesn't actually send emails
        // ========================================
        private void ForgotPassword_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Create a simple input dialog for email entry
            var emailWindow = new Window
            {
                Title = "Forgot Password",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White
            };

            // Create the layout for the popup
            var mainStack = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(30)
            };

            // Add instruction text
            var instructionText = new System.Windows.Controls.TextBlock
            {
                Text = "Enter your email address to receive password reset instructions:",
                FontSize = 14,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 62, 80)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };
            mainStack.Children.Add(instructionText);

            // Add email textbox
            var emailBox = new System.Windows.Controls.TextBox
            {
                Height = 40,
                Padding = new Thickness(10, 8, 10, 8),
                FontSize = 14,
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(189, 195, 199)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 20)
            };
            mainStack.Children.Add(emailBox);

            // Add submit button
            var submitButton = new System.Windows.Controls.Button
            {
                Content = "Submit",
                Height = 40,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(93, 173, 226)),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            // When submit button is clicked, show confirmation message
            submitButton.Click += (s, args) =>
            {
                // Validate email input
                string email = emailBox.Text.Trim();
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

                // Show fake confirmation message
                MessageBox.Show(
                    "A confirmation email has been sent to your email address.",
                    "Email Sent",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Close the email popup window
                emailWindow.Close();
            };
            mainStack.Children.Add(submitButton);

            // Set the content of the window and show it
            emailWindow.Content = mainStack;
            emailWindow.ShowDialog();
        }

        // ========================================
        // SIGN IN BUTTON - Check credentials
        // ========================================
        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            // Step 1: Get the username and password from the text boxes
            // Trim() removes any extra spaces the user might have typed
            string username = UserNameBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            // Step 2: Validate input - make sure both fields have text
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show(
                    "Please enter both username and password.",
                    "Missing Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return; // Stop here, don't try to login
            }

            // Step 3: Check the database for matching credentials
            // 'using' ensures the database connection is properly closed after use
            using (var db = new AppDbContext())
            {
                // Search the Users table for a matching username only (not password yet)
                // Include(u => u.Employee) loads the related Employee data too
                // We check username first, then verify the hashed password separately
                var user = db.Users
                    .Include(u => u.Employee) // This loads Employee data along with User
                    .FirstOrDefault(u =>
                        u.Username == username &&
                        u.IsActive == true
                    );

                // Step 3.5: Verify the password using BCrypt
                // This compares the entered password with the hashed password in database
                // BCrypt.Verify() safely checks if the plain text password matches the hash
                if (user != null && !PasswordHasher.VerifyPassword(password, user.Password))
                {
                    user = null; // Password didn't match, treat as if user doesn't exist
                }

                // Step 4: Check if we found a matching user
                if (user == null)
                {
                    // No match found - wrong username or password - error checking
                    MessageBox.Show(
                        "Invalid username or password.",
                        "Login Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    return; // Stop here, don't login
                }

                // Step 5: Login successful! Update last login date
                user.LastLoginDate = System.DateTime.Now;
                db.SaveChanges(); // Save the updated last login time

                // Step 6: Open the main dashboard window
                // Pass the logged-in user to the dashboard so it knows who's logged in
                var dashboard = new DashboardWindow(user);
                dashboard.Show();

                // Step 7: Close the login window
                this.Close();
            }
        }
    }
}