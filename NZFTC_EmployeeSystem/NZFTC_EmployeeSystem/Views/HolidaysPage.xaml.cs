using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NZFTC_EmployeeSystem.Views
{

    /// This page displays a list of company holidays with countdown timers.

    public partial class HolidaysPage : Page
    {
        // Store the current logged-in user so we know who's viewing the page aka admin or employee
        private readonly User _currentUser;
        private List<HolidayViewModel> _allHolidays = new List<HolidayViewModel>();

      
        public HolidaysPage(User currentUser)
        {
            try
            {
                InitializeComponent();
                _currentUser = currentUser;

                // Configure role-based access 
                ConfigureRoleBasedAccess();

                // Set date picker constraints
                ConfigureDatePicker();

                // Load all holidays from the database and display them to user
                LoadHolidayData();

                // Set the default selection for holiday type dropdown to "Public"
                if (HolidayTypeComboBox != null)
                {
                    HolidayTypeComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error initializing Holidays page: {ex.Message}\n\nPlease contact your system administrator.",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

       
        private void ConfigureRoleBasedAccess()
        {
            try
            {
                if (_currentUser.Role != "Admin")
                {
                    // Hide admin-only controls for non-admin users
                    if (AdminActionsColumn != null)
                    {
                        AdminActionsColumn.Visibility = Visibility.Collapsed;
                    }

                    if (AddHolidayButton != null)
                    {
                        AddHolidayButton.Visibility = Visibility.Collapsed;
                    }

                    if (AddHolidayPanel != null)
                    {
                        AddHolidayPanel.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    // Admin can see everything here
                    if (AdminActionsColumn != null)
                    {
                        AdminActionsColumn.Visibility = Visibility.Visible;
                    }

                    if (AddHolidayButton != null)
                    {
                        AddHolidayButton.Visibility = Visibility.Visible;
                    }

                    // Form starts hidden until user clicks Add Holiday button
                    if (AddHolidayPanel != null)
                    {
                        AddHolidayPanel.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configuring role-based access: {ex.Message}");
            }
        }

  
        private void ConfigureDatePicker()
        {
            try
            {
                // Set date picker to only allow today or future dates
                if (HolidayDatePicker != null)
                {
                    HolidayDatePicker.DisplayDateStart = DateTime.Today;
                    // Set maximum date to 10 years in future
                    HolidayDatePicker.DisplayDateEnd = DateTime.Today.AddYears(10);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configuring date picker: {ex.Message}");
            }
        }

        
        private void LoadHolidayData()
        {
            try
            {
                // 'using' ensures the database connection is closed properly after use
                using (var db = new AppDbContext())
                {
                    // Query the Holidays table and order by date (earliest first)
                    var holidays = db.Holidays
                        .OrderBy(h => h.Date)
                        .ToList();

                    // Convert the holidays to HolidayViewModel which includes countdown calculation
                    _allHolidays = holidays.Select(h => new HolidayViewModel
                    {
                        Id = h.Id,
                        Name = h.Name,
                        Date = h.Date,
                        Type = h.Type,
                        Description = h.Description,
                        IsRecurring = h.IsRecurring,
                        CreatedDate = h.CreatedDate,
                        CreatedByUserId = h.CreatedByUserId,
                        // Calculate the countdown string by calling the helper method
                        DaysUntil = CalculateCountdown(h.Date)
                    }).ToList();

                    // For admin grid: filter out passed holidays
                    // For employee grid: show all holidays
                    var displayHolidays = _allHolidays;

                    if (_currentUser.Role == "Admin")
                    {
                        // Admins only see current and future holidays in the grid
                        displayHolidays = _allHolidays
                            .Where(h => h.Date >= DateTime.Today)
                            .ToList();
                    }

                    // Show holidays in the grid
                    if (HolidaysGrid != null)
                    {
                        HolidaysGrid.ItemsSource = displayHolidays;
                    }

                    // Filter to only show upcoming holidays in the timeline (next 12 months)
                    var upcomingHolidays = _allHolidays
                        .Where(h => h.Date >= DateTime.Today && h.Date <= DateTime.Today.AddMonths(12))
                        .Take(10) // Limit to 10 holidays to prevent overcrowding
                        .ToList();

                    // Bind the upcoming holidays to the timeline control
                    if (HolidayTimeline != null)
                    {
                        HolidayTimeline.ItemsSource = upcomingHolidays;
                    }
                }
            }
            catch (Exception ex)
            {
                // If something goes wrong, show an error message for feedback purposes
                MessageBox.Show(
                    $"Error loading holidays: {ex.Message}\n\nPlease ensure the database exists and is accessible.",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        
        /// <param name="holidayDate">The date of the holiday</param>

        private string CalculateCountdown(DateTime holidayDate)
        {
            try
            {
                // Calculate the difference between today and the holiday date
                var daysUntil = (holidayDate.Date - DateTime.Today).TotalDays;

                // Return appropriate message based on how far away the holiday is
                if (daysUntil < 0)
                    return "Passed";
                else if (daysUntil == 0)
                    return "Today";
                else if (daysUntil == 1)
                    return "Tomorrow";
                else if (daysUntil <= 7)
                    return $"In {daysUntil} days";
                else if (daysUntil <= 30)
                    return $"In {Math.Ceiling(daysUntil / 7)} weeks";
                else
                    return $"In {Math.Ceiling(daysUntil / 30)} months";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating countdown: {ex.Message}");
                return "Unknown";
            }
        }

        /// <summary>
        /// Shows or hides the Add Holiday form panel (Admin only)

        private void ShowAddHolidayForm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Verify user is admin
                if (_currentUser.Role != "Admin")
                {
                    MessageBox.Show(
                        "Only administrators can add holidays.",
                        "Access Denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Toggle the add form visibility
                if (AddHolidayPanel != null)
                {
                    if (AddHolidayPanel.Visibility == Visibility.Collapsed)
                    {
                        AddHolidayPanel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        AddHolidayPanel.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error toggling form: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        /// Handles the Add Holiday button click

        private void AddHoliday_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Verify user is admin
                if (_currentUser.Role != "Admin")
                {
                    MessageBox.Show(
                        "Only administrators can add holidays.",
                        "Access Denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Step 1: Validate holiday name
                var holidayName = HolidayNameTextBox?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(holidayName))
                {
                    MessageBox.Show(
                        "Please enter a holiday name.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    if (HolidayNameTextBox != null)
                    {
                        HolidayNameTextBox.Focus();
                    }
                    return;
                }

                // Step 2: Validate date is selected
                if (HolidayDatePicker == null || !HolidayDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show(
                        "Please select a date for the holiday.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    if (HolidayDatePicker != null)
                    {
                        HolidayDatePicker.Focus();
                    }
                    return;
                }

                // Step 3: Validate type is selected
                if (HolidayTypeComboBox == null || HolidayTypeComboBox.SelectedIndex < 0)
                {
                    MessageBox.Show(
                        "Please select a holiday type (Public or Company).",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    if (HolidayTypeComboBox != null)
                    {
                        HolidayTypeComboBox.Focus();
                    }
                    return;
                }

                using (var db = new AppDbContext())
                {
                    // Step 4: Check for duplicate holidays on the same date
                    var selectedDate = HolidayDatePicker.SelectedDate.Value;
                    var existingHoliday = db.Holidays
                        .FirstOrDefault(h => h.Date.Date == selectedDate.Date);

                    if (existingHoliday != null)
                    {
                        var result = MessageBox.Show(
                            $"A holiday already exists on {selectedDate:dd/MM/yyyy}:\n\n" +
                            $"'{existingHoliday.Name}'\n\n" +
                            "Do you want to add another holiday on the same date?",
                            "Duplicate Date",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );

                        if (result != MessageBoxResult.Yes)
                            return;
                    }

                    // Step 5: Create the new holiday object
                    var holiday = new Holiday
                    {
                        Name = holidayName,
                        Date = selectedDate,
                        Type = ((ComboBoxItem)HolidayTypeComboBox.SelectedItem).Content.ToString(),
                        Description = HolidayDescriptionTextBox?.Text?.Trim() ?? string.Empty,
                        IsRecurring = RecurringCheckBox?.IsChecked ?? false,
                        CreatedDate = DateTime.Now,
                        CreatedByUserId = _currentUser.Id
                    };

                    // Step 6: Add to database
                    db.Holidays.Add(holiday);
                    db.SaveChanges();

                    MessageBox.Show(
                        $"Holiday '{holidayName}' added successfully!",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Step 7: Clear the form
                    ClearAddHolidayForm();

                    // Step 8: Reload the holidays to show the new one
                    LoadHolidayData();

                    // Step 9: Hide the form panel
                    if (AddHolidayPanel != null)
                    {
                        AddHolidayPanel.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error adding holiday: {ex.Message}\n\nPlease try again or contact your system administrator.",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }


        /// Clears all fields in the Add Holiday form

        private void ClearAddHolidayForm()
        {
            try
            {
                if (HolidayNameTextBox != null)
                {
                    HolidayNameTextBox.Clear();
                }

                if (HolidayDatePicker != null)
                {
                    HolidayDatePicker.SelectedDate = null;
                }

                if (HolidayDescriptionTextBox != null)
                {
                    HolidayDescriptionTextBox.Clear();
                }

                if (RecurringCheckBox != null)
                {
                    RecurringCheckBox.IsChecked = false;
                }

                if (HolidayTypeComboBox != null)
                {
                    HolidayTypeComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing form: {ex.Message}");
            }
        }


        /// Handles the Delete button click on a holiday row

        private void DeleteHoliday_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Verify user is admin
                if (_currentUser.Role != "Admin")
                {
                    MessageBox.Show(
                        "Only administrators can delete holidays.",
                        "Access Denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Step 1: Get the holiday ID from the button's Tag property
                if (sender is Button button && button.Tag is int holidayId)
                {
                    using (var db = new AppDbContext())
                    {
                        // Step 2: Find the holiday in the database
                        var holiday = db.Holidays.Find(holidayId);

                        if (holiday == null)
                        {
                            MessageBox.Show(
                                "Holiday not found in the database. It may have already been deleted.",
                                "Not Found",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning
                            );
                            LoadHolidayData(); // Refresh to show current state
                            return;
                        }

                        // Step 3: Confirm deletion
                        var result = MessageBox.Show(
                            $"Are you sure you want to delete this holiday?\n\n" +
                            $"Name: {holiday.Name}\n" +
                            $"Date: {holiday.Date:dd/MM/yyyy}\n\n" +
                            "This action cannot be undone.",
                            "Confirm Delete",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning
                        );

                        if (result != MessageBoxResult.Yes)
                            return;

                        // Step 4: Delete the holiday
                        db.Holidays.Remove(holiday);
                        db.SaveChanges();

                        MessageBox.Show(
                            "Holiday deleted successfully.",
                            "Success",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );

                        // Step 5: Reload the holidays list
                        LoadHolidayData();
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Error: Unable to identify which holiday to delete.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error deleting holiday: {ex.Message}\n\nThe holiday may be referenced by other records.",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }


        private void ApplyForLeave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Try to get the dashboard window
                var dashboardWindow = Window.GetWindow(this) as DashboardWindow;

                if (dashboardWindow != null)
                {
                    // Use the dashboard's navigation method
                    dashboardWindow.NavigateToLeaveManagement();
                }
                else
                {
                    // Fallback: Try direct navigation
                    var leavePage = new LeavePage(_currentUser);
                    this.NavigationService?.Navigate(leavePage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error navigating to Leave Management: {ex.Message}\n\nPlease use the sidebar menu to navigate.",
                    "Navigation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string helpMessage = "Holidays Page Help\n\n" +
                    "Holiday Calendar Timeline:\n" +
                    "- View upcoming holidays in a visual timeline\n" +
                    "- See countdown to each holiday\n" +
                    "- Cards show holiday name, date, type, and days remaining\n\n" +
                    "All Holidays Table:\n" +
                    "- Complete list of all company holidays\n" +
                    "- Shows holiday name, date, type, countdown, and recurring status\n" +
                    "- Apply for Leave button navigates to leave request page\n";

                if (_currentUser.Role == "Admin")
                {
                    helpMessage += "\nAdmin Features:\n" +
                        "- Click 'Add Holiday' button to show the form\n" +
                        "- Fill in holiday details (name, date, type required)\n" +
                        "- Check 'Recurring' for annual holidays like birthdays\n" +
                        "- Click 'Add Holiday' in form to save\n" +
                        "- Click 'Delete' button in table to remove holidays\n" +
                        "- System will confirm before deleting\n" +
                        "- Duplicate date warnings help prevent errors";
                }

                MessageBox.Show(
                    helpMessage,
                    "Holidays Help",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing help: {ex.Message}");
            }
        }

       
        /// Handles the search box text changed event
        /// Filters the holidays grid based on the search text
       
        private void HolidaySearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (HolidaysGrid == null || _allHolidays == null)
                    return;

                var searchText = HolidaySearchTextBox?.Text?.ToLower() ?? string.Empty;

                // Start with all holidays or only future holidays for admins
                var filteredHolidays = _allHolidays.AsEnumerable();

                // Filter out passed holidays for admins
                if (_currentUser.Role == "Admin")
                {
                    filteredHolidays = filteredHolidays.Where(h => h.Date >= DateTime.Today);
                }

                // Apply search filter if search text is not empty
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    filteredHolidays = filteredHolidays.Where(h =>
                        h.Name.ToLower().Contains(searchText) ||
                        h.Type.ToLower().Contains(searchText) ||
                        h.Date.ToString("dd/MM/yyyy").Contains(searchText) ||
                        h.Description.ToLower().Contains(searchText)
                    );
                }

                // Update the grid
                HolidaysGrid.ItemsSource = filteredHolidays.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error filtering holidays: {ex.Message}");
            }
        }
    }


    public class HolidayViewModel
    {
        // All the standard Holiday properties
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRecurring { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedByUserId { get; set; }

        // Additional property for countdown display
        // This is calculated in the code-behind and not stored in the database
        public string DaysUntil { get; set; } = string.Empty;
    }
}