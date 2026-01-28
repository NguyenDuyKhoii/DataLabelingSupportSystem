using System.ComponentModel.DataAnnotations;

namespace DataLabelingSupportSystem.DTOs
{
    // Create DTO
    public class CreateUserDto
    {
        [Required(ErrorMessage = "Username is required")]
        [MinLength(3)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        [Required]
        public int RoleId { get; set; }
    }
}
