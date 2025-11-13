using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// Profile page - shows employee info and lets them upload a picture or choose from preset avatars.
    /// Pictures are saved in the ProfilePictures folder.
    /// Avatars are stored in Images folder in the project.
    /// </summary>
    public partial class ProfilePage : Page
    {
        private readonly User _currentUser;

        public ProfilePage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            LoadProfile();
        }

        /// <summary>
        /// Load employee details and show them on the page
        /// </summary>
        private void LoadProfile()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var employee = db.Employees
                        .Include(e => e.Department)
                        .FirstOrDefault(e => e.Id == _currentUser.EmployeeId);

                    if (employee != null)
                    {
                        FullNameText.Text = employee.FullName;
                        EmailText.Text = employee.Email;
                        PhoneText.Text = employee.PhoneNumber;
                        JobTitleText.Text = employee.JobTitle;
                        DepartmentText.Text = employee.Department?.Name ?? string.Empty;
                        HireDateText.Text = employee.HireDate.ToString("dd/MM/yyyy");
                        SalaryText.Text = employee.Salary.ToString("C");
                        AnnualLeaveText.Text = employee.AnnualLeaveBalance.ToString();
                        SickLeaveText.Text = employee.SickLeaveBalance.ToString();

                        LoadProfilePicture(employee.ProfilePicturePath);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading profile: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Load and show the profile picture or avatar
        /// </summary>
        private void LoadProfilePicture(string? picturePath)
        {
            if (string.IsNullOrEmpty(picturePath))
            {
                ProfilePictureImage.Visibility = Visibility.Collapsed;
                DefaultAvatar.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                // Check if it's a preset avatar
                if (picturePath.StartsWith("avatar_"))
                {
                    LoadAvatarImage(picturePath);
                }
                else
                {
                    // It's a custom uploaded picture
                    LoadCustomPicture(picturePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading picture: {ex.Message}");
                ProfilePictureImage.Visibility = Visibility.Collapsed;
                DefaultAvatar.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Load a preset avatar from Images folder
        /// </summary>
        private void LoadAvatarImage(string avatarFileName)
        {
            try
            {
                // Extract the actual filename (hamster.png, panda.png, etc.)
                string actualFileName = avatarFileName.Replace("avatar_", "");

                // Build pack URI for embedded resource
                string packUri = $"/Images/{actualFileName}";

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(packUri, UriKind.Relative);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                ProfilePictureImage.Source = bitmap;
                ProfilePictureImage.Visibility = Visibility.Visible;
                DefaultAvatar.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading avatar: {ex.Message}");
                ProfilePictureImage.Visibility = Visibility.Collapsed;
                DefaultAvatar.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Load a custom uploaded picture from ProfilePictures folder
        /// </summary>
        private void LoadCustomPicture(string picturePath)
        {
            string fullPath = GetProfilePicturePath(picturePath);

            if (File.Exists(fullPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ProfilePictureImage.Source = bitmap;
                    ProfilePictureImage.Visibility = Visibility.Visible;
                    DefaultAvatar.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading custom picture: {ex.Message}");
                    ProfilePictureImage.Visibility = Visibility.Collapsed;
                    DefaultAvatar.Visibility = Visibility.Visible;
                }
            }
            else
            {
                ProfilePictureImage.Visibility = Visibility.Collapsed;
                DefaultAvatar.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Get full path to profile picture in ProfilePictures folder
        /// </summary>
        private string GetProfilePicturePath(string fileName)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName ?? baseDir;
            var picturesFolder = Path.Combine(projectRoot, "ProfilePictures");
            return Path.Combine(picturesFolder, fileName);
        }

        /// <summary>
        /// Make sure ProfilePictures folder exists
        /// </summary>
        private string EnsureProfilePicturesFolder()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName ?? baseDir;
            var picturesFolder = Path.Combine(projectRoot, "ProfilePictures");

            if (!Directory.Exists(picturesFolder))
            {
                Directory.CreateDirectory(picturesFolder);
            }

            return picturesFolder;
        }

        /// <summary>
        /// Handle avatar selection button click
        /// </summary>
        private void SelectAvatar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string avatarFileName)
                {
                    // Save avatar selection to database with "avatar_" prefix
                    using (var db = new AppDbContext())
                    {
                        var employee = db.Employees.Find(_currentUser.EmployeeId);
                        if (employee != null)
                        {
                            employee.ProfilePicturePath = $"avatar_{avatarFileName}";
                            db.SaveChanges();

                            MessageBox.Show(
                                "Avatar selected successfully!",
                                "Success",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                            LoadProfilePicture(employee.ProfilePicturePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error selecting avatar: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle Upload Picture button click
        /// </summary>
        private void UploadPicture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open file picker
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Select Profile Picture",
                    Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedFile = openFileDialog.FileName;

                    if (!File.Exists(selectedFile))
                    {
                        MessageBox.Show(
                            "File not found.",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    // Create ProfilePictures folder if needed
                    string picturesFolder = EnsureProfilePicturesFolder();

                    // Create unique filename: employee_2.jpg
                    string extension = Path.GetExtension(selectedFile);
                    string newFileName = $"employee_{_currentUser.EmployeeId}{extension}";
                    string destinationPath = Path.Combine(picturesFolder, newFileName);

                    // Copy picture to ProfilePictures folder
                    File.Copy(selectedFile, destinationPath, overwrite: true);

                    // Save filename (not full path) to database
                    using (var db = new AppDbContext())
                    {
                        var employee = db.Employees.Find(_currentUser.EmployeeId);
                        if (employee != null)
                        {
                            employee.ProfilePicturePath = newFileName;
                            db.SaveChanges();
                        }
                    }

                    MessageBox.Show(
                        "Picture uploaded successfully!",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    LoadProfilePicture(newFileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error uploading picture: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Shows help information for the Profile page
        /// </summary>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpMessage = "Profile Page Help\n\n" +
                "View Your Information:\n" +
                "- Your personal details are displayed here\n" +
                "- This includes contact info, job title, and department\n" +
                "- Leave balances show your remaining days off\n\n" +
                "Profile Picture:\n" +
                "- Choose from 4 preset avatars (hamster, panda, bee, frog)\n" +
                "- Or click 'Upload Custom' for your own photo\n" +
                "- Accepted formats: JPG, PNG, BMP\n" +
                "- Your picture will appear throughout the system\n\n" +
                "Salary Information:\n" +
                "- Your current salary is displayed\n" +
                "- Contact HR or your manager for salary inquiries\n\n" +
                "Leave Balances:\n" +
                "- Annual Leave: Yearly vacation days\n" +
                "- Sick Leave: Days available for illness\n" +
                "- Balances update when leave is approved\n\n";

            if (_currentUser.Role == "Admin")
            {
                helpMessage += "Admin Note:\n" +
                    "To edit employee information, use Employee Management page";
            }

            MessageBox.Show(
                helpMessage,
                "Profile Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}