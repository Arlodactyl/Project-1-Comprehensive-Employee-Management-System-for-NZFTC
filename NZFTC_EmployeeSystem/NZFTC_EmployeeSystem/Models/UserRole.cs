namespace NZFTC_EmployeeSystem.Models
{
    /// <summary>
    /// Junction table to link users and roles in a many-to-many relationship.
    /// Each instance associates one user with one role.
    /// </summary>
    public class UserRole
    {
        // Foreign key to User
        public int UserId { get; set; }

        // Navigation property to the user
        public User? User { get; set; }

        // Foreign key to Role
        public int RoleId { get; set; }

        // Navigation property to the role
        public Role? Role { get; set; }
    }
}
