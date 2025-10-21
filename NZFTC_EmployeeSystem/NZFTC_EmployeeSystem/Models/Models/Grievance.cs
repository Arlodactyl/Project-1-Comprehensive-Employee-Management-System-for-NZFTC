using System;

namespace NZFTC_EmployeeSystem.Models
{
    // This class represents employee grievances
    // Demonstrates ENCAPSULATION
    public class Grievance
    {
        // Primary key
        public int Id { get; set; }

        // Foreign key to Employee
        public int EmployeeId { get; set; }

        // Navigation property
        public Employee? Employee { get; set; }

        // Grievance details
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Open"; // Open, InProgress, Resolved, Closed

        // Response tracking
        public string? AdminResponse { get; set; }
        public int? HandledByUserId { get; set; }
        public DateTime? ResolvedDate { get; set; }

        // Audit fields
        public DateTime SubmittedDate { get; set; } = DateTime.Now;
    }
}