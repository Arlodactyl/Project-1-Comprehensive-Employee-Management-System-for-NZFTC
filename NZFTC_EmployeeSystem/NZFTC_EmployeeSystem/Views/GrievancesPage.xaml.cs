using Microsoft.EntityFrameworkCore;
using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Text;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// Interaction logic for GrievancesPage.xaml
    /// Handles submission and management of employee grievances.
    /// </summary>
    public partial class GrievancesPage : Page
    {
        private readonly User _currentUser;

        public GrievancesPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            // Hide admin tab if not admin
            if (_currentUser.Role != "Admin")
            {
                AdminGrievancesTab.Visibility = Visibility.Collapsed;
            }

            LoadGrievances();
        }

        // Load grievances into the grids
        private void LoadGrievances()
        {
            using (var db = new AppDbContext())
            {
                // Load current user's grievances
                var myGrievances = db.Grievances
                    .Where(g => g.EmployeeId == _currentUser.EmployeeId)
                    .OrderByDescending(g => g.RequestDate)
                    .ToList();
                MyGrievancesGrid.ItemsSource = myGrievances;

                // Load all grievances if admin
                if (_currentUser.Role == "Admin")
                {
                    var allGrievances = db.Grievances
                        .Include(g => g.Employee)
                        .OrderByDescending(g => g.RequestDate)
                        .ToList();
                    AllGrievancesGrid.ItemsSource = allGrievances;
                }
            }
        }

        // Validate grievance form input
        private bool ValidateGrievanceForm()
        {
            // Reset validation message
            ValidationMessageTextBlock.Visibility = Visibility.Collapsed;
            ValidationMessageTextBlock.Text = "";

            // Check grievance type
            if (GrievanceTypeComboBox.SelectedIndex == 0 || GrievanceTypeComboBox.SelectedItem == null)
            {
                ShowValidationError("Please select a grievance type.");
                return false;
            }

            // Check title
            if (string.IsNullOrWhiteSpace(GrievanceTitleTextBox.Text))
            {
                ShowValidationError("Please enter a title for your grievance.");
                GrievanceTitleTextBox.Focus();
                return false;
            }

            if (GrievanceTitleTextBox.Text.Trim().Length < 5)
            {
                ShowValidationError("Title must be at least 5 characters long.");
                GrievanceTitleTextBox.Focus();
                return false;
            }

            // Check incident date(s)
            if (IncidentDateCalendar.SelectedDates.Count == 0)
            {
                ShowValidationError("Please select at least one incident date.");
                return false;
            }

            // Check description
            if (string.IsNullOrWhiteSpace(GrievanceDescriptionTextBox.Text))
            {
                ShowValidationError("Please enter a description for your grievance.");
                GrievanceDescriptionTextBox.Focus();
                return false;
            }

            if (GrievanceDescriptionTextBox.Text.Trim().Length < 20)
            {
                ShowValidationError("Description must be at least 20 characters long to provide sufficient detail.");
                GrievanceDescriptionTextBox.Focus();
                return false;
            }

            return true;
        }

        // Show validation error message
        private void ShowValidationError(string message)
        {
            ValidationMessageTextBlock.Text = message;
            ValidationMessageTextBlock.Visibility = Visibility.Visible;
        }

        // Handles submission of a new grievance
        private void SubmitGrievance_Click(object sender, RoutedEventArgs e)
        {
            // Validate form
            if (!ValidateGrievanceForm())
            {
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    // Get selected grievance type
                    var selectedType = (GrievanceTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                    // Format selected dates as comma-separated string
                    var selectedDates = IncidentDateCalendar.SelectedDates
                        .OrderBy(d => d)
                        .Select(d => d.ToString("dd/MM/yyyy"))
                        .ToList();
                    var incidentDatesString = string.Join(", ", selectedDates);

                    var grievance = new Grievance
                    {
                        EmployeeId = _currentUser.EmployeeId,
                        GrievanceType = selectedType,
                        Title = GrievanceTitleTextBox.Text.Trim(),
                        Description = GrievanceDescriptionTextBox.Text.Trim(),
                        IncidentDates = incidentDatesString,
                        Status = "Open",
                        RequestDate = DateTime.Now
                    };

                    db.Grievances.Add(grievance);
                    db.SaveChanges();
                }

                MessageBox.Show(
                    "Your grievance has been submitted successfully and will be reviewed by HR.\n\nYou can track its status in the 'My Grievances' tab.",
                    "Grievance Submitted",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Clear the form
                GrievanceTypeComboBox.SelectedIndex = 0;
                GrievanceTitleTextBox.Clear();
                GrievanceDescriptionTextBox.Clear();
                IncidentDateCalendar.SelectedDates.Clear();
                ValidationMessageTextBlock.Visibility = Visibility.Collapsed;

                // Reload grievances
                LoadGrievances();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred while submitting your grievance:\n{ex.Message}",
                    "Submission Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Handles responding to a grievance (admin)
        private void Respond_Click(object sender, RoutedEventArgs e)
        {
            var selected = AllGrievancesGrid.SelectedItem as Grievance;

            if (selected == null)
            {
                MessageBox.Show(
                    "Please select a grievance from the list to respond to.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(AdminResponseTextBox.Text))
            {
                MessageBox.Show(
                    "Please enter a response before saving.",
                    "Response Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                AdminResponseTextBox.Focus();
                return;
            }

            if (AdminResponseTextBox.Text.Trim().Length < 10)
            {
                MessageBox.Show(
                    "Response must be at least 10 characters long.",
                    "Response Too Short",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                AdminResponseTextBox.Focus();
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    var grievance = db.Grievances.Find(selected.Id);
                    if (grievance != null)
                    {
                        grievance.AdminResponse = AdminResponseTextBox.Text.Trim();
                        grievance.Status = "InProgress";
                        grievance.HandledByUserId = _currentUser.Id;
                        db.SaveChanges();
                    }
                }

                MessageBox.Show(
                    "Response saved successfully. The employee will be notified.",
                    "Response Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                AdminResponseTextBox.Clear();
                LoadGrievances();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred while saving the response:\n{ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Handles closing a grievance (admin)
        private void CloseGrievance_Click(object sender, RoutedEventArgs e)
        {
            var selected = AllGrievancesGrid.SelectedItem as Grievance;

            if (selected == null)
            {
                MessageBox.Show(
                    "Please select a grievance from the list to close.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to close this grievance?\n\nEmployee: {selected.Employee?.FullName}\nTitle: {selected.Title}\n\nThis action cannot be undone.",
                "Confirm Close",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var grievance = db.Grievances.Find(selected.Id);
                    if (grievance != null)
                    {
                        grievance.Status = "Closed";
                        grievance.HandledByUserId = _currentUser.Id;
                        grievance.ResolvedDate = DateTime.Now;
                        db.SaveChanges();
                    }
                }

                MessageBox.Show(
                    "Grievance has been closed successfully.",
                    "Grievance Closed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                AdminResponseTextBox.Clear();
                LoadGrievances();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred while closing the grievance:\n{ex.Message}",
                    "Close Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Export employee's own grievances to CSV
        private void ExportMyGrievances_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var grievances = db.Grievances
                        .Where(g => g.EmployeeId == _currentUser.EmployeeId)
                        .OrderByDescending(g => g.RequestDate)
                        .ToList();

                    if (grievances.Count == 0)
                    {
                        MessageBox.Show(
                            "You have no grievances to export.",
                            "No Data",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        return;
                    }

                    ExportGrievancesToCsv(grievances, "My_Grievances");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred while exporting grievances:\n{ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Export all grievances to CSV (admin only)
        private void ExportAllGrievances_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var grievances = db.Grievances
                        .Include(g => g.Employee)
                        .OrderByDescending(g => g.RequestDate)
                        .ToList();

                    if (grievances.Count == 0)
                    {
                        MessageBox.Show(
                            "There are no grievances to export.",
                            "No Data",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        return;
                    }

                    ExportGrievancesToCsv(grievances, "All_Grievances");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred while exporting grievances:\n{ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Helper method to export grievances to CSV file
        private void ExportGrievancesToCsv(System.Collections.Generic.List<Grievance> grievances, string filePrefix)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = ".csv",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var csv = new StringBuilder();

                    // Add header
                    if (_currentUser.Role == "Admin")
                    {
                        csv.AppendLine("ID,Employee Name,Type,Title,Incident Date(s),Description,Status,Submitted Date,Admin Response,Resolved Date");
                    }
                    else
                    {
                        csv.AppendLine("ID,Type,Title,Incident Date(s),Description,Status,Submitted Date,Admin Response,Resolved Date");
                    }

                    // Add data rows
                    foreach (var g in grievances)
                    {
                        var line = _currentUser.Role == "Admin"
                            ? $"{g.Id},\"{EscapeCsv(g.Employee?.FullName ?? "N/A")}\",\"{EscapeCsv(g.GrievanceType)}\",\"{EscapeCsv(g.Title)}\",\"{EscapeCsv(g.IncidentDates ?? "Not specified")}\",\"{EscapeCsv(g.Description)}\",{g.Status},{g.RequestDate:dd/MM/yyyy},\"{EscapeCsv(g.AdminResponse ?? "No response yet")}\",{(g.ResolvedDate.HasValue ? g.ResolvedDate.Value.ToString("dd/MM/yyyy") : "Not resolved")}"
                            : $"{g.Id},\"{EscapeCsv(g.GrievanceType)}\",\"{EscapeCsv(g.Title)}\",\"{EscapeCsv(g.IncidentDates ?? "Not specified")}\",\"{EscapeCsv(g.Description)}\",{g.Status},{g.RequestDate:dd/MM/yyyy},\"{EscapeCsv(g.AdminResponse ?? "No response yet")}\",{(g.ResolvedDate.HasValue ? g.ResolvedDate.Value.ToString("dd/MM/yyyy") : "Not resolved")}";

                        csv.AppendLine(line);
                    }

                    File.WriteAllText(saveDialog.FileName, csv.ToString());

                    MessageBox.Show(
                        $"Successfully exported {grievances.Count} grievance(s) to:\n{saveDialog.FileName}",
                        "Export Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to save the file:\n{ex.Message}",
                        "Save Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        // Helper method to escape CSV special characters
        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Replace quotes with double quotes and handle line breaks
            return value.Replace("\"", "\"\"").Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
        }

        // Show information dialog about grievances in NZ context
        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            var infoMessage = @"What is a Grievance in New Zealand?

A grievance is a formal concern or complaint raised by an employee about their employment. Under New Zealand employment law, employers must:

Key Points:
• Have a fair and reasonable process for handling grievances
• Act in good faith when addressing employee concerns
• Follow natural justice principles
• Maintain confidentiality where possible

Common Types of Grievances:
• Workplace harassment or bullying
• Discrimination
• Unfair treatment
• Breach of employment agreement
• Health and safety concerns
• Pay or entitlements disputes

Your Rights:
 Raise concerns without fear of retaliation
Have a support person present in meetings
 Receive a fair and timely response
 Escalate to mediation if unresolved
 Seek advice from Employment New Zealand

For more information, visit: www.employment.govt.nz

All grievances submitted through this system are treated as confidential and will be handled in accordance with your employment agreement and New Zealand employment law.";

            MessageBox.Show(
                infoMessage,
                "Understanding Grievances in NZ",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // New Grievance button clicked - navigates to submit form (removed as tabs are directly accessible)
        // Keeping method for backwards compatibility
        private void NewGrievance_Click(object sender, RoutedEventArgs e)
        {
            GrievanceTabControl.SelectedItem = NewGrievanceTab;
        }

        // View Status button clicked - navigates to my grievances (removed as tabs are directly accessible)
        // Keeping method for backwards compatibility
        private void ViewStatus_Click(object sender, RoutedEventArgs e)
        {
            GrievanceTabControl.SelectedItem = MyGrievancesTab;
        }

        // Upload Notes button clicked - placeholder for file upload (removed from UI but keeping for backwards compatibility)
        private void UploadNotes_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "File upload feature coming soon!",
                "Upload Notes",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}