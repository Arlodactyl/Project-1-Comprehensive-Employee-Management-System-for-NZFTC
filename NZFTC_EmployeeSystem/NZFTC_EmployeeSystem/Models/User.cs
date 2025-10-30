using System;

namespace NZFTC_EmployeeSystem.Models
{
    // Import generic collection classes so we can use List<T>
    using System.Collections.Generic;
    // This class represents a user account in the system
    // It demonstrates ENCAPSULATION by keeping data private and controlled
    // A user logs in to the system and can have one or more roles assigned
    public class User
    {
        // Primary key for database
        public int Id { get; set; }

        // Login credentials
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Role defines permissions (Admin or Employee)
        // Earlier versions stored a single role as a string. With the
        // introduction of the Role entity and the UserRole join table,
        // this property can still be used to store a default role, but
        // users can now have multiple roles via the UserRoles navigation
        // property below.
        public string Role { get; set; } = "Employee";

        // Navigation property for many-to-many relationship with roles
        // A user can have many roles, and a role can belong to many users.
        public List<UserRole> UserRoles { get; set; } = new List<UserRole>();

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