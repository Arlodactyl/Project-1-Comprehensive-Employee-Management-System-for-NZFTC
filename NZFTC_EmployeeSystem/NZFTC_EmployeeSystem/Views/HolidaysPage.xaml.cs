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
    /// Displays a list of holidays and allows admins to add new ones.
    /// </summary>
    public partial class HolidaysPage : Page
    {
        private readonly User _currentUser;

        public HolidaysPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            // Show add form if admin
            if (_currentUser.Role == "Admin")
            {
                AdminAddPanel.Visibility = Visibility.Visible;
            }
            LoadHolidayData();
            // Default selection for holiday type
            HolidayTypeComboBox.SelectedIndex = 0;
        }

        // Loads holidays from the database and binds to the grid
        private void LoadHolidayData()
        {
            using (var db = new AppDbContext())
            {
                var holidays = db.Holidays
                    .OrderBy(h => h.Date)
                    .ToList();
                HolidaysGrid.ItemsSource = holidays;
            }
        }

        // Handles the AddHoliday button click for admins
        private void AddHoliday_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(HolidayNameTextBox.Text))
            {
                MessageBox.Show("Please enter a holiday name", "Validation Error");
                return;
            }
            if (!HolidayDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select a date", "Validation Error");
                return;
            }

            using (var db = new AppDbContext())
            {
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
                db.Holidays.Add(holiday);
                db.SaveChanges();
            }

            MessageBox.Show("Holiday added successfully!", "Success");

            // Reset form
            HolidayNameTextBox.Clear();
            HolidayDatePicker.SelectedDate = null;
            HolidayTypeComboBox.SelectedIndex = 0;
            RecurringCheckBox.IsChecked = false;
            HolidayDescriptionTextBox.Clear();

            LoadHolidayData();
        }
    }
}
