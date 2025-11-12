using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// Interaction logic for HolidaysPage.xaml
    /// This page displays a list of company holidays with countdown timers.
    /// Administrators can add new holidays through the Add Holiday tab.
    /// Features a visual timeline map showing upcoming holidays.
    /// </summary>
    public partial class HolidaysPage : Page
    {
        // Store the current logged-in user so we know who's viewing the page
        private readonly User _currentUser;

        /// <summary>
        /// Constructor - runs when the page is first created
        /// </summary>
        public HolidaysPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            // Only show the "Add Holiday" tab if the user is an admin
            if (_currentUser.Role != "Admin")
            {
                AdminHolidayTab.Visibility = Visibility.Collapsed;
                // Also hide the delete actions column for non-admins
                if (AdminActionsColumn != null)
                {
                    AdminActionsColumn.Visibility = Visibility.Collapsed;
                }
            }

            // Set date picker to only allow today or future dates
            if (HolidayDatePicker != null)
            {
                HolidayDatePicker.DisplayDateStart = DateTime.Today;
                // Set maximum date to 10 years in future
                HolidayDatePicker.DisplayDateEnd = DateTime.Today.AddYears(10);
            }

            // Load all holidays from the database and display them
            LoadHolidayData();

            // Set the default selection for holiday type dropdown to "Public"
            if (HolidayTypeComboBox != null)
            {
                HolidayTypeComboBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Loads all holidays from the database and displays them in both the grid and timeline
        /// This method connects to the database, retrieves holidays, calculates countdowns, and binds to UI
        /// </summary>
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
                    var holidayViewModels = holidays.Select(h => new HolidayViewModel
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

                    // Show all holidays in the grid
                    HolidaysGrid.ItemsSource = holidayViewModels;

                    // Filter to only show upcoming holidays in the timeline (next 12 months)
                    // This prevents the timeline from becoming cluttered with old holidays
                    var upcomingHolidays = holidayViewModels
                        .Where(h => h.Date >= DateTime.Today && h.Date <= DateTime.Today.AddMonths(12))
                        .Take(10) // Limit to 10 holidays to prevent overcrowding
                        .ToList();

                    // Bind the upcoming holidays to the timeline control
                    HolidayTimeline.ItemsSource = upcomingHolidays;
                }
            }
            catch (Exception ex)
            {
                // If something goes wrong, show an error message for feedback purposes
                MessageBox.Show(
                    $"Error loading holidays: {ex.Message}\n\nPlease ensure the database exists.",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Calculates and formats a user-friendly countdown message for a holiday
        /// </summary>
        /// <param name="holidayDate">The date of the holiday</param>
        /// <returns>A formatted string like "In 5 days", "Tomorrow", "Today", or "Passed"</returns>
        private string CalculateCountdown(DateTime holidayDate)
        {
            // Calculate the difference between today and the holiday date
            var daysUntil = (holidayDate.Date - DateTime.Today).TotalDays;

            // Return appropriate message based on how far away the holiday is
            if (daysUntil < 0)
                return "Passed";
            else if (daysUntil == 0)
                return "Today!";
            else if (daysUntil == 1)
                return "Tomorrow";
            else if (daysUntil <= 7)
                return $"In {daysUntil} days";
            else if (daysUntil <= 30)
                return $"In {Math.Ceiling(daysUntil / 7)} weeks";
            else
                return $"In {Math.Ceiling(daysUntil / 30)} months";
        }

        /// <summary>
        /// Handles the Add Holiday button click
        /// This validates the input, creates a new Holiday record, and saves it to the database
        /// </summary>
        private void AddHoliday_Click(object sender, RoutedEventArgs e)
        {
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
                return;
            }

            // Step 2: Validate date is selected
            if (!HolidayDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show(
                    "Please select a date for the holiday.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Step 3: Validate type is selected
            if (HolidayTypeComboBox.SelectedIndex < 0)
            {
                MessageBox.Show(
                    "Please select a holiday type (Public or Company).",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            try
            {
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
                }

                MessageBox.Show(
                    $"Holiday '{holidayName}' added successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Step 7: Clear the form
                HolidayNameTextBox.Clear();
                HolidayDatePicker.SelectedDate = null;
                HolidayDescriptionTextBox.Clear();
                RecurringCheckBox.IsChecked = false;
                HolidayTypeComboBox.SelectedIndex = 0;

                // Step 8: Reload the holidays to show the new one
                LoadHolidayData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error adding holiday: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Handles the Delete button click on a holiday row
        /// </summary>
        private void DeleteHoliday_Click(object sender, RoutedEventArgs e)
        {
            // Step 1: Get the holiday ID from the button's Tag property
            if (sender is Button button && button.Tag is int holidayId)
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        // Step 2: Find the holiday in the database
                        var holiday = db.Holidays.Find(holidayId);

                        if (holiday == null)
                        {
                            MessageBox.Show(
                                "Holiday not found in the database.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );
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
                    }

                    MessageBox.Show(
                        "Holiday deleted successfully.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Step 5: Reload the holidays list
                    LoadHolidayData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error deleting holiday: {ex.Message}",
                        "Database Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
        }

        /// <summary>
        /// Navigate to Leave Management page
        /// </summary>
        private void ApplyForLeave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create the Leave page and navigate to it
                var leavePage = new LeavePage(_currentUser);
                this.NavigationService?.Navigate(leavePage);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error navigating to Leave Management: {ex.Message}",
                    "Navigation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Shows help information for using the holidays page
        /// </summary>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpMessage = "Holidays Page Help\n\n" +
                "View Holidays Tab:\n" +
                "- See all company holidays with countdown timers\n" +
                "- Click 'Apply for Leave' to request time off\n" +
                "- Timeline shows upcoming holidays visually\n";

            if (_currentUser.Role == "Admin")
            {
                helpMessage += "\nAdd Holiday Tab (Admin Only):\n" +
                    "- Add new company or public holidays\n" +
                    "- Fill in holiday name, date, type, and description\n" +
                    "- Check 'Recurring' for annual holidays\n" +
                    "- Click 'Add Holiday' to save\n\n" +
                    "Delete Holidays:\n" +
                    "- Click the 'Delete' button next to any holiday to remove it\n" +
                    "- You will be asked to confirm before deletion";
            }

            MessageBox.Show(
                helpMessage,
                "Holidays Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }

    /// <summary>
    /// View Model class for displaying holidays with calculated countdown
    /// This class extends the Holiday model with a DaysUntil property for display purposes
    /// We use a separate view model so we don't modify the database model
    /// </summary>
    public class HolidayViewModel
    {
        // All the standard Holiday properties
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public bool IsRecurring { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedByUserId { get; set; }

        // Additional property for countdown display
        // This is calculated in the code-behind and not stored in the database
        public string DaysUntil { get; set; }
    }
}