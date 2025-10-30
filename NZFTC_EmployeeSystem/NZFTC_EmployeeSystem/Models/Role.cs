using System.Collections.Generic;

namespace NZFTC_EmployeeSystem.Models
{
    /// <summary>
    /// This class represents a role that can be assigned to a user, such as "Admin" or "Employee".
    /// A role defines a set of permissions or responsibilities. This entity is used in a many-to-many
    /// relationship with users via the UserRole join table.
    /// </summary>
    public class Role
    {
        // Primary key for the role.
        public int Id { get; set; }

        // Name of the role (e.g. "Admin", "Employee", "Manager")
        public string Name { get; set; } = string.Empty;

        // Navigation property - roles can be assigned to many users through the UserRole table
        public List<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
