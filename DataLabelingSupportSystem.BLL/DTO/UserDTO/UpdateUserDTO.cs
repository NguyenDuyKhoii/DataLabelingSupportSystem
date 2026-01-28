using System.ComponentModel.DataAnnotations;

namespace DataLabelingSupportSystem.DTOs
{
    public class UpdateUserDto
    {
        [Required]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public int RoleId { get; set; }

        public bool IsActive { get; set; }
    }
}
