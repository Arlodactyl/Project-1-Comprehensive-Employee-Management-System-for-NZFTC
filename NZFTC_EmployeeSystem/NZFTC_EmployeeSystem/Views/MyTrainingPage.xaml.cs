using Microsoft.EntityFrameworkCore;
using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// My Training Page - Shows ONLY the logged-in employee's training records
    /// This is a read-only view for employees to track their training progress
    /// Employees cannot add or edit training - only view their records
    /// </summary>
    public partial class MyTrainingPage : Page
    {
        // The currently logged-in user
        private readonly User _currentUser;

        // Store all training records for the current employee
        private System.Collections.Generic.List<Training> _allTrainings;

        /// <summary>
        /// Constructor - loads the page with the current user's training records
        /// </summary>
        /// <param name="currentUser">The logged-in employee</param>
        public MyTrainingPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            // Set personalized welcome message
            WelcomeText.Text = $"Welcome back, {_currentUser.Employee?.FullName ?? _currentUser.Username}!";

            // Load the employee's training records
            LoadMyTraining();
        }

        /// <summary>
        /// Loads all training records for the current logged-in employee
        /// Filters by EmployeeId to show only their training
        /// </summary>
        private void LoadMyTraining()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // Get ONLY the training records for the current employee
                    // Include the related user who signed off the training
                    _allTrainings = db.Trainings
                        .Include(t => t.SignedOffByUser)
                        .Where(t => t.EmployeeId == _currentUser.EmployeeId)
                        .OrderByDescending(t => t.CompletedDate)
                        .ThenBy(t => t.TrainingType)
                        .ToList();

                    // Display the training records in the grid
                    TrainingGrid.ItemsSource = _allTrainings;

                    // Update the summary statistics
                    UpdateStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading your training records: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Updates the summary cards showing total, completed, and pending training
        /// </summary>
        private void UpdateStatistics()
        {
            if (_allTrainings == null)
            {
                TotalTrainingText.Text = "0";
                CompletedTrainingText.Text = "0";
                PendingTrainingText.Text = "0";
                return;
            }

            // Count total training records
            int total = _allTrainings.Count;

            // Count completed training (status = "Completed")
            int completed = _allTrainings.Count(t => t.Status == "Completed");

            // Count pending training (status = "Not Started" or "In Progress")
            int pending = _allTrainings.Count(t => t.Status == "Not Started" || t.Status == "In Progress");

            // Update the text blocks
            TotalTrainingText.Text = total.ToString();
            CompletedTrainingText.Text = completed.ToString();
            PendingTrainingText.Text = pending.ToString();
        }

        /// <summary>
        /// Search functionality - filters training records as the user types
        /// Searches by training type
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLower().Trim();

            try
            {
                // If search box is empty, show all training
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    TrainingGrid.ItemsSource = _allTrainings;
                    return;
                }

                // Filter the training records by training type
                var filtered = _allTrainings
                    .Where(t => t.TrainingType.ToLower().Contains(searchText))
                    .ToList();

                // Update the grid with filtered results
                TrainingGrid.ItemsSource = filtered;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error searching training: {ex.Message}",
                    "Search Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Double-click handler - shows detailed information about the training record
        /// </summary>
        private void TrainingGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Get the selected training record
            var selectedTraining = TrainingGrid.SelectedItem as Training;

            // If no training is selected, do nothing
            if (selectedTraining == null) return;

            // Create a detailed message showing all training information
            var detailsMessage = $"Training Details\n\n" +
                               $"Training Type: {selectedTraining.TrainingType}\n" +
                               $"Status: {selectedTraining.Status}\n" +
                               $"Completed Date: {(selectedTraining.CompletedDate.HasValue ? selectedTraining.CompletedDate.Value.ToString("dd/MM/yyyy") : "Not completed yet")}\n" +
                               $"Signed Off By: {selectedTraining.SignedOffByUser?.Username ?? "Not signed off"}\n" +
                               $"Notes: {selectedTraining.Notes ?? "No notes"}";

            // Show the details in a popup
            MessageBox.Show(
                detailsMessage,
                "Training Record Details",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        /// <summary>
        /// Exports the employee's training records to a CSV file
        /// Saves to the Downloads folder
        /// </summary>
        private void ExportTraining_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if there are any training records to export
                if (_allTrainings == null || _allTrainings.Count == 0)
                {
                    MessageBox.Show(
                        "You don't have any training records to export.",
                        "No Records",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    return;
                }

                // Create CSV content
                var csv = new StringBuilder();
                csv.AppendLine("My Training Records");
                csv.AppendLine($"Employee: {_currentUser.Employee?.FullName ?? _currentUser.Username}");
                csv.AppendLine($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                csv.AppendLine("");
                csv.AppendLine("ID,Training Type,Status,Completed Date,Signed Off By,Notes");

                // Add each training record to the CSV
                foreach (var training in _allTrainings)
                {
                    csv.AppendLine($"{training.Id}," +
                                  $"\"{training.TrainingType}\"," +
                                  $"\"{training.Status}\"," +
                                  $"{(training.CompletedDate.HasValue ? training.CompletedDate.Value.ToString("dd/MM/yyyy") : "")}," +
                                  $"\"{training.SignedOffByUser?.Username ?? ""}\"," +
                                  $"\"{training.Notes ?? ""}\"");
                }

                // Get the user's Downloads folder
                string downloadsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads");

                // Create filename with current date and time
                string fileName = $"My_Training_Records_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string fullPath = Path.Combine(downloadsPath, fileName);

                // Save the CSV file
                File.WriteAllText(fullPath, csv.ToString());

                // Show success message with file location
                MessageBox.Show(
                    $"Your training records have been exported successfully!\n\nSaved to: {fullPath}",
                    "Export Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error exporting training records: {ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}