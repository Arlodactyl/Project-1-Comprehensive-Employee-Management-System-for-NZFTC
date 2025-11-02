using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// Interaction logic for HolidaysPage.xaml
    /// This page displays a list of company holidays.
    /// Administrators can add new holidays through the Add Holiday tab.
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
        /// Loads all holidays from the database and displays them in the grid
        /// This method connects to the database, retrieves holidays, and binds them to the UI
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

                    // Bind the holidays to the DataGrid so they appear on screen
                    HolidaysGrid.ItemsSource = holidays;
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

                // Step 7: Reload the holidays grid to show the newly added holiday
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
}