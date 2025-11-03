using Microsoft.EntityFrameworkCore;
using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using NZFTC_EmployeeSystem.Views;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// Dashboard window - shows after login with navigation menu and user's profile picture
    /// </summary>
    public partial class DashboardWindow : Window
    {
        private readonly User _currentUser;

        public DashboardWindow(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            WelcomeText.Text = $"Welcome, {_currentUser.Employee?.FullName ?? _currentUser.Username}";
            LoadUserProfilePicture();

            if (_currentUser.Role == "Admin")
            {
                AdminMenuPanel.Visibility = Visibility.Visible;
            }
        }

        // Load user's profile picture from ProfilePictures folder
        private void LoadUserProfilePicture()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var employee = db.Employees.FirstOrDefault(e => e.Id == _currentUser.EmployeeId);

                    if (employee != null && !string.IsNullOrEmpty(employee.ProfilePicturePath))
                    {
                        string fullPath = GetProfilePicturePath(employee.ProfilePicturePath);

                        if (File.Exists(fullPath))
                        {
                            try
                            {
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.EndInit();

                                UserProfilePicture.Source = bitmap;
                                UserProfilePicture.Visibility = Visibility.Visible;
                                DefaultUserAvatar.Visibility = Visibility.Collapsed;
                            }
                            catch
                            {
                                UserProfilePicture.Visibility = Visibility.Collapsed;
                                DefaultUserAvatar.Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
            }
            catch
            {
                UserProfilePicture.Visibility = Visibility.Collapsed;
                DefaultUserAvatar.Visibility = Visibility.Visible;
            }
        }

        // Get full path to profile picture
        private string GetProfilePicturePath(string fileName)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName ?? baseDir;
            var picturesFolder = Path.Combine(projectRoot, "ProfilePictures");
            return Path.Combine(picturesFolder, fileName);
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            try { ContentFrame.Navigate(new DashboardHomePage(_currentUser)); }
            catch { MessageBox.Show("Dashboard page coming soon", "Info"); }
        }

        private void MyProfile_Click(object sender, RoutedEventArgs e)
        {
            try { ContentFrame.Navigate(new ProfilePage(_currentUser)); }
            catch { MessageBox.Show("Profile page coming soon", "Info"); }
        }

        private void LeaveManagement_Click(object sender, RoutedEventArgs e)
        {
            try { ContentFrame.Navigate(new LeavePage(_currentUser)); }
            catch { MessageBox.Show("Leave page coming soon", "Info"); }
        }

        private void Payroll_Click(object sender, RoutedEventArgs e)
        {
            try { ContentFrame.Navigate(new PayrollPage(_currentUser)); }
            catch { MessageBox.Show("Payroll page coming soon", "Info"); }
        }

        private void Holidays_Click(object sender, RoutedEventArgs e)
        {
            try { ContentFrame.Navigate(new HolidaysPage(_currentUser)); }
            catch { MessageBox.Show("Holidays page coming soon", "Info"); }
        }

        private void Grievances_Click(object sender, RoutedEventArgs e)
        {
            try { ContentFrame.Navigate(new GrievancesPage(_currentUser)); }
            catch { MessageBox.Show("Grievances page coming soon", "Info"); }
        }

        private void ManageEmployees_Click(object sender, RoutedEventArgs e)
        {
            try { ContentFrame.Navigate(new EmployeeManagementPage(_currentUser)); }
            catch { MessageBox.Show("Employee Management page coming soon", "Info"); }
        }

        private void Departments_Click(object sender, RoutedEventArgs e)
        {
            try { ContentFrame.Navigate(new DepartmentsPage(_currentUser)); }
            catch { MessageBox.Show("Departments page coming soon", "Info"); }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}