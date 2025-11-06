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
            }

            // Load all holidays from the database and display them
            LoadHolidayData();

            // Set the default selection for holiday type dropdown to "Public"
            HolidayTypeComboBox.SelectedIndex = 0;
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

                    // Bind the holidays to the DataGrid so they appear on screen
                    HolidaysGrid.ItemsSource = holidayViewModels;

                    // Filter to only show upcoming holidays in the timeline (next 12 months)
                    // This prevents the timeline from becoming cluttered with old holidays
                    var upcomingHolidays = holidayViewModels
                        .Where(h => h.Date >= DateTime.Today && h.Date <= DateTime.Today.AddMonths(12))
                        .Take(10) // Limit to 10 holidays to prevent overcrowding
                        .ToList();

                    // Bind the upcoming holidays to the timeline control
                    TimelineItemsControl.ItemsSource = upcomingHolidays;
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
        /// Calculates a human-readable countdown string from today to the holiday date
        /// Returns different messages based on whether the holiday is past, today, or future
        /// </summary>
        /// <param name="holidayDate">The date of the holiday</param>
        /// <returns>A string like "In 5 days", "Today!", or "Passed"</returns>
        private string CalculateCountdown(DateTime holidayDate)
        {
            // Get today's date at midnight for accurate day calculation
            var today = DateTime.Today;

            // Calculate the difference in days between today and the holiday
            var daysUntil = (holidayDate.Date - today).Days;

            // Return appropriate message based on the number of days
            if (daysUntil < 0)
            {
                // Holiday has already passed
                return "Passed";
            }
            else if (daysUntil == 0)
            {
                // Holiday is today
                return "Today!";
            }
            else if (daysUntil == 1)
            {
                // Holiday is tomorrow
                return "Tomorrow!";
            }
            else if (daysUntil <= 7)
            {
                // Holiday is within a week - show urgency
                return $"In {daysUntil} days";
            }
            else if (daysUntil <= 30)
            {
                // Holiday is within a month
                return $"In {daysUntil} days";
            }
            else if (daysUntil <= 365)
            {
                // Holiday is within a year - show in weeks for easier understanding
                var weeks = daysUntil / 7;
                return $"In {weeks} weeks";
            }
            else
            {
                // Holiday is more than a year away - show in years
                var years = daysUntil / 365;
                return $"In {years} year(s)";
            }
        }

        /// <summary>
        /// Handles the Add Holiday button click
        /// This method validates the input, creates a new holiday record, and saves it to the database
        /// </summary>
        private void AddHoliday_Click(object sender, RoutedEventArgs e)
        {
            // Step 1: Validate that the holiday name is not empty
            if (string.IsNullOrWhiteSpace(HolidayNameTextBox.Text))
            {
                MessageBox.Show(
                    "Please enter a holiday name.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return; // Stop here if validation fails
            }

            // Step 2: Validate that a date has been selected
            if (!HolidayDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show(
                    "Please select a date for the holiday.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return; // Stop here if validation fails
            }

            // Step 3: Validate that a holiday type has been selected
            if (HolidayTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show(
                    "Please select a holiday type.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return; // Stop here if validation fails
            }

            try
            {
                // Step 4: Create and save the new holiday to the database
                using (var db = new AppDbContext())
                {
                    // Create a new Holiday object with all the details from the form
                    var holiday = new Holiday
                    {
                        Name = HolidayNameTextBox.Text.Trim(),
                        Date = HolidayDatePicker.SelectedDate.Value,
                        Type = ((ComboBoxItem)HolidayTypeComboBox.SelectedItem)?.Content?.ToString() ?? "Public",
                        Description = HolidayDescriptionTextBox.Text.Trim(),
                        IsRecurring = RecurringCheckBox.IsChecked ?? false,
                        CreatedDate = DateTime.Now,
                        CreatedByUserId = _currentUser.Id
                    };

                    // Add the holiday to the database
                    db.Holidays.Add(holiday);

                    // Save changes to the database 
                    db.SaveChanges();
                }

                // Step 5: Show success message
                MessageBox.Show(
                    "Holiday added successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Step 6: Clear the form so the admin can add another holiday
                HolidayNameTextBox.Clear();
                HolidayDatePicker.SelectedDate = null;
                HolidayTypeComboBox.SelectedIndex = 0;
                RecurringCheckBox.IsChecked = false;
                HolidayDescriptionTextBox.Clear();

                // Step 7: Reload the holidays grid and timeline to show the newly added holiday
                LoadHolidayData();
            }
            catch (Exception ex)
            {
                // If something goes wrong like input or glitch, show an error message to the user to give feedback
                MessageBox.Show(
                    $"Error adding holiday: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
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