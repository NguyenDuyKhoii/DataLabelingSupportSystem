using System.ComponentModel.DataAnnotations;

namespace DataLabelingSupportSystem.DTOs
{
    public class UpdateUserDto
    {
        [Required]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Tên bắt buộc")]
        public string Name { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vai trò bắt buộc")]
        public int RoleId { get; set; }

        public bool IsActive { get; set; }
    }
}
