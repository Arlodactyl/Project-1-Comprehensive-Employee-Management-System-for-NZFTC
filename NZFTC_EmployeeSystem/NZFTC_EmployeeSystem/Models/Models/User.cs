using System;

namespace NZFTC_EmployeeSystem.Models
{
    // This class represents a user account in the system
    // It demonstrates ENCAPSULATION by keeping data private and controlled
    public class User
    {
        // Primary key for database
        public int Id { get; set; }

        // Login credentials
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Role defines permissions (Admin or Employee)
        // This is part of POLYMORPHISM - different behavior based on role
        public string Role { get; set; } = "Employee";

        // Foreign key linking to Employee table
        public int EmployeeId { get; set; }

        // Navigation property - shows ASSOCIATION between User and Employee
        public Employee? Employee { get; set; }

        // Account status
        public bool IsActive { get; set; } = true;

        // Audit fields
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastLoginDate { get; set; }
    }
}