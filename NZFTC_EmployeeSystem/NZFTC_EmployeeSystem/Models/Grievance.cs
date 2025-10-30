using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NZFTC_EmployeeSystem.Models
{
    // This class represents employee grievances
    // Demonstrates ENCAPSULATION, INHERITANCE, and POLYMORPHISM
    // INHERITANCE: Grievance inherits from BaseRequest
    public class Grievance : BaseRequest
    {
        // NOTE: Id, EmployeeId, Employee, Status inherited from BaseRequest
        // BaseRequest also provides RequestDate (replaces SubmittedDate)

        // Grievance details
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Response tracking
        public string? AdminResponse { get; set; }
        public int? HandledByUserId { get; set; }
        public DateTime? ResolvedDate { get; set; }

        // Navigation property - the user (admin) who handled this grievance
        // Associates HandledByUserId with a User entity
        [ForeignKey("HandledByUserId")]
        public virtual User? HandledByUser { get; set; }

        // POLYMORPHISM: Override abstract method from BaseRequest
        public override string GetRequestSummary()
        {
            return $"Grievance: {Title} - Status: {Status}";
        }

        // POLYMORPHISM: Override virtual method - grievances have different approval rules
        public override bool CanBeApproved()
        {
            // Grievances can move from "Open" to "InProgress" to "Closed"
            return Status == "Open" || Status == "InProgress";
        }

        // POLYMORPHISM: Override virtual method - all grievances are high priority
        public override string GetPriorityLevel()
        {
            return "High";
        }

        // POLYMORPHISM: Override virtual method - returns request type
        public override string GetRequestType()
        {
            return "Grievance";
        }
    }
}