using System;

namespace NZFTC_EmployeeSystem.Models
{
    // This class represents a leave request in the system
    // Demonstrates ENCAPSULATION and data validation
    public class LeaveRequest
    {
        // Primary key
        public int Id { get; set; }

        // Foreign key to Employee
        public int EmployeeId { get; set; }

        // Navigation property - shows ASSOCIATION
        public Employee? Employee { get; set; }

        // Leave details
        public string LeaveType { get; set; } = string.Empty; // Annual, Sick, Unpaid
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysRequested { get; set; }
        public string Reason { get; set; } = string.Empty;

        // Status tracking
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        // Approval information
        public int? ApprovedByUserId { get; set; }
        public DateTime? ApprovedDate { get; set; }

        // Navigation property - the user who approved this leave request
        // This creates a link to the User table using the ApprovedByUserId
        // field. It's optional because a leave may not yet be approved.
        public User? ApprovedByUser { get; set; }

        // Audit field
        public DateTime RequestDate { get; set; } = DateTime.Now;
    }
}