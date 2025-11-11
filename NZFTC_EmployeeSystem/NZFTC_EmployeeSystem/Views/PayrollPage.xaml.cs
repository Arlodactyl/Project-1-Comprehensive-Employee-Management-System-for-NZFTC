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

                    // Calculate the start date (first Monday on/after hire date)
                    DateTime startDate = GetNextMonday(employee.HireDate);

                    // Today
                    DateTime today = DateTime.Today;

                    // Get all existing payslip week endings for this employee
                    var existingWeekEndings = db.Payslips
                        .Where(p => p.EmployeeId == employee.Id)
                        .Select(p => p.PayPeriodEnd.Date)
                        .ToHashSet();

                    int generatedCount = 0;
                    List<Payslip> newPayslips = new List<Payslip>();

                    // Loop through each week from start date to current week
                    DateTime currentWeekStart = startDate;
                    while (currentWeekStart <= today)
                    {
                        // Week end (6 days after start for a 7-day week)
                        DateTime weekEnd = currentWeekStart.AddDays(6);

                        // Only generate if this week doesn't already have a payslip
                        if (!existingWeekEndings.Contains(weekEnd.Date))
                        {
                            // Calculate payslip amounts (assuming 40 hours per week)
                            decimal weeklyHours = 40m;
                            decimal hourlyRate = employee.Salary / 52m / 40m;  // Annual to hourly
                            decimal grossSalary = Math.Round(hourlyRate * weeklyHours, 2, MidpointRounding.AwayFromZero);
                            decimal taxDeduction = Math.Round(grossSalary * (employee.TaxRate / 100m), 2, MidpointRounding.AwayFromZero);
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

                        // Subtle notification
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
                // Silent fail - inform but don't block page load
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
            if (date.DayOfWeek == DayOfWeek.Monday)
                return date;

            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)date.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0) daysUntilMonday = 7;

            return date.AddDays(daysUntilMonday);
        }

        /// <summary>
        /// Gets the next Sunday on or after the given date
        /// </summary>
        private DateTime GetNextSunday(DateTime date)
        {
            if (date.DayOfWeek == DayOfWeek.Sunday)
                return date;

            int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)date.DayOfWeek + 7) % 7;
            if (daysUntilSunday == 0) daysUntilSunday = 7;

            return date.AddDays(daysUntilSunday);
        }

        /// <summary>
        /// Load all payslip data from database
        /// </summary>
        private void LoadPayslipData()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // Load ALL payslips with employee data for admin view
                    _allPayslips = db.Payslips
                        .Include(p => p.Employee)
                        .OrderByDescending(p => p.PayPeriodEnd)
                        .ToList();

                    // Load only current user's payslips for employee view
                    _myPayslips = db.Payslips
                        .Where(p => p.EmployeeId == _currentUser.EmployeeId)
                        .OrderByDescending(p => p.PayPeriodEnd)
                        .ToList();

                    // Bind to appropriate grids
                    AllPayslipsGrid.ItemsSource = _allPayslips;
                    MyPayslipsGrid.ItemsSource = _myPayslips;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading payslips: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Load employee list for payslip generation dropdown
        /// </summary>
        private void LoadEmployeeList()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var employees = db.Employees
                        .OrderBy(e => e.FullName)
                        .ToList();

                    EmployeeComboBox.ItemsSource = employees;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading employees: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Refresh payslips button click - reloads all data from database
        /// </summary>
        private void RefreshPayslips_Click(object sender, RoutedEventArgs e)
        {
            LoadPayslipData();
            PayslipSearchTextBox.Clear();
            MessageBox.Show("Payslips refreshed!", "Success");
        }

        /// <summary>
        /// Search functionality - filters payslips as user types
        /// </summary>
        private void PayslipSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = PayslipSearchTextBox.Text.ToLower().Trim();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                AllPayslipsGrid.ItemsSource = _allPayslips;
                return;
            }

            var filtered = _allPayslips
                .Where(p => p.Employee != null &&
                           p.Employee.FullName.ToLower().Contains(searchText))
                .ToList();

            AllPayslipsGrid.ItemsSource = filtered;
        }

        /// <summary>
        /// Generate payslip button - creates new payslip for selected employee
        /// </summary>
        private void GeneratePayslip_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (EmployeeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an employee", "Validation Error");
                return;
            }

            if (!WeekEndingDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select a week ending date", "Validation Error");
                return;
            }

            var employee = EmployeeComboBox.SelectedItem as Employee;
            var weekEnd = WeekEndingDatePicker.SelectedDate.Value.Date;

            // Calculate week start (6 days before week end)
            var weekStart = weekEnd.AddDays(-6);

            try
            {
                using (var db = new AppDbContext())
                {
                    // Check if payslip already exists for this period
                    var existing = db.Payslips
                        .FirstOrDefault(p => p.EmployeeId == employee.Id &&
                                             p.PayPeriodEnd.Date == weekEnd);

                    if (existing != null)
                    {
                        MessageBox.Show(
                            "A payslip already exists for this employee and week ending date.",
                            "Duplicate Payslip",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // Calculate payslip amounts
                    decimal weeklyHours = 40m;
                    decimal hourlyRate = employee.Salary / 52m / 40m;
                    decimal grossSalary = Math.Round(hourlyRate * weeklyHours, 2, MidpointRounding.AwayFromZero);
                    decimal taxDeduction = Math.Round(grossSalary * (employee.TaxRate / 100m), 2, MidpointRounding.AwayFromZero);
                    decimal netSalary = grossSalary - taxDeduction;

                    // Create new payslip
                    var payslip = new Payslip
                    {
                        EmployeeId = employee.Id,
                        PayPeriodStart = weekStart,
                        PayPeriodEnd = weekEnd,
                        GrossSalary = grossSalary,
                        TaxDeduction = taxDeduction,
                        NetSalary = netSalary,
                        GeneratedDate = DateTime.Now,
                        GeneratedByUserId = _currentUser.Id
                    };

                    db.Payslips.Add(payslip);
                    db.SaveChanges();

                    MessageBox.Show(
                        $"Payslip generated successfully!\n\n" +
                        $"Employee: {employee.FullName}\n" +
                        $"Period: {weekStart:dd/MM/yyyy} - {weekEnd:dd/MM/yyyy}\n" +
                        $"Net Pay: {netSalary:C}",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Refresh data
                    LoadPayslipData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating payslip: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Clear form button - resets all form fields
        /// </summary>
        private void ClearForm_Click(object sender, RoutedEventArgs e)
        {
            EmployeeComboBox.SelectedIndex = -1;
            WeekEndingDatePicker.SelectedDate = null;
        }

        /// <summary>
        /// Double-click handler for All Payslips grid - shows detailed payslip
        /// </summary>
        private void AllPayslipsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectedPayslip = AllPayslipsGrid.SelectedItem as Payslip;
            if (selectedPayslip != null)
            {
                ShowPayslipDetails(selectedPayslip);
            }
        }

        /// <summary>
        /// Double-click handler for My Payslips grid - shows detailed payslip
        /// </summary>
        private void MyPayslipsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectedPayslip = MyPayslipsGrid.SelectedItem as Payslip;
            if (selectedPayslip != null)
            {
                ShowPayslipDetails(selectedPayslip);
            }
        }

        /// <summary>
        /// Shows detailed payslip information in a popup
        /// </summary>
        private void ShowPayslipDetails(Payslip payslip)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // Get fresh employee data with department
                    var employee = db.Employees
                        .Include(e => e.Department)
                        .FirstOrDefault(e => e.Id == payslip.EmployeeId);

                    var generatedBy = db.Users.Find(payslip.GeneratedByUserId);

                    // Calculate hours and rate for display
                    decimal hours = 40m;
                    decimal hourlyRate = employee != null ? employee.Salary / 52m / 40m : 0m;

                    var details = $"PAYSLIP DETAILS\n\n" +
                                  $"Employee: {employee?.FullName ?? "Unknown"}\n" +
                                  $"Department: {employee?.Department?.Name ?? "N/A"}\n" +
                                  $"Employee ID: {payslip.EmployeeId}\n\n" +
                                  $"PAY PERIOD\n" +
                                  $"Start: {payslip.PayPeriodStart:dd/MM/yyyy}\n" +
                                  $"End: {payslip.PayPeriodEnd:dd/MM/yyyy}\n\n" +
                                  $"EARNINGS\n" +
                                  $"Hours Worked: {hours}\n" +
                                  $"Hourly Rate: {hourlyRate:C}\n" +
                                  $"Gross Pay: {payslip.GrossSalary:C}\n\n" +
                                  $"DEDUCTIONS\n" +
                                  $"Tax Rate: {employee?.TaxRate ?? 0}%\n" +
                                  $"Tax Deducted: {payslip.TaxDeduction:C}\n\n" +
                                  $"NET PAY: {payslip.NetSalary:C}\n\n" +
                                  $"Generated: {payslip.GeneratedDate:dd/MM/yyyy HH:mm:ss}\n" +
                                  $"Generated By: {generatedBy?.Username ?? "System"}";

                    MessageBox.Show(details, "Payslip Details", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading payslip details: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Export visible payslips to CSV file
        /// </summary>
        private void ExportPayslips_Click(object sender, RoutedEventArgs e)
        {
            // Get currently displayed payslips (respects search filter)
            var payslipsList = AllPayslipsGrid.ItemsSource as List<Payslip>;

            if (payslipsList == null || payslipsList.Count == 0)
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
                        writer.WriteLine("Payslip Export - NZFTC Employee System");
                        writer.WriteLine($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                        writer.WriteLine($"Generated By: {_currentUser.Username}");
                        writer.WriteLine("");
                        writer.WriteLine("Payslip ID,Employee ID,Employee Name,Department," +
                                         "Period Start,Period End,Hours,Hourly Rate,Gross Pay," +
                                         "Tax Rate,Tax Deduction,Net Pay," +
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
                                decimal hours = 40m;  // Standard week
                                decimal hourlyRate = employee != null ? employee.Salary / 52m / 40m : 0m;

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
        /// Show information dialog about payroll in NZ context (role-aware)
        /// </summary>
        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            string helpMessage;

            if (_currentUser.Role == "Admin")
            {
                helpMessage = @"Payroll Information & Help - ADMINISTRATOR

HOW TO USE THIS PAGE:

All Payslips Tab:
• View payslips for all employees in the organization
• Use the search box to filter by employee name
• Double-click any payslip row to view full details
• Click 'Refresh' to reload payslip data from the database
• Click 'Export Visible Payslips' to save current view to CSV

Generate Weekly Payslip Section:
1. Select an employee from the dropdown (searchable)
2. Choose the week ending date (typically a Sunday)
3. Click 'Generate' to create the payslip
4. System automatically calculates gross pay, tax, and net pay
5. Click 'Clear' to reset the form

Tips for Administrators:
• Payslips are automatically generated for employees from their hire date
• The system uses a standard 40-hour work week for calculations
• Week periods run Monday to Sunday
• Duplicate payslips for the same period are prevented
• Export filtered results by searching first, then exporting

WHAT IS PAYROLL?

Payroll is the process of calculating and paying employee wages, including deductions for tax and other obligations. Under New Zealand employment law, employers must:

Key Employer Obligations:
• Pay employees on the agreed date
• Deduct and pay PAYE tax to Inland Revenue (IRD)
• Provide accurate payslips showing all deductions
• Keep wage and time records for 6 years
• Pay at least minimum wage ($23.15/hour as of 2024)
• Calculate holiday pay correctly
• Deduct and pay ACC earner levies (1.53%)

Essential Terms:

Gross Pay: Total earnings before any deductions
PAYE (Pay As You Earn): Tax deducted from wages by employers and paid to IRD on behalf of employees
Net Pay: Take-home pay after all deductions (gross minus tax, ACC, KiwiSaver, etc.)
Tax Code examples:
• M: Main job
• S/SH: Secondary job or with student loan
• ST: Special rate from IRD

NZ TAX RATES (2024/2025):
• Up to $14,000: 10.5%
• $14,001 - $48,000: 17.5%
• $48,001 - $70,000: 30%
• $70,001 - $180,000: 33%
• Over $180,000: 39%

Note: ACC levy (1.53%) and student loan payments (12%) are additional

COMMON DEDUCTIONS:
Mandatory: PAYE Tax, ACC Earner Levy
Voluntary: KiwiSaver (3–10%), Student Loan (12% over threshold)

RECORD KEEPING (6 years minimum):
• Wage and time records
• Holiday and leave records
• Employment agreements
• Payslip copies
• Tax payment records

FOR MORE INFORMATION:
• Inland Revenue: www.ird.govt.nz or 0800 227 774
• Employment NZ: www.employment.govt.nz or 0800 209 020
• MBIE: www.mbie.govt.nz";
            }
            else
            {
                helpMessage = @"Payroll Information & Help - EMPLOYEE

HOW TO USE THIS PAGE:

My Payslips Tab:
• View all your payslips
• Payslips generated automatically each week
• Double-click any row to see full details
• Click 'Export My Payslips' to save records

Understanding Your Payslip:
• Period: Week you worked (Monday–Sunday)
• Gross: Total earnings before deductions
• Tax: PAYE income tax deducted
• Net: Your take-home pay

WHAT IS PAYROLL?

Payroll is how your employer calculates your wages and ensures you're paid correctly. Under New Zealand law, you have important rights:

Your Rights:
• Receive accurate payslips showing all details
• Be paid on time as per your employment agreement
• Have correct tax deducted based on your tax code
• Be paid at least minimum wage for all hours worked
• Receive holiday pay and leave entitlements
• Access your payroll records at any time
• Question or dispute any pay errors

What Your Payslip Must Show:
• Your name and pay period dates
• Gross earnings (before deductions)
• All deductions listed (tax, ACC, KiwiSaver, etc.)
• Net pay (what you actually receive)
• Year-to-date totals

UNDERSTANDING YOUR PAY:

Example Calculation:
Gross: $1,000 (40 h × $25/h)
Tax (17.5%): -$175
ACC (1.53%): -$15
KiwiSaver (3%): -$30
Net Pay: $780

NZ TAX RATES (2024/2025):
• Up to $14,000/year: 10.5%
• $14,001–$48,000: 17.5%
• $48,001–$70,000: 30%
• $70,001–$180,000: 33%
• Over $180,000: 39%

Tax Codes:
M (main job), S/SH (secondary or with student loan)
Update at www.ird.govt.nz or call 0800 227 774

COMMON DEDUCTIONS:
Must: PAYE Tax, ACC (1.53%)
Optional: KiwiSaver (3–10%), Student Loan (12% over threshold)

MINIMUM WAGE (Apr 2024): $23.15/hour

HOLIDAY PAY (summary):
• Annual Leave: 4 weeks/year after 12 months
• Sick Leave: 10 days/year after 6 months
• Public Holidays: 12 days/year (paid if you'd normally work)
• Bereavement Leave: up to 3 days

IF YOUR PAY IS WRONG:
1) Check your payslip  2) Talk to payroll/HR  3) Keep records
4) Contact Employment NZ if unresolved: 0800 209 020

KIWISAVER:
• Your choice: 3%, 4%, 6%, 8%, 10%
• Employer contributes min 3%
• Government contribution up to $521/year (conditions apply)

This system calculates weekly payslips using:
• Your annual salary
• Standard 40-hour week
• Your tax rate
• Weekly periods (Monday–Sunday)";
            }

            MessageBox.Show(
                helpMessage,
                "Payroll Information & Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
