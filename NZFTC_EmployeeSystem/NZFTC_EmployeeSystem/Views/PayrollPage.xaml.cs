using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// PayrollPage handles payslip management for employees and administrators
    /// </summary>
    public partial class PayrollPage : Page
    {
        private readonly User _currentUser;
        private List<Payslip> _allPayslips;  // Store all payslips for search filtering
        private List<Payslip> _myPayslips;   // Store user's payslips

        public PayrollPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            // Configure page based on user role
            if (_currentUser.Role != "Admin")
            {
                // Hide admin-only tab for regular employees
                AllPayslipsTab.Visibility = Visibility.Collapsed;
                // Start on My Payslips tab for employees
                PayrollTabControl.SelectedIndex = 0;
            }
            else
            {
                // Admin can see everything
                LoadEmployeeList();
            }

            // Load payslip data
            LoadPayslipData();
        }

        /// <summary>
        /// Load all payslip data from database
        /// </summary>
        private void LoadPayslipData()
        {
            using (var db = new AppDbContext())
            {
                // Always load user's own payslips
                _myPayslips = db.Payslips
                    .Where(p => p.EmployeeId == _currentUser.EmployeeId)
                    .OrderByDescending(p => p.PayPeriodEnd)
                    .ToList();
                MyPayslipsGrid.ItemsSource = _myPayslips;

                // Only load all payslips if user is admin
                if (_currentUser.Role == "Admin")
                {
                    _allPayslips = db.Payslips
                        .Include(p => p.Employee)
                        .OrderByDescending(p => p.PayPeriodEnd)
                        .ToList();
                    AllPayslipsGrid.ItemsSource = _allPayslips;
                }
            }
        }

        /// <summary>
        /// Load list of active employees for the dropdown
        /// </summary>
        private void LoadEmployeeList()
        {
            using (var db = new AppDbContext())
            {
                // Get all active employees
                var employees = db.Employees
                    .Where(e => e.IsActive)
                    .OrderBy(e => e.LastName)
                    .ThenBy(e => e.FirstName)
                    .ToList();

                EmployeeComboBox.ItemsSource = employees;
            }
        }

        /// <summary>
        /// Search payslips by employee name
        /// </summary>
        private void PayslipSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Only search if admin and has data
            if (_currentUser.Role != "Admin" || _allPayslips == null) return;

            string searchText = PayslipSearchTextBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Show all if search is empty
                AllPayslipsGrid.ItemsSource = _allPayslips;
            }
            else
            {
                // Filter by employee name or ID
                var filtered = _allPayslips.Where(p =>
                    p.Employee != null && (
                        p.Employee.FullName.ToLower().Contains(searchText) ||
                        p.Employee.FirstName.ToLower().Contains(searchText) ||
                        p.Employee.LastName.ToLower().Contains(searchText) ||
                        p.EmployeeId.ToString().Contains(searchText)
                    )
                ).ToList();

                AllPayslipsGrid.ItemsSource = filtered;
            }
        }

        /// <summary>
        /// Refresh all payslips from database
        /// </summary>
        private void RefreshPayslips_Click(object sender, RoutedEventArgs e)
        {
            // Clear search
            PayslipSearchTextBox.Clear();

            // Reload from database
            LoadPayslipData();

            MessageBox.Show("Payslips refreshed successfully!", "Refresh Complete",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Generate a new weekly payslip
        /// </summary>
        private void GeneratePayslip_Click(object sender, RoutedEventArgs e)
        {
            // Check if employee is selected
            if (EmployeeComboBox.SelectedItem is not Employee selectedEmployee)
            {
                MessageBox.Show("Please select an employee.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if week ending date is selected
            if (!WeekEndingDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select the week ending date.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime weekEnding = WeekEndingDatePicker.SelectedDate.Value;
            // Calculate week start (6 days before week end for a 7-day week)
            DateTime weekStart = weekEnding.AddDays(-6);

            using (var db = new AppDbContext())
            {
                // Get fresh employee data with tax rate
                var employee = db.Employees.Find(selectedEmployee.Id);
                if (employee == null)
                {
                    MessageBox.Show("Selected employee not found in database.", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Check if payslip already exists for this period
                var existingPayslip = db.Payslips.Any(p =>
                    p.EmployeeId == employee.Id &&
                    p.PayPeriodEnd == weekEnding);

                if (existingPayslip)
                {
                    MessageBox.Show($"A payslip already exists for {employee.FullName} for week ending {weekEnding:dd/MM/yyyy}.",
                                  "Duplicate Payslip", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Calculate payslip amounts
                // Assuming 40 hours per week as standard
                decimal weeklyHours = 40;
                decimal hourlyRate = employee.Salary / 52 / 40;  // Annual to hourly
                decimal grossSalary = hourlyRate * weeklyHours;
                decimal taxDeduction = grossSalary * (employee.TaxRate / 100m);
                decimal netSalary = grossSalary - taxDeduction;

                // Create new payslip
                var payslip = new Payslip
                {
                    EmployeeId = employee.Id,
                    PayPeriodStart = weekStart,
                    PayPeriodEnd = weekEnding,
                    GrossSalary = grossSalary,
                    TaxDeduction = taxDeduction,
                    NetSalary = netSalary,
                    GeneratedDate = DateTime.Now,
                    GeneratedByUserId = _currentUser.Id
                };

                // Save to database
                db.Payslips.Add(payslip);
                db.SaveChanges();

                // Show success message with details
                MessageBox.Show($"Payslip generated successfully!\n\n" +
                               $"Employee: {employee.FullName}\n" +
                               $"Period: {weekStart:dd/MM/yyyy} to {weekEnding:dd/MM/yyyy}\n" +
                               $"Hours: {weeklyHours}\n" +
                               $"Hourly Rate: {hourlyRate:C}\n" +
                               $"Gross: {grossSalary:C}\n" +
                               $"Tax ({employee.TaxRate}%): {taxDeduction:C}\n" +
                               $"Net: {netSalary:C}",
                               "Payslip Generated", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Clear form and refresh
            ClearForm_Click(sender, e);
            LoadPayslipData();
        }

        /// <summary>
        /// Clear the payslip generation form
        /// </summary>
        private void ClearForm_Click(object sender, RoutedEventArgs e)
        {
            EmployeeComboBox.SelectedIndex = -1;
            WeekEndingDatePicker.SelectedDate = null;
        }

        /// <summary>
        /// Export all payslips to CSV file (Admin only)
        /// </summary>
        private void ExportPayslips_Click(object sender, RoutedEventArgs e)
        {
            // Check if user is admin
            if (_currentUser.Role != "Admin")
            {
                MessageBox.Show("Only administrators can export all payslips.", "Access Denied",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if there's data to export
            if (_allPayslips == null || !_allPayslips.Any())
            {
                MessageBox.Show("No payslips to export.", "No Data",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Create save file dialog
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                FilterIndex = 1,
                FileName = $"Payslips_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                DefaultExt = "csv",
                AddExtension = true,
                Title = "Export Payslips"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(saveDialog.FileName, false, Encoding.UTF8))
                    {
                        // Write CSV header
                        writer.WriteLine("Payslip ID,Employee ID,Employee Name,Department,Week Start,Week End," +
                                       "Hours,Hourly Rate,Gross Salary,Tax Rate,Tax Deduction,Net Salary," +
                                       "Generated Date,Generated By");

                        // Write data rows
                        foreach (var payslip in _allPayslips)
                        {
                            using (var db = new AppDbContext())
                            {
                                // Get fresh employee data for department info
                                var employee = db.Employees.Include(e => e.Department).FirstOrDefault(e => e.Id == payslip.EmployeeId);
                                var generatedBy = db.Users.Find(payslip.GeneratedByUserId);

                                // Calculate hours and rate for display
                                decimal hours = 40;  // Standard week
                                decimal hourlyRate = employee != null ? employee.Salary / 52 / 40 : 0;

                                writer.WriteLine($"{payslip.Id}," +
                                               $"{payslip.EmployeeId}," +
                                               $"\"{payslip.Employee?.FullName ?? "Unknown"}\"," +
                                               $"\"{employee?.Department?.Name ?? "N/A"}\"," +
                                               $"{payslip.PayPeriodStart:dd/MM/yyyy}," +
                                               $"{payslip.PayPeriodEnd:dd/MM/yyyy}," +
                                               $"{hours}," +
                                               $"{hourlyRate:F2}," +
                                               $"{payslip.GrossSalary:F2}," +
                                               $"{employee?.TaxRate ?? 0}," +
                                               $"{payslip.TaxDeduction:F2}," +
                                               $"{payslip.NetSalary:F2}," +
                                               $"{payslip.GeneratedDate:dd/MM/yyyy HH:mm:ss}," +
                                               $"\"{generatedBy?.Username ?? "System"}\"");
                            }
                        }
                    }

                    MessageBox.Show($"Payslips exported successfully!\n\nFile saved to:\n{saveDialog.FileName}",
                                  "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Open the file location in explorer
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{saveDialog.FileName}\"");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting payslips:\n{ex.Message}", "Export Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Export user's own payslips to CSV file
        /// </summary>
        private void ExportMyPayslips_Click(object sender, RoutedEventArgs e)
        {
            // Check if there's data to export
            if (_myPayslips == null || !_myPayslips.Any())
            {
                MessageBox.Show("No payslips to export.", "No Data",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Create save file dialog
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FilterIndex = 1,
                FileName = $"My_Payslips_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                DefaultExt = "csv",
                AddExtension = true,
                Title = "Export My Payslips"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(saveDialog.FileName, false, Encoding.UTF8))
                    {
                        // Write CSV header
                        writer.WriteLine("Period Start,Period End,Gross Salary,Tax Deduction,Net Salary,Generated Date");

                        // Write data rows
                        foreach (var payslip in _myPayslips)
                        {
                            writer.WriteLine($"{payslip.PayPeriodStart:dd/MM/yyyy}," +
                                           $"{payslip.PayPeriodEnd:dd/MM/yyyy}," +
                                           $"{payslip.GrossSalary:F2}," +
                                           $"{payslip.TaxDeduction:F2}," +
                                           $"{payslip.NetSalary:F2}," +
                                           $"{payslip.GeneratedDate:dd/MM/yyyy HH:mm:ss}");
                        }
                    }

                    MessageBox.Show($"Your payslips exported successfully!\n\nFile saved to:\n{saveDialog.FileName}",
                                  "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Open the file location in explorer
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{saveDialog.FileName}\"");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting payslips:\n{ex.Message}", "Export Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}