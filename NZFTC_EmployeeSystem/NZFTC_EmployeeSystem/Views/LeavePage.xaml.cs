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
    }
}