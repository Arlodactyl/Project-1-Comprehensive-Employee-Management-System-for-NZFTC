using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NZFTC_EmployeeSystem.Models
{
    // INHERITANCE: LeaveRequest inherits from BaseRequest
    public class LeaveRequest : BaseRequest
    {
        // NOTE: Id, EmployeeId, Employee, Status, RequestDate inherited from BaseRequest

        // Leave-specific details
        public string LeaveType { get; set; } = string.Empty; // Annual, Sick, Unpaid
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysRequested { get; set; }
        public string Reason { get; set; } = string.Empty;

        // Approval information
        public int? ApprovedByUserId { get; set; }
        public DateTime? ApprovedDate { get; set; }

        // Navigation property - links to User who approved this leave
        [ForeignKey("ApprovedByUserId")]
        public virtual User? ApprovedByUser { get; set; }

        // POLYMORPHISM: Override abstract method
        public override string GetRequestSummary()
        {
            return $"{LeaveType}: {StartDate:dd/MM/yyyy} to {EndDate:dd/MM/yyyy} ({DaysRequested} days) - Status: {Status}";
        }

        // POLYMORPHISM: Override virtual method - custom validation for leave
        public override bool CanBeApproved()
        {
            bool baseCheck = base.CanBeApproved();
            bool hasValidDays = DaysRequested > 0 && DaysRequested <= 30;
            bool hasValidDates = EndDate >= StartDate;

            return baseCheck && hasValidDays && hasValidDates;
        }

        // POLYMORPHISM: Override virtual method - priority based on leave type
        public override string GetPriorityLevel()
        {
            if (LeaveType == "Sick Leave")
                return "High";
            else if (LeaveType == "Annual Leave")
                return "Normal";
            else
                return "Low";
        }

        // POLYMORPHISM: Override virtual method - returns request type
        public override string GetRequestType()
        {
            return "Leave Request";
        }
    }
}