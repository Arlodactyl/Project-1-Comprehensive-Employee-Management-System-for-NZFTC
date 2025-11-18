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
    public partial class ProfilePage : Page
    {
        private readonly User _currentUser;

        public ProfilePage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            LoadProfile();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadProfile();
        }

        private void LoadProfile()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // Get employee with department information
                    var employee = db.Employees
                        .Include(e => e.Department)
                        .FirstOrDefault(e => e.Id == _currentUser.EmployeeId);

                    if (employee != null)
                    {
                        // Display employee information
                        FullNameText.Text = employee.FullName;
                        EmailText.Text = employee.Email;
                        PhoneText.Text = employee.PhoneNumber;
                        JobTitleText.Text = employee.JobTitle;
                        DepartmentText.Text = employee.Department?.Name ?? string.Empty;
                        HireDateText.Text = employee.HireDate.ToString("dd/MM/yyyy");
                        SalaryText.Text = employee.Salary.ToString("C");
                        AnnualLeaveText.Text = employee.AnnualLeaveBalance.ToString();
                        SickLeaveText.Text = employee.SickLeaveBalance.ToString();

                        // Load profile picture
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

        private void LoadProfilePicture(string? picturePath)
        {
            if (string.IsNullOrEmpty(picturePath))
            {
                // No picture set, show default avatar
                ProfilePictureImage.Visibility = Visibility.Collapsed;
                DefaultAvatar.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                // Check if it's a preset avatar or custom picture
                if (picturePath.StartsWith("avatar_"))
                {
                    // It's a preset avatar from /Images folder
                    LoadAvatarImage(picturePath);
                }
                else
                {
                    // It's a custom uploaded picture from ProfilePictures folder
                    LoadCustomPicture(picturePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading picture: {ex.Message}");
                // On any error, show default red avatar
                ProfilePictureImage.Source = null;
                ProfilePictureImage.Visibility = Visibility.Collapsed;
                DefaultAvatar.Visibility = Visibility.Visible;
            }
        }

        private void LoadAvatarImage(string avatarFileName)
        {
            try
            {
                // Clear previous image source
                ProfilePictureImage.Source = null;

                // Extract actual filename (remove "avatar_" prefix)
                string actualFileName = avatarFileName.Replace("avatar_", "");

                // Build pack URI for embedded resource images
                string packUri = $"pack://application:,,,/Images/{actualFileName}";

                System.Diagnostics.Debug.WriteLine($"Loading avatar: {actualFileName}");
                System.Diagnostics.Debug.WriteLine($"Pack URI: {packUri}");

                // Create and load the bitmap
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(packUri, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // Freeze for better performance

                System.Diagnostics.Debug.WriteLine($"Bitmap loaded successfully");

                // Update UI - only after successful load
                ProfilePictureImage.Source = bitmap;
                ProfilePictureImage.Visibility = Visibility.Visible;
                DefaultAvatar.Visibility = Visibility.Collapsed;

                System.Diagnostics.Debug.WriteLine($"Avatar display complete - image should be visible");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR loading avatar: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Show default red avatar on error
                ProfilePictureImage.Source = null;
                ProfilePictureImage.Visibility = Visibility.Collapsed;
                DefaultAvatar.Visibility = Visibility.Visible;
            }
        }

        private void LoadCustomPicture(string picturePath)
        {
            string fullPath = GetProfilePicturePath(picturePath);

            System.Diagnostics.Debug.WriteLine($"LoadCustomPicture called");
            System.Diagnostics.Debug.WriteLine($"  Picture path parameter: {picturePath}");
            System.Diagnostics.Debug.WriteLine($"  Full resolved path: {fullPath}");
            System.Diagnostics.Debug.WriteLine($"  File exists: {File.Exists(fullPath)}");

            if (!File.Exists(fullPath))
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: File does not exist at: {fullPath}");
                ProfilePictureImage.Source = null;
                ProfilePictureImage.Visibility = Visibility.Collapsed;
                DefaultAvatar.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                // Clear any previous image to release file locks
                ProfilePictureImage.Source = null;

                // Force UI update
                ProfilePictureImage.UpdateLayout();

                System.Diagnostics.Debug.WriteLine($"  Opening file stream...");

                // Load image from file stream - FIXED: Removed CreateOptions that was causing the error
                using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    System.Diagnostics.Debug.WriteLine($"  File stream opened, length: {stream.Length} bytes");

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    // REMOVED: bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze(); // Freeze for better performance and to release the stream

                    System.Diagnostics.Debug.WriteLine($"  Bitmap created, size: {bitmap.PixelWidth}x{bitmap.PixelHeight}");

                    ProfilePictureImage.Source = bitmap;
                }

                System.Diagnostics.Debug.WriteLine($"Custom picture loaded successfully");

                // Update visibility after successful load
                ProfilePictureImage.Visibility = Visibility.Visible;
                DefaultAvatar.Visibility = Visibility.Collapsed;

                System.Diagnostics.Debug.WriteLine($"Visibility updated: ProfilePictureImage=Visible, DefaultAvatar=Collapsed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR loading custom picture: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                ProfilePictureImage.Source = null;
                ProfilePictureImage.Visibility = Visibility.Collapsed;
                DefaultAvatar.Visibility = Visibility.Visible;
            }
        }

        private string GetProfilePicturePath(string fileName)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName ?? baseDir;
            var picturesFolder = Path.Combine(projectRoot, "ProfilePictures");
            return Path.Combine(picturesFolder, fileName);
        }

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

        private void SelectAvatar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string avatarFileName)
                {
                    System.Diagnostics.Debug.WriteLine($"Avatar clicked: {avatarFileName}");

                    // Clear current image (but don't show red dot yet)
                    ProfilePictureImage.Source = null;

                    // Delete any old custom pictures for this employee when switching to avatar
                    try
                    {
                        string picturesFolder = EnsureProfilePicturesFolder();
                        string[] patterns = { "*.jpg", "*.jpeg", "*.png", "*.bmp" };
                        foreach (string pattern in patterns)
                        {
                            string searchPattern = $"employee_{_currentUser.EmployeeId}_*";
                            string[] oldFiles = Directory.GetFiles(picturesFolder, searchPattern + pattern.Substring(1));
                            foreach (string oldFile in oldFiles)
                            {
                                try
                                {
                                    File.Delete(oldFile);
                                    System.Diagnostics.Debug.WriteLine($"Deleted old custom picture: {oldFile}");
                                }
                                catch (Exception deleteEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Could not delete old picture: {deleteEx.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Cleanup error: {cleanupEx.Message}");
                        // Continue even if cleanup fails
                    }

                    using (var db = new AppDbContext())
                    {
                        // Find employee and update their avatar
                        var employee = db.Employees.Find(_currentUser.EmployeeId);
                        if (employee != null)
                        {
                            string newPath = $"avatar_{avatarFileName}";
                            System.Diagnostics.Debug.WriteLine($"Saving to database: {newPath}");

                            employee.ProfilePicturePath = newPath;
                            db.SaveChanges();

                            System.Diagnostics.Debug.WriteLine($"Database saved, now loading picture");

                            // Reload the profile picture BEFORE showing message
                            LoadProfilePicture(newPath);

                            // Update the dashboard avatar too
                            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
                            if (dashboardWindow != null)
                            {
                                dashboardWindow.RefreshUserAvatar();
                            }

                            MessageBox.Show(
                                "Avatar selected successfully!",
                                "Success",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"ERROR: Employee not found with ID: {_currentUser.EmployeeId}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: Button or Tag is null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in SelectAvatar_Click: {ex.Message}");
                MessageBox.Show(
                    $"Error selecting avatar: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UploadPicture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

                    // Clear current image FIRST
                    ProfilePictureImage.Source = null;
                    DefaultAvatar.Visibility = Visibility.Visible;
                    ProfilePictureImage.Visibility = Visibility.Collapsed;

                    // Force UI update
                    this.UpdateLayout();
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new Action(() => { }));

                    // Create unique filename with timestamp - this forces a NEW file each time
                    string picturesFolder = EnsureProfilePicturesFolder();
                    string extension = Path.GetExtension(selectedFile);
                    long timestamp = DateTime.Now.Ticks;
                    string newFileName = $"employee_{_currentUser.EmployeeId}_{timestamp}{extension}";
                    string destinationPath = Path.Combine(picturesFolder, newFileName);

                    // Delete ALL old pictures for this employee (all extensions)
                    try
                    {
                        string[] patterns = { "*.jpg", "*.jpeg", "*.png", "*.bmp" };
                        foreach (string pattern in patterns)
                        {
                            string searchPattern = $"employee_{_currentUser.EmployeeId}_*";
                            string[] oldFiles = Directory.GetFiles(picturesFolder, searchPattern + pattern.Substring(1));
                            foreach (string oldFile in oldFiles)
                            {
                                try
                                {
                                    File.Delete(oldFile);
                                }
                                catch
                                {
                                    // Ignore if can't delete old file
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Continue even if cleanup fails
                    }

                    // Copy new picture
                    System.Diagnostics.Debug.WriteLine($"Copying file from: {selectedFile}");
                    System.Diagnostics.Debug.WriteLine($"Copying file to: {destinationPath}");
                    File.Copy(selectedFile, destinationPath, true); // Overwrite if exists

                    // Verify file was copied
                    if (!File.Exists(destinationPath))
                    {
                        throw new Exception($"File was not copied successfully to {destinationPath}");
                    }
                    System.Diagnostics.Debug.WriteLine($"File copied successfully, size: {new FileInfo(destinationPath).Length} bytes");

                    // Update database with new filename
                    using (var db = new AppDbContext())
                    {
                        var employee = db.Employees.Find(_currentUser.EmployeeId);
                        if (employee != null)
                        {
                            employee.ProfilePicturePath = newFileName;
                            db.SaveChanges();
                            System.Diagnostics.Debug.WriteLine($"Database updated with filename: {newFileName}");
                        }
                        else
                        {
                            throw new Exception($"Employee not found with ID: {_currentUser.EmployeeId}");
                        }
                    }

                    // Small delay to ensure file is fully written
                    System.Threading.Thread.Sleep(300);

                    // Load the new profile picture
                    System.Diagnostics.Debug.WriteLine($"About to load profile picture: {newFileName}");
                    LoadProfilePicture(newFileName);
                    System.Diagnostics.Debug.WriteLine($"Profile picture loaded");

                    // Update the dashboard avatar too
                    var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
                    if (dashboardWindow != null)
                    {
                        dashboardWindow.RefreshUserAvatar();
                    }

                    MessageBox.Show(
                        "Picture uploaded successfully!",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in UploadPicture_Click: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show(
                    $"Error uploading picture: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpMessage = "Profile Page Help\n\n" +
                "View Your Information:\n" +
                "• Personal details including contact info and job title\n" +
                "• Department and hire date information\n" +
                "• Current salary and leave balances\n\n" +
                "Profile Picture:\n" +
                "• Choose from 4 preset avatars (hamster, panda, bee, frog)\n" +
                "• Or upload your own photo using 'Upload Custom'\n" +
                "• Accepted formats: JPG, PNG, BMP\n" +
                "• Your picture appears throughout the system\n\n" +
                "Leave Balances:\n" +
                "• Annual Leave: Vacation days remaining\n" +
                "• Sick Leave: Days available for illness\n" +
                "• Balances automatically update when leave is approved\n\n";

            if (_currentUser.Role == "Admin")
            {
                helpMessage += "Admin Note:\n" +
                    "To edit employee details, use the Employee Management page.";
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