using DataLabelingSupportSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;
using static DataLabelingSupportSystem.DAL.Models.Enums;

namespace DataLabelingSupportSystem.DAL.DbContext
{
    public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<DataItem> DataItems => Set<DataItem>();
        public DbSet<Label> Labels => Set<Label>();
        public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
        public DbSet<TaskItem> TaskItems => Set<TaskItem>();
        public DbSet<DataItemSubmission> DataItemSubmissions => Set<DataItemSubmission>();
        public DbSet<DataItemReview> DataItemReviews => Set<DataItemReview>();
        public DbSet<Annotation> Annotations => Set<Annotation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>(e =>
            {
                e.ToTable("roles");
                e.HasKey(x => x.RoleId);

                e.Property(x => x.RoleId).ValueGeneratedOnAdd();
                e.Property(x => x.RoleName).IsRequired();
                e.HasIndex(x => x.RoleName).IsUnique();

                // Seed dữ liệu roles (tạo luôn khi chạy migration)
                e.HasData(
                    new Role { RoleId = 1, RoleName = "Admin" },
                    new Role { RoleId = 2, RoleName = "Manager" },
                    new Role { RoleId = 3, RoleName = "Annotator" },
                    new Role { RoleId = 4, RoleName = "Reviewer" }
                );
            });

            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("users");
                e.HasKey(x => x.UserId);
                e.Property(x => x.UserId).ValueGeneratedOnAdd();

                e.Property(x => x.Username).IsRequired();
                e.HasIndex(x => x.Username).IsUnique();

                e.Property(x => x.Password).IsRequired();

                e.HasOne(x => x.Role)
                 .WithMany(r => r.Users)
                 .HasForeignKey(x => x.RoleId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Project>(e =>
            {
                e.ToTable("projects");
                e.HasKey(x => x.ProjectId);
                e.Property(x => x.ProjectId).ValueGeneratedOnAdd();
                e.Property(x => x.Name).IsRequired();

                e.HasOne(x => x.Manager)
                 .WithMany(u => u.ManagedProjects)
                 .HasForeignKey(x => x.ManagerId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DataItem>(e =>
            {
                e.ToTable("data_items");
                e.HasKey(x => x.DataItemId);
                e.Property(x => x.DataItemId).ValueGeneratedOnAdd();
                e.Property(x => x.ImagePath).IsRequired();

                e.HasOne(x => x.Project)
                 .WithMany(p => p.DataItems)
                 .HasForeignKey(x => x.ProjectId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Label>(e =>
            {
                e.ToTable("labels");
                e.HasKey(x => x.LabelId);
                e.Property(x => x.LabelId).ValueGeneratedOnAdd();
                e.Property(x => x.Name).IsRequired();

                e.HasOne(x => x.Project)
                 .WithMany(p => p.Labels)
                 .HasForeignKey(x => x.ProjectId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.ProjectId, x.Name }).IsUnique();
            });

            modelBuilder.Entity<TaskEntity>(e =>
            {
                e.ToTable("tasks");
                e.HasKey(x => x.TaskId);
                e.Property(x => x.TaskId).ValueGeneratedOnAdd();

                e.Property(x => x.Status)
                 .IsRequired()
                 .HasConversion<int>()
                 .HasDefaultValue(Enums.TaskStatus.Assigned);

                e.HasOne(x => x.Project)
                 .WithMany(p => p.Tasks)
                 .HasForeignKey(x => x.ProjectId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Annotator)
                 .WithMany(u => u.Tasks)
                 .HasForeignKey(x => x.AnnotatorId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TaskItem>(e =>
            {
                e.ToTable("task_items");
                e.HasKey(x => x.TaskItemId);
                e.Property(x => x.TaskItemId).ValueGeneratedOnAdd();

                e.HasOne(x => x.Task)
                 .WithMany(t => t.TaskItems)
                 .HasForeignKey(x => x.TaskId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.DataItem)
                 .WithMany(d => d.TaskItems)
                 .HasForeignKey(x => x.DataItemId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(x => new { x.TaskId, x.DataItemId }).IsUnique();
            });

            modelBuilder.Entity<DataItemSubmission>(e =>
            {
                e.ToTable("data_item_submissions");
                e.HasKey(x => x.DataItemSubmissionId);
                e.Property(x => x.DataItemSubmissionId).ValueGeneratedOnAdd();

                e.Property(x => x.SubmittedAt).IsRequired();

                e.Property(x => x.Status)
                 .IsRequired()
                 .HasConversion<int>()
                 .HasDefaultValue(SubmissionStatus.Submitted);

                e.HasOne(x => x.TaskItem)
                 .WithMany(ti => ti.Submissions)
                 .HasForeignKey(x => x.TaskItemId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Submitter)
                 .WithMany(u => u.Submissions)
                 .HasForeignKey(x => x.SubmittedBy)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DataItemReview>(e =>
            {
                e.ToTable("data_item_reviews");
                e.HasKey(x => x.ReviewId);
                e.Property(x => x.ReviewId).ValueGeneratedOnAdd();

                e.Property(x => x.Decision)
                 .IsRequired()
                 .HasConversion<int>();

                e.Property(x => x.ReviewedAt).IsRequired();

                e.HasIndex(x => x.DataItemSubmissionId).IsUnique();

                e.HasOne(x => x.Submission)
                 .WithOne(s => s.Review)
                 .HasForeignKey<DataItemReview>(x => x.DataItemSubmissionId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Reviewer)
                 .WithMany(u => u.Reviews)
                 .HasForeignKey(x => x.ReviewerId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Annotation>(e =>
            {
                e.ToTable("annotations");
                e.HasKey(x => x.AnnotationId);
                e.Property(x => x.AnnotationId).ValueGeneratedOnAdd();

                e.Property(x => x.X).IsRequired();
                e.Property(x => x.Y).IsRequired();
                e.Property(x => x.Width).IsRequired();
                e.Property(x => x.Height).IsRequired();

                e.HasOne(x => x.Submission)
                 .WithMany(s => s.Annotations)
                 .HasForeignKey(x => x.DataItemSubmissionId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Label)
                 .WithMany(l => l.Annotations)
                 .HasForeignKey(x => x.LabelId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
