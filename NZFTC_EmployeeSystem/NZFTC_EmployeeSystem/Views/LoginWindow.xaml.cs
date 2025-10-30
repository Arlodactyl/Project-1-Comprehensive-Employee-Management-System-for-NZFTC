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
        }

        // ========================================
        // CANCEL BUTTON - Close the window
        // ========================================
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close(); // This closes the login window and exits the app
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
                // Search the Users table for a matching username and password
                // Include(u => u.Employee) loads the related Employee data too
                var user = db.Users
                    .Include(u => u.Employee) // This loads Employee data along with User
                    .FirstOrDefault(u =>
                        u.Username == username &&
                        u.Password == password &&
                        u.IsActive == true
                    );

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