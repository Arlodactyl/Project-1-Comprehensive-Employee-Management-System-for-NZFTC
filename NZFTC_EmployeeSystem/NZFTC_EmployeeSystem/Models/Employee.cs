using System;

namespace NZFTC_EmployeeSystem.Models
{
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
        public int DepartmentId { get; set; } // Foreign key to Department

        // Navigation property - the department this employee belongs to
        public Department? Department { get; set; }

        public DateTime HireDate { get; set; } = DateTime.Now;

        // Salary information
        public decimal Salary { get; set; }
        public decimal TaxRate { get; set; }

        // Leave balances
        public int AnnualLeaveBalance { get; set; } = 20;
        public int SickLeaveBalance { get; set; } = 10;

        // Account status
        public bool IsActive { get; set; } = true;

        // Profile picture path - stores the file path to the employee's profile picture
        // Example: "C:\Users\YourName\Pictures\profile.jpg"
        // If null or empty, a default red circle will be shown
        public string? ProfilePicturePath { get; set; }

        // Full name helper
        public string FullName => $"{FirstName} {LastName}";
    }
}