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
    /// Profile page - shows employee info and lets them upload a picture.
    /// Pictures are saved in the ProfilePictures folder so they work on any computer.
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

        // Load employee details and show them on the page
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
                MessageBox.Show($"Error loading profile: {ex.Message}", "Error");
            }
        }

        // Load and show the profile picture
        private void LoadProfilePicture(string? picturePath)
        {
            if (!string.IsNullOrEmpty(picturePath))
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
                    catch
                    {
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
            else
            {
                ProfilePictureImage.Visibility = Visibility.Collapsed;
                DefaultAvatar.Visibility = Visibility.Visible;
            }
        }

        // Get full path to profile picture in ProfilePictures folder
        private string GetProfilePicturePath(string fileName)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName ?? baseDir;
            var picturesFolder = Path.Combine(projectRoot, "ProfilePictures");
            return Path.Combine(picturesFolder, fileName);
        }

        // Make sure ProfilePictures folder exists
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

        // Handle Upload Picture button click
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
                        MessageBox.Show("File not found.", "Error");
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

                    MessageBox.Show("Picture uploaded successfully!", "Success");
                    LoadProfilePicture(newFileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error uploading picture: {ex.Message}", "Error");
            }
        }
    }
}