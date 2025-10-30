using System.Collections.Generic;

namespace NZFTC_EmployeeSystem.Models
{
    /// <summary>
    /// This class represents a department within the company.
    /// A department groups employees together under a common area like
    /// "IT", "Human Resources", or "Finance". Each department
    /// has a unique identifier and a name.
    ///
    /// Using a separate table for departments instead of storing
    /// the department name directly on the employee makes our database
    /// more flexible and normalized. Many employees can belong to the
    /// same department (one-to-many relationship).
    /// </summary>
    public class Department
    {
        // Primary key for the department. Each department gets
        // its own unique identifier.
        public int Id { get; set; }

        // The name of the department (e.g. "IT", "HR").
        public string Name { get; set; } = string.Empty;

        // Navigation property - a department can have many employees.
        // List<Employee> represents the one-to-many relationship
        // where one department contains multiple employees.
        public List<Employee> Employees { get; set; } = new List<Employee>();
    }
}
