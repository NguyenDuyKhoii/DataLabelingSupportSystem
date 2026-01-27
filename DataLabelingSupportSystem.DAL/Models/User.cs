namespace DataLabelingSupportSystem.DAL.Models;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Name { get; set; }

    // THÊM: Thuộc tính mới cho CRUD
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
    public ICollection<TaskEntity> Tasks { get; set; } = new List<TaskEntity>();
    public ICollection<DataItemSubmission> Submissions { get; set; } = new List<DataItemSubmission>();
    public ICollection<DataItemReview> Reviews { get; set; } = new List<DataItemReview>();
}
