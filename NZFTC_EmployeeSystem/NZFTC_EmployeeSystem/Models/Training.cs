using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NZFTC_EmployeeSystem.Models
{
    /// <summary>
    /// Represents a training record for an employee
    /// Tracks completion status and sign-off details
    /// </summary>
    public class Training
    {
        // Primary key - unique identifier for each training record
        public int Id { get; set; }

        // Foreign key linking to the employee who took the training
        public int EmployeeId { get; set; }

        // Navigation property to access the related employee
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        // Type of training completed
        // Examples: "Ethics Training", "Induction", "Health & Safety", "Fire Safety"
        public string TrainingType { get; set; } = string.Empty;

        // Current status of the training
        // Values: "Not Started", "In Progress", "Completed"
        public string Status { get; set; } = "Not Started";

        // Date when the training was completed (null if not completed yet)
        public DateTime? CompletedDate { get; set; }

        // User ID of the admin who signed off on the training completion
        public int? SignedOffByUserId { get; set; }

        // Navigation property to the user who signed off
        [ForeignKey("SignedOffByUserId")]
        public virtual User? SignedOffByUser { get; set; }

        // Optional notes about the training
        public string? Notes { get; set; }
    }
}