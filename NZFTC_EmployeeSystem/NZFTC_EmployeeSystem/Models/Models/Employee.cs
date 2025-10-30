using System;

namespace NZFTC_EmployeeSystem.Models

    /// <summary>
    /// This class represents one employee in the database
    /// Each property below becomes a column in the Employees table
    /// </summary>
    public class Employee
    {
        // Primary Key - This is the unique ID for each employee
        // The database will automatically generate this number
        public int Id { get; set; }

        // Basic employee information
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        // Employment details
        public string JobTitle { get; set; } = string.Empty;

        // Department details
        // Instead of storing the department name directly on the employee,
        // we store a foreign key to the Department table. This makes
        // managing departments easier and more consistent. When you need
        // the department name, you can access it through the Department
        // navigation property.
        public int DepartmentId { get; set; } // Foreign key to Department

        // Navigation property - the department this employee belongs to
        // One department can have many employees (one-to-many relationship)
        public Department? Department { get; set; }

        public DateTime HireDate { get; set; } = DateTime.Now;

        // Salary information - decimal is used for money values to keep it legit
        public decimal Salary { get; set; }
        public decimal TaxRate { get; set; } // Store as percentage (e.g., 15 means 15%)

        // Leave balances - how many days of leave the employee has
        public int AnnualLeaveBalance { get; set; } = 20; // Default 20 days per year
        public int SickLeaveBalance { get; set; } = 10;   // Default 10 days per year

        // Account status
        public bool IsActive { get; set; } = true; // Is the employee currently working here?

        // Full name helper - this combines first and last name
        // This is NOT stored in database, it's calculated when needed
        public string FullName => $"{FirstName} {LastName}";
    }
}
