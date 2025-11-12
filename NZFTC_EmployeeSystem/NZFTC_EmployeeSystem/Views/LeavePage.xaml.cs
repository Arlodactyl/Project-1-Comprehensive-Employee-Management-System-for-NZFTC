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

            // Set date pickers to only allow today or future dates
            StartDatePicker.DisplayDateStart = DateTime.Today;
            EndDatePicker.DisplayDateStart = DateTime.Today;

            // Set maximum date to 1 year ahead
            StartDatePicker.DisplayDateEnd = DateTime.Today.AddYears(1);
            EndDatePicker.DisplayDateEnd = DateTime.Today.AddYears(1);

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
            // Step 1: Validate leave type selection
            if (LeaveTypeCombo.SelectedItem == null)
            {
                MessageBox.Show(
                    "Please select a leave type.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Step 2: Validate dates are selected
            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show(
                    "Please select both start and end dates.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Step 3: Validate reason is provided
            if (string.IsNullOrWhiteSpace(ReasonTextBox.Text))
            {
                MessageBox.Show(
                    "Please enter a reason for your leave request.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Step 3b: Validate reason length - minimum 10 characters for meaningful reason
            if (ReasonTextBox.Text.Trim().Length < 10)
            {
                MessageBox.Show(
                    "Please provide a more detailed reason (at least 10 characters).",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Step 3c: Validate reason length - maximum 500 characters
            if (ReasonTextBox.Text.Trim().Length > 500)
            {
                MessageBox.Show(
                    "Reason is too long. Please keep it under 500 characters.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Step 3d: Validate reason contains letters (not just numbers/symbols)
            if (!ReasonTextBox.Text.Any(char.IsLetter))
            {
                MessageBox.Show(
                    "Please provide a valid reason with descriptive text.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Get dates for validation
            var startDate = StartDatePicker.SelectedDate.Value;
            var endDate = EndDatePicker.SelectedDate.Value;
            var today = DateTime.Today;

            // Step 4: Validate start date is not in the past
            if (startDate.Date < today)
            {
                MessageBox.Show(
                    $"Start date cannot be in the past.\n\nPlease select today ({today:dd/MM/yyyy}) or a future date.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Step 5: Validate end date is not in the past
            if (endDate.Date < today)
            {
                MessageBox.Show(
                    $"End date cannot be in the past.\n\nPlease select today ({today:dd/MM/yyyy}) or a future date.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Step 6: Validate end date is after or equal to start date
            if (endDate.Date < startDate.Date)
            {
                MessageBox.Show(
                    "End date must be on or after the start date.\n\nPlease check your dates.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Step 7: Calculate and validate duration
            var days = (int)(endDate.Date - startDate.Date).TotalDays + 1;

            // Validate maximum leave duration - 30 days at once
            if (days > 30)
            {
                MessageBox.Show(
                    $"Leave requests cannot exceed 30 consecutive days.\n\n" +
                    $"You requested: {days} days\n\n" +
                    "Please split longer absences into separate requests or contact your manager.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Step 8: Validate leave is not too far in advance (max 1 year ahead)
            if (startDate.Date > today.AddYears(1))
            {
                MessageBox.Show(
                    "Leave requests cannot be made more than 1 year in advance.\n\n" +
                    "Please submit your request closer to the intended dates.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Step 9: Get leave type
            var leaveType = ((ComboBoxItem)LeaveTypeCombo.SelectedItem).Content.ToString();

            using (var db = new AppDbContext())
            {
                // Step 10: Get employee record
                var employee = db.Employees.Find(_currentUser.EmployeeId);
                if (employee == null)
                {
                    MessageBox.Show(
                        "Error: Employee record not found.\n\nPlease contact IT support.",
                        "System Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Step 11: Check leave balance
                decimal currentBalance = leaveType == "Annual Leave"
                    ? employee.AnnualLeaveBalance
                    : employee.SickLeaveBalance;

                if (days > currentBalance)
                {
                    MessageBox.Show(
                        $"Insufficient {leaveType.ToLower()} balance.\n\n" +
                        $"Days requested: {days}\n" +
                        $"Available balance: {currentBalance} days\n\n" +
                        "Please adjust your request or contact your manager.",
                        "Insufficient Balance",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Step 12: Check for overlapping leave requests
                var overlappingLeave = db.LeaveRequests
                    .Where(lr => lr.EmployeeId == _currentUser.EmployeeId &&
                                 lr.Status != "Rejected" &&
                                 ((lr.StartDate <= endDate && lr.EndDate >= startDate)))
                    .ToList();

                if (overlappingLeave.Any())
                {
                    var overlap = overlappingLeave.First();
                    MessageBox.Show(
                        $"You already have a leave request for overlapping dates.\n\n" +
                        $"Existing request:\n" +
                        $"Type: {overlap.LeaveType}\n" +
                        $"Dates: {overlap.StartDate:dd/MM/yyyy} - {overlap.EndDate:dd/MM/yyyy}\n" +
                        $"Status: {overlap.Status}\n\n" +
                        "Please choose different dates or cancel the existing request first.",
                        "Overlapping Leave Request",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Step 13: Check for too many pending requests (max 5)
                var pendingCount = db.LeaveRequests
                    .Count(lr => lr.EmployeeId == _currentUser.EmployeeId &&
                                 lr.Status == "Pending");

                if (pendingCount >= 5)
                {
                    MessageBox.Show(
                        $"You have {pendingCount} pending leave requests.\n\n" +
                        "Please wait for approval on existing requests before submitting new ones.",
                        "Too Many Pending Requests",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Step 14: Warn if requesting leave very soon (less than 3 days notice for non-sick leave)
                if (leaveType == "Annual Leave" && (startDate.Date - today).TotalDays < 3)
                {
                    var result = MessageBox.Show(
                        $"Notice: You are requesting annual leave with less than 3 days notice.\n\n" +
                        $"Leave starts: {startDate:dd/MM/yyyy}\n" +
                        $"Days notice: {(int)(startDate.Date - today).TotalDays}\n\n" +
                        "This may require manager approval. Do you want to continue?",
                        "Short Notice Warning",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                        return;
                }

                // Step 15: Create and save leave request
                var leaveRequest = new LeaveRequest
                {
                    EmployeeId = _currentUser.EmployeeId,
                    LeaveType = leaveType,
                    StartDate = startDate,
                    EndDate = endDate,
                    DaysRequested = days,
                    Reason = ReasonTextBox.Text.Trim(),
                    Status = "Pending",
                    RequestDate = DateTime.Now
                };

                db.LeaveRequests.Add(leaveRequest);
                db.SaveChanges();
            }

            MessageBox.Show(
                $"Leave request submitted successfully!\n\n" +
                $"Type: {leaveType}\n" +
                $"Dates: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}\n" +
                $"Days: {days}\n\n" +
                "Your request is now pending manager approval.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

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
            // Step 1: Validate selection
            var selectedLeave = AllLeaveGrid.SelectedItem as LeaveRequest;
            if (selectedLeave == null)
            {
                MessageBox.Show(
                    "Please select a leave request from the grid to approve.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Step 2: Validate the request is pending
            if (selectedLeave.Status != "Pending")
            {
                MessageBox.Show(
                    $"This leave request has already been {selectedLeave.Status.ToLower()}.\n\n" +
                    "Only pending requests can be approved.",
                    "Already Processed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Step 3: Check if leave dates have already passed
            if (selectedLeave.EndDate.Date < DateTime.Today)
            {
                var result = MessageBox.Show(
                    $"Warning: This leave period has already ended.\n\n" +
                    $"End date: {selectedLeave.EndDate:dd/MM/yyyy}\n\n" +
                    "Do you still want to approve it?",
                    "Past Leave Request",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            using (var db = new AppDbContext())
            {
                // Step 4: Get fresh copy from database
                var leave = db.LeaveRequests.Find(selectedLeave.Id);
                if (leave == null)
                {
                    MessageBox.Show(
                        "Leave request not found in database.\n\nPlease refresh the page.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Step 5: Double-check status hasn't changed
                if (leave.Status != "Pending")
                {
                    MessageBox.Show(
                        $"This request was recently {leave.Status.ToLower()} by another user.\n\n" +
                        "Refreshing the page...",
                        "Already Processed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    LoadLeaveData();
                    return;
                }

                // Step 6: Get employee and validate balance
                var employee = db.Employees.Find(leave.EmployeeId);
                if (employee == null)
                {
                    MessageBox.Show(
                        "Employee record not found.\n\nCannot approve leave request.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Step 7: Check if employee has sufficient balance
                decimal currentBalance = leave.LeaveType == "Annual Leave"
                    ? employee.AnnualLeaveBalance
                    : employee.SickLeaveBalance;

                if (leave.DaysRequested > currentBalance)
                {
                    var result = MessageBox.Show(
                        $"Warning: Employee has insufficient {leave.LeaveType.ToLower()} balance.\n\n" +
                        $"Days requested: {leave.DaysRequested}\n" +
                        $"Current balance: {currentBalance} days\n" +
                        $"Shortfall: {leave.DaysRequested - currentBalance} days\n\n" +
                        "Approving will result in a negative balance.\n\n" +
                        "Do you want to approve anyway?",
                        "Insufficient Balance Warning",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                        return;
                }

                // Step 8: Final confirmation
                var confirmResult = MessageBox.Show(
                    $"Approve this leave request?\n\n" +
                    $"Employee: {employee.FullName}\n" +
                    $"Type: {leave.LeaveType}\n" +
                    $"Dates: {leave.StartDate:dd/MM/yyyy} - {leave.EndDate:dd/MM/yyyy}\n" +
                    $"Days: {leave.DaysRequested}\n" +
                    $"Reason: {leave.Reason}\n\n" +
                    $"Current balance: {currentBalance} days\n" +
                    $"New balance: {currentBalance - leave.DaysRequested} days",
                    "Confirm Approval",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult != MessageBoxResult.Yes)
                    return;

                // Step 9: Approve the request
                leave.Status = "Approved";
                leave.ApprovedByUserId = _currentUser.Id;
                leave.ApprovedDate = DateTime.Now;

                // Step 10: Update employee's leave balance
                if (leave.LeaveType == "Annual Leave")
                    employee.AnnualLeaveBalance -= leave.DaysRequested;
                else if (leave.LeaveType == "Sick Leave")
                    employee.SickLeaveBalance -= leave.DaysRequested;

                db.SaveChanges();

                MessageBox.Show(
                    $"Leave request approved successfully!\n\n" +
                    $"Employee: {employee.FullName}\n" +
                    $"New {leave.LeaveType.ToLower()} balance: {(leave.LeaveType == "Annual Leave" ? employee.AnnualLeaveBalance : employee.SickLeaveBalance)} days",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            LoadLeaveData();
        }

        // Reject leave button (Admin only)
        private void RejectLeave_Click(object sender, RoutedEventArgs e)
        {
            // Step 1: Validate selection
            var selectedLeave = AllLeaveGrid.SelectedItem as LeaveRequest;
            if (selectedLeave == null)
            {
                MessageBox.Show(
                    "Please select a leave request from the grid to reject.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Step 2: Validate the request is pending
            if (selectedLeave.Status != "Pending")
            {
                MessageBox.Show(
                    $"This leave request has already been {selectedLeave.Status.ToLower()}.\n\n" +
                    "Only pending requests can be rejected.",
                    "Already Processed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            using (var db = new AppDbContext())
            {
                // Step 3: Get fresh copy from database
                var leave = db.LeaveRequests.Find(selectedLeave.Id);
                if (leave == null)
                {
                    MessageBox.Show(
                        "Leave request not found in database.\n\nPlease refresh the page.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Step 4: Double-check status hasn't changed
                if (leave.Status != "Pending")
                {
                    MessageBox.Show(
                        $"This request was recently {leave.Status.ToLower()} by another user.\n\n" +
                        "Refreshing the page...",
                        "Already Processed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    LoadLeaveData();
                    return;
                }

                // Step 5: Get employee info
                var employee = db.Employees.Find(leave.EmployeeId);
                if (employee == null)
                {
                    MessageBox.Show(
                        "Employee record not found.\n\nCannot process rejection.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Step 6: Confirm rejection
                var confirmResult = MessageBox.Show(
                    $"Reject this leave request?\n\n" +
                    $"Employee: {employee.FullName}\n" +
                    $"Type: {leave.LeaveType}\n" +
                    $"Dates: {leave.StartDate:dd/MM/yyyy} - {leave.EndDate:dd/MM/yyyy}\n" +
                    $"Days: {leave.DaysRequested}\n" +
                    $"Reason: {leave.Reason}\n\n" +
                    "The employee will be notified of the rejection.\n" +
                    "Their leave balance will not be affected.",
                    "Confirm Rejection",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult != MessageBoxResult.Yes)
                    return;

                // Step 7: Reject the request
                leave.Status = "Rejected";
                leave.ApprovedByUserId = _currentUser.Id;
                leave.ApprovedDate = DateTime.Now;
                db.SaveChanges();

                MessageBox.Show(
                    $"Leave request rejected.\n\n" +
                    $"Employee: {employee.FullName}\n" +
                    $"No changes made to leave balance.",
                    "Rejection Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

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