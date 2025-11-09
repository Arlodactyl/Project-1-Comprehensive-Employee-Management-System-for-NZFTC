using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// PayrollPage handles payslip management for employees and administrators
    /// ENHANCED: Now auto-generates missing weekly payslips for employees from hire date to current week
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

            // Load payslip data first
            LoadPayslipData();

            // Configure page based on user role
            if (_currentUser.Role != "Admin")
            {
                // Hide admin-only tab for regular employees
                AllPayslipsTab.Visibility = Visibility.Collapsed;
                // Start on My Payslips tab for employees (which is now index 1, but will be 0 after hiding All Payslips)
                PayrollTabControl.SelectedItem = MyPayslipsTab;

                // ENHANCEMENT: Auto-generate missing payslips for this employee
                AutoGenerateMissingPayslips();
            }
            else
            {
                // Admin can see everything
                LoadEmployeeList();
                // Admin starts on All Payslips tab
                PayrollTabControl.SelectedItem = AllPayslipsTab;
            }
        }

        /// <summary>
        /// Auto-generates missing weekly payslips for the current employee
        /// from their hire date up to the current week
        /// </summary>
        private void AutoGenerateMissingPayslips()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // Get the current employee's details
                    var employee = db.Employees.Find(_currentUser.EmployeeId);
                    if (employee == null) return;

                    // Calculate the start date (first Monday after hire date)
                    DateTime startDate = GetNextMonday(employee.HireDate);

                    // Calculate the current week's ending (next Sunday)
                    DateTime today = DateTime.Today;
                    DateTime currentWeekEnd = GetNextSunday(today);

                    // Get all existing payslip week endings for this employee
                    var existingWeekEndings = db.Payslips
                        .Where(p => p.EmployeeId == employee.Id)
                        .Select(p => p.PayPeriodEnd)
                        .ToHashSet();

                    int generatedCount = 0;
                    List<Payslip> newPayslips = new List<Payslip>();

                    // Loop through each week from start date to current week
                    DateTime currentWeekStart = startDate;
                    while (currentWeekStart <= today)
                    {
                        // Calculate week end (6 days after start for a 7-day week)
                        DateTime weekEnd = currentWeekStart.AddDays(6);

                        // Only generate if this week doesn't already have a payslip
                        if (!existingWeekEndings.Contains(weekEnd))
                        {
                            // Calculate payslip amounts (assuming 40 hours per week)
                            decimal weeklyHours = 40;
                            decimal hourlyRate = employee.Salary / 52 / 40;  // Annual to hourly
                            decimal grossSalary = hourlyRate * weeklyHours;
                            decimal taxDeduction = grossSalary * (employee.TaxRate / 100m);
                            decimal netSalary = grossSalary - taxDeduction;

                            // Create new payslip
                            var payslip = new Payslip
                            {
                                EmployeeId = employee.Id,
                                PayPeriodStart = currentWeekStart,
                                PayPeriodEnd = weekEnd,
                                GrossSalary = grossSalary,
                                TaxDeduction = taxDeduction,
                                NetSalary = netSalary,
                                GeneratedDate = DateTime.Now,
                                GeneratedByUserId = _currentUser.Id
                            };

                            newPayslips.Add(payslip);
                            generatedCount++;
                        }

                        // Move to next week (add 7 days)
                        currentWeekStart = currentWeekStart.AddDays(7);
                    }

                    // Save all new payslips to database if any were generated
                    if (newPayslips.Any())
                    {
                        db.Payslips.AddRange(newPayslips);
                        db.SaveChanges();

                        // Show a subtle notification
                        if (generatedCount > 0)
                        {
                            MessageBox.Show(
                                $"Welcome! Your payroll history has been updated.\n\n" +
                                $"Generated {generatedCount} payslip(s) covering weeks from {startDate:dd/MM/yyyy} to present.",
                                "Payslips Generated",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent fail - don't block page load if auto-generation fails
                MessageBox.Show(
                    $"Note: Unable to auto-generate payslips.\n{ex.Message}",
                    "Payroll Notice",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        /// <summary>
        /// Gets the next Monday on or after the given date
        /// </summary>
        private DateTime GetNextMonday(DateTime date)
        {
            // If date is already Monday, return it
            if (date.DayOfWeek == DayOfWeek.Monday)
                return date;

            // Calculate days until next Monday
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)date.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0) daysUntilMonday = 7;

            return date.AddDays(daysUntilMonday);
        }

        /// <summary>
        /// Gets the next Sunday on or after the given date
        /// </summary>
        private DateTime GetNextSunday(DateTime date)
        {
            // If date is already Sunday, return it
            if (date.DayOfWeek == DayOfWeek.Sunday)
                return date;

            // Calculate days until next Sunday
            int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)date.DayOfWeek + 7) % 7;
            if (daysUntilSunday == 0) daysUntilSunday = 7;

            return date.AddDays(daysUntilSunday);
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
        /// Load list of active employees for the dropdown - sorted by ID
        /// </summary>
        private void LoadEmployeeList()
        {
            using (var db = new AppDbContext())
            {
                // Get all active employees sorted by ID
                var employees = db.Employees
                    .Where(e => e.IsActive)
                    .OrderBy(e => e.Id)  // Sort by ID
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

                MessageBox.Show(
                    $"Payslip generated successfully!\n\n" +
                    $"Employee: {employee.FullName}\n" +
                    $"Period: {weekStart:dd/MM/yyyy} - {weekEnding:dd/MM/yyyy}\n" +
                    $"Net Pay: {netSalary:C}",
                    "Payslip Created",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Clear form
                ClearForm_Click(sender, e);

                // Reload data
                LoadPayslipData();
            }
        }

        /// <summary>
        /// Clear the payslip generation form
        /// </summary>
        private void ClearForm_Click(object sender, RoutedEventArgs e)
        {
            EmployeeComboBox.SelectedItem = null;
            WeekEndingDatePicker.SelectedDate = null;
        }

        /// <summary>
        /// Handle double-click on All Payslips grid to view details
        /// </summary>
        private void AllPayslipsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AllPayslipsGrid.SelectedItem is Payslip payslip)
            {
                ShowPayslipDetails(payslip);
            }
        }

        /// <summary>
        /// Handle double-click on My Payslips grid to view details
        /// </summary>
        private void MyPayslipsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MyPayslipsGrid.SelectedItem is Payslip payslip)
            {
                ShowPayslipDetails(payslip);
            }
        }

        /// <summary>
        /// Display detailed payslip information in a popup window
        /// </summary>
        private void ShowPayslipDetails(Payslip payslip)
        {
            using (var db = new AppDbContext())
            {
                // Get employee details
                var employee = db.Employees
                    .Include(e => e.Department)
                    .FirstOrDefault(e => e.Id == payslip.EmployeeId);

                var generatedBy = db.Users.Find(payslip.GeneratedByUserId);

                if (employee == null)
                {
                    MessageBox.Show("Employee data not found.", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Calculate additional details
                decimal weeklyHours = 40;
                decimal hourlyRate = employee.Salary / 52 / 40;

                // Build detailed message
                string details = $@"
═══════════════════════════════════════
PAYSLIP DETAILS
═══════════════════════════════════════

EMPLOYEE INFORMATION
Employee: {employee.FullName}
Employee ID: {employee.Id}
Department: {employee.Department?.Name ?? "N/A"}
Position: {employee.JobTitle}

PAY PERIOD
Start Date: {payslip.PayPeriodStart:dddd, dd MMMM yyyy}
End Date: {payslip.PayPeriodEnd:dddd, dd MMMM yyyy}
Duration: 7 days (1 week)

HOURS & RATE
Hours Worked: {weeklyHours} hours
Hourly Rate: {hourlyRate:C2}/hour
Annual Salary: {employee.Salary:C0}

EARNINGS & DEDUCTIONS
Gross Salary: {payslip.GrossSalary:C2}
Tax Rate: {employee.TaxRate}%
Tax Deducted: {payslip.TaxDeduction:C2}
────────────────────────────────────
NET PAY: {payslip.NetSalary:C2}
════════════════════════════════════

PAYSLIP INFORMATION
Payslip ID: #{payslip.Id}
Generated: {payslip.GeneratedDate:dd/MM/yyyy HH:mm:ss}
Generated By: {generatedBy?.Username ?? "System"}

═══════════════════════════════════════";

                MessageBox.Show(
                    details,
                    "Payslip Details",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        /// <summary>
        /// Export visible payslips to CSV file
        /// </summary>
        private void ExportPayslips_Click(object sender, RoutedEventArgs e)
        {
            // Get the currently visible payslips from the grid
            var payslipsList = AllPayslipsGrid.ItemsSource as List<Payslip>;

            if (payslipsList == null || !payslipsList.Any())
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
                        foreach (var payslip in payslipsList)
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

                    MessageBox.Show($"Payslips exported successfully!\n\nRecords exported: {payslipsList.Count}\n\nFile saved to:\n{saveDialog.FileName}",
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

                    MessageBox.Show($"Your payslips exported successfully!\n\nRecords exported: {_myPayslips.Count}\n\nFile saved to:\n{saveDialog.FileName}",
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
        /// Show information dialog about payroll in NZ context
        /// </summary>
        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            var infoMessage = @"Payroll Information & Help

HOW TO USE THIS PAGE:

For Employees:
• View your payslips in the 'My Payslips' tab
• Your payslips are automatically generated weekly from your hire date
• Double-click any payslip to view full details
• Export your payslips to CSV for your records

For Administrators:
• View all employee payslips in the 'All Payslips' tab
• Search for specific employees using the search box
• Double-click any payslip to view full details
• Generate new weekly payslips for employees
• Export filtered results to CSV

═══════════════════════════════════════

PAYROLL IN NEW ZEALAND

What is Payroll?
Payroll is the process of calculating and paying employee wages, including deductions for tax and other obligations.

Key NZ Payroll Terms:

• Gross Pay: Total earnings before deductions
• PAYE (Pay As You Earn): Tax deducted from wages by employers
• Net Pay: Take-home pay after all deductions
• Pay Period: Frequency of payment (weekly, fortnightly, monthly)
• Tax Code: Determines how much tax is deducted

Employer Obligations (NZ):
• Pay at least minimum wage ($23.15/hour as of 2024)
• Deduct and pay PAYE to IRD
• Provide accurate payslips showing all deductions
• Keep wage and time records for 6 years
• Pay employees on time as per employment agreement
• Calculate and pay holiday pay and leave entitlements

Employee Rights:
• Receive accurate payslips for each pay period
• Be paid at least minimum wage
• Have correct tax deducted
• Receive holiday pay and leave entitlements
• Access your payroll records

Common Deductions:
• PAYE Tax (10.5% - 39% depending on income)
• ACC Earner Levy (currently 1.53%)
• KiwiSaver (3%, 4%, 6%, 8%, or 10%)
• Student Loan repayments (if applicable)

Tax Rates (2024/2025):
• Up to $14,000: 10.5%
• $14,001 - $48,000: 17.5%
• $48,001 - $70,000: 30%
• $70,001 - $180,000: 33%
• Over $180,000: 39%

For More Information:
• Inland Revenue (IRD): www.ird.govt.nz
• Employment New Zealand: www.employment.govt.nz
• Ministry of Business: www.mbie.govt.nz

Questions? Contact your HR department or payroll administrator.";

            MessageBox.Show(
                infoMessage,
                "Payroll Information & Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}