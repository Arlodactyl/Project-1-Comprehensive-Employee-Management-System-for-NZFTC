using System;

namespace NZFTC_EmployeeSystem.Models
{
    /// <summary>
    /// Represents one row in the Employees table.
    /// [DB HOOK] Add/modify properties to match your schema.
    /// </summary>
    public class Employee   // must be public (fixes CS0053)
    {
        public int Id { get; set; }                 // PK
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Role { get; set; } = "";
        public DateTime HiredOn { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
