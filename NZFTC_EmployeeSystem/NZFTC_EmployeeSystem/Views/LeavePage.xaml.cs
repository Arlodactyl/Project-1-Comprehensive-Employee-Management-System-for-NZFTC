using Microsoft.EntityFrameworkCore;
using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// LeavePage - Handles leave management for employees and admins
    /// </summary>
    public partial class LeavePage : Page
    {
        private readonly User _currentUser;

        public LeavePage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            // Hide admin tab if not admin
            if (_currentUser.Role != "Admin")
            {
                AdminLeaveTab.Visibility = Visibility.Collapsed;
            }

            LoadLeaveData();
        }

        // Load leave requests from database
        private void LoadLeaveData()
        {
            using (var db = new AppDbContext())
            {
                // Load employee's own leave requests
                var myLeave = db.LeaveRequests
                    .Where(l => l.EmployeeId == _currentUser.EmployeeId)
                    .OrderByDescending(l => l.RequestDate)
                    .ToList();

                MyLeaveGrid.ItemsSource = myLeave;

                // Load all leave requests if admin
                if (_currentUser.Role == "Admin")
                {
                    var allLeave = db.LeaveRequests
                        .Include(l => l.Employee)
                        .OrderByDescending(l => l.RequestDate)
                        .ToList();

                    AllLeaveGrid.ItemsSource = allLeave;
                }
            }
        }

        // Submit leave request button
        private void SubmitLeave_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (LeaveTypeCombo.SelectedItem == null)
            {
                MessageBox.Show("Please select leave type", "Validation Error");
                return;
            }

            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select start and end dates", "Validation Error");
                return;
            }

            if (string.IsNullOrWhiteSpace(ReasonTextBox.Text))
            {
                MessageBox.Show("Please enter a reason", "Validation Error");
                return;
            }

            // Calculate days
            var startDate = StartDatePicker.SelectedDate.Value;
            var endDate = EndDatePicker.SelectedDate.Value;
            var days = (int)(endDate - startDate).TotalDays + 1;

            DateOnly startDateOnly = DateOnly.FromDateTime(startDate);
            DateOnly endDateOnly = DateOnly.FromDateTime(endDate);
            DateOnly currentDate = DateOnly.FromDateTime(DateTime.Now);

            if (startDateOnly < currentDate) // Making sure the user cant select a starting date beyond the current date
            {
                MessageBox.Show("Please enter the starting date from : " + currentDate);
                return;
            }

            if (endDateOnly < currentDate)
            {
                MessageBox.Show("Please enter the ending date from : " + currentDate);// Making sure the user cant select a ending date beyond the current date
                return;
            }


            if (days <= 0)
            {
                MessageBox.Show("End date must be after start date", "Validation Error");
                return;
            }

            // Create leave request
            using (var db = new AppDbContext())
            {
                var leaveRequest = new LeaveRequest
                {
                    EmployeeId = _currentUser.EmployeeId,
                    LeaveType = ((ComboBoxItem)LeaveTypeCombo.SelectedItem).Content.ToString(),
                    StartDate = startDate,
                    EndDate = endDate,
                    DaysRequested = days,
                    Reason = ReasonTextBox.Text,
                    Status = "Pending",
                    RequestDate = DateTime.Now
                };

                db.LeaveRequests.Add(leaveRequest);
                db.SaveChanges();
            }

            MessageBox.Show("Leave request submitted successfully!", "Success");

            // Clear form
            LeaveTypeCombo.SelectedIndex = -1;
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;
            ReasonTextBox.Clear();

            // Reload data
            LoadLeaveData();
        }

        // Approve leave button (Admin only)
        private void ApproveLeave_Click(object sender, RoutedEventArgs e)
        {
            var selectedLeave = AllLeaveGrid.SelectedItem as LeaveRequest;
            if (selectedLeave == null)
            {
                MessageBox.Show("Please select a leave request", "No Selection");
                return;
            }

            using (var db = new AppDbContext())
            {
                var leave = db.LeaveRequests.Find(selectedLeave.Id);
                if (leave != null)
                {
                    leave.Status = "Approved";
                    leave.ApprovedByUserId = _currentUser.Id;
                    leave.ApprovedDate = DateTime.Now;

                    // Update employee's leave balance
                    var employee = db.Employees.Find(leave.EmployeeId);
                    if (employee != null)
                    {
                        if (leave.LeaveType == "Annual Leave")
                            employee.AnnualLeaveBalance -= leave.DaysRequested;
                        else if (leave.LeaveType == "Sick Leave")
                            employee.SickLeaveBalance -= leave.DaysRequested;
                    }

                    db.SaveChanges();
                }
            }

            MessageBox.Show("Leave request approved!", "Success");
            LoadLeaveData();
        }

        // Reject leave button (Admin only)
        private void RejectLeave_Click(object sender, RoutedEventArgs e)
        {
            var selectedLeave = AllLeaveGrid.SelectedItem as LeaveRequest;
            if (selectedLeave == null)
            {
                MessageBox.Show("Please select a leave request", "No Selection");
                return;
            }

            using (var db = new AppDbContext())
            {
                var leave = db.LeaveRequests.Find(selectedLeave.Id);
                if (leave != null)
                {
                    leave.Status = "Rejected";
                    leave.ApprovedByUserId = _currentUser.Id;
                    leave.ApprovedDate = DateTime.Now;
                    db.SaveChanges();
                }
            }

            MessageBox.Show("Leave request rejected", "Success");
            LoadLeaveData();
        }

        /// <summary>
        /// Shows role-specific help for Leave Management
        /// </summary>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpMessage;

            if (_currentUser.Role == "Admin")
            {
                helpMessage = "Leave Management Help - ADMIN\n\n" +
                    "MY LEAVE TAB:\n" +
                    "- View and submit your own leave requests\n" +
                    "- Same functionality as regular employees\n\n" +
                    "MANAGE ALL LEAVE TAB:\n" +
                    "- View all employee leave requests\n" +
                    "- See pending, approved, and rejected requests\n\n" +
                    "Approving Leave:\n" +
                    "1. Select a pending leave request from the grid\n" +
                    "2. Click 'Approve' button\n" +
                    "3. System automatically deducts from employee balance\n" +
                    "4. Employee receives approved status\n\n" +
                    "Rejecting Leave:\n" +
                    "1. Select a pending leave request\n" +
                    "2. Click 'Reject' button\n" +
                    "3. No balance changes occur\n" +
                    "4. Employee is notified of rejection\n\n" +
                    "Leave Balances:\n" +
                    "- Annual Leave: 20 days per year (standard)\n" +
                    "- Sick Leave: 10 days per year\n" +
                    "- Balances update automatically on approval\n\n" +
                    "Tips:\n" +
                    "- Review requests promptly to help planning\n" +
                    "- Check employee balances before approving\n" +
                    "- Sort by request date to prioritize\n" +
                    "- Approved/rejected requests cannot be changed";
            }
            else
            {
                helpMessage = "Leave Management Help - EMPLOYEE\n\n" +
                    "Submitting Leave Request:\n" +
                    "1. Select leave type (Annual or Sick Leave)\n" +
                    "2. Choose start and end dates\n" +
                    "3. Enter reason for your leave\n" +
                    "4. Click 'Submit Request'\n\n" +
                    "Leave Types:\n" +
                    "- Annual Leave: Vacation time, holidays\n" +
                    "- Sick Leave: When you're ill or need medical care\n\n" +
                    "Important Notes:\n" +
                    "- Requests must be for current or future dates\n" +
                    "- End date must be after start date\n" +
                    "- Provide clear reason for your request\n" +
                    "- Submit requests in advance when possible\n\n" +
                    "Request Status:\n" +
                    "- Pending: Waiting for manager approval\n" +
                    "- Approved: You can take the leave\n" +
                    "- Rejected: Request denied, contact your manager\n\n" +
                    "Checking Your Balance:\n" +
                    "- View remaining days on your Profile page\n" +
                    "- Balance updates after approval\n" +
                    "- Standard: 20 annual + 10 sick days per year\n\n" +
                    "Tips:\n" +
                    "- Submit requests early for better approval chances\n" +
                    "- Check your leave balance before requesting\n" +
                    "- Contact your manager for urgent leave needs";
            }

            MessageBox.Show(
                helpMessage,
                "Leave Management Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}