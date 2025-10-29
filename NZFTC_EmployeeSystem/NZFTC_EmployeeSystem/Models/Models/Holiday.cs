using System;

namespace NZFTC_EmployeeSystem.Models
{
    // This class represents company holidays
    // Demonstrates ENCAPSULATION
    public class Holiday
    {
        // Primary key
        public int Id { get; set; }

        // Holiday details
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Type { get; set; } = "Public"; // Public, Company
        public string Description { get; set; } = string.Empty;
        public bool IsRecurring { get; set; } = false;

        // Audit fields
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public int CreatedByUserId { get; set; }

        // Navigation property - the user who created this holiday record
        public User? CreatedByUser { get; set; }
    }
}