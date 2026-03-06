using DataLabelingSupportSystem.DAL.DbContext;
using DataLabelingSupportSystem.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static DataLabelingSupportSystem.DAL.Models.Enums;

namespace DataLabelingSupportSystem;

/// <summary>
/// Seeds test data for Review Queue & Review Decision testing.
/// Idempotent: checks each entity before creating, safe to call every startup.
/// </summary>
public static class SeedTestData
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        // ── 1. Users (create only if username doesn't exist) ────
        var admin = await GetOrCreateUser(db, hasher, "admin", "Admin User", 1, "123456");
        var manager = await GetOrCreateUser(db, hasher, "manager", "Manager Tuan", 2, "123456");
        var annotator1 = await GetOrCreateUser(db, hasher, "annotator1", "Annotator Minh", 3, "123456");
        var annotator2 = await GetOrCreateUser(db, hasher, "annotator2", "Annotator Linh", 3, "123456");
        var reviewer = await GetOrCreateUser(db, hasher, "reviewer", "Reviewer Huy", 4, "123456");

        // ── 2. Project ──────────────────────────────────────────
        var project = await db.Set<Project>().FirstOrDefaultAsync(p => p.Name == "Traffic Signs Detection");
        if (project == null)
        {
            project = new Project
            {
                Name = "Traffic Signs Detection",
                Description = "Label traffic signs in street images",
                ManagerId = manager.UserId,
                Status = ProjectStatus.InProgress
            };
            db.Set<Project>().Add(project);
            await db.SaveChangesAsync();
        }

        // ── 3. Labels ───────────────────────────────────────────
        var labelCar = await GetOrCreateLabel(db, project.ProjectId, "Car", "#e74c3c");
        var labelPerson = await GetOrCreateLabel(db, project.ProjectId, "Person", "#2ecc71");
        var labelSign = await GetOrCreateLabel(db, project.ProjectId, "Traffic Sign", "#3498db");
        var labelBike = await GetOrCreateLabel(db, project.ProjectId, "Bicycle", "#f39c12");

        // ── 4. DataItems (sample placeholder images) ────────────
        var imageUrls = new[]
        {
            "https://images.unsplash.com/photo-1449824913935-59a10b8d2000?w=800",  // city street
            "https://images.unsplash.com/photo-1477959858617-67f85cf4f1df?w=800",  // urban road
            "https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800",  // car
            "https://images.unsplash.com/photo-1532274402911-5a369e4c4bb5?w=800",  // road
            "https://images.unsplash.com/photo-1517649763962-0c623066013b?w=800",  // cyclist
        };

        var dataItems = new List<DataItem>();
        foreach (var img in imageUrls)
        {
            var existing = await db.Set<DataItem>().FirstOrDefaultAsync(
                d => d.ProjectId == project.ProjectId && d.ImagePath == img);
            if (existing != null)
            {
                dataItems.Add(existing);
            }
            else
            {
                var di = new DataItem
                {
                    ProjectId = project.ProjectId,
                    Width = 800,
                    Height = 600,
                    ImagePath = img
                };
                db.Set<DataItem>().Add(di);
                await db.SaveChangesAsync();
                dataItems.Add(di);
            }
        }

        // ── 5. Tasks & TaskItems ────────────────────────────────
        // Task for annotator1: first 3 images
        var task1 = await db.Set<TaskEntity>().FirstOrDefaultAsync(
            t => t.ProjectId == project.ProjectId && t.AnnotatorId == annotator1.UserId);
        if (task1 == null)
        {
            task1 = new TaskEntity
            {
                ProjectId = project.ProjectId,
                AnnotatorId = annotator1.UserId,
                Status = Enums.TaskStatus.InProgress
            };
            db.Set<TaskEntity>().Add(task1);
            await db.SaveChangesAsync();
        }

        var taskItems1 = await EnsureTaskItems(db, task1.TaskId, dataItems.Take(3).ToList());

        // Task for annotator2: last 2 images
        var task2 = await db.Set<TaskEntity>().FirstOrDefaultAsync(
            t => t.ProjectId == project.ProjectId && t.AnnotatorId == annotator2.UserId);
        if (task2 == null)
        {
            task2 = new TaskEntity
            {
                ProjectId = project.ProjectId,
                AnnotatorId = annotator2.UserId,
                Status = Enums.TaskStatus.InProgress
            };
            db.Set<TaskEntity>().Add(task2);
            await db.SaveChangesAsync();
        }

        var taskItems2 = await EnsureTaskItems(db, task2.TaskId, dataItems.Skip(3).Take(2).ToList());

        // ── 6. Submissions with annotations ─────────────────────
        // Only create if no submissions exist for this project's task items
        var allTaskItemIds = taskItems1.Concat(taskItems2).Select(ti => ti.TaskItemId).ToList();
        var hasSubmissions = await db.Set<DataItemSubmission>()
            .AnyAsync(s => allTaskItemIds.Contains(s.TaskItemId));

        if (!hasSubmissions)
        {
            // Submission 1: Status = Submitted
            var sub1 = new DataItemSubmission
            {
                TaskItemId = taskItems1[0].TaskItemId,
                SubmittedBy = annotator1.UserId,
                SubmittedAt = DateTime.UtcNow.AddHours(-2),
                Status = SubmissionStatus.Submitted
            };
            db.Set<DataItemSubmission>().Add(sub1);
            await db.SaveChangesAsync();

            db.Set<Annotation>().AddRange(
                new Annotation { DataItemSubmissionId = sub1.DataItemSubmissionId, LabelId = labelCar.LabelId, X = 0.1f, Y = 0.3f, Width = 0.25f, Height = 0.2f },
                new Annotation { DataItemSubmissionId = sub1.DataItemSubmissionId, LabelId = labelPerson.LabelId, X = 0.5f, Y = 0.2f, Width = 0.1f, Height = 0.35f },
                new Annotation { DataItemSubmissionId = sub1.DataItemSubmissionId, LabelId = labelSign.LabelId, X = 0.7f, Y = 0.05f, Width = 0.08f, Height = 0.1f }
            );

            // Submission 2: Status = Submitted
            var sub2 = new DataItemSubmission
            {
                TaskItemId = taskItems1[1].TaskItemId,
                SubmittedBy = annotator1.UserId,
                SubmittedAt = DateTime.UtcNow.AddHours(-1),
                Status = SubmissionStatus.Submitted
            };
            db.Set<DataItemSubmission>().Add(sub2);
            await db.SaveChangesAsync();

            db.Set<Annotation>().AddRange(
                new Annotation { DataItemSubmissionId = sub2.DataItemSubmissionId, LabelId = labelCar.LabelId, X = 0.05f, Y = 0.4f, Width = 0.3f, Height = 0.25f },
                new Annotation { DataItemSubmissionId = sub2.DataItemSubmissionId, LabelId = labelCar.LabelId, X = 0.45f, Y = 0.35f, Width = 0.2f, Height = 0.2f },
                new Annotation { DataItemSubmissionId = sub2.DataItemSubmissionId, LabelId = labelBike.LabelId, X = 0.75f, Y = 0.5f, Width = 0.15f, Height = 0.2f }
            );

            // Submission 3: Status = InReview
            var sub3 = new DataItemSubmission
            {
                TaskItemId = taskItems1[2].TaskItemId,
                SubmittedBy = annotator1.UserId,
                SubmittedAt = DateTime.UtcNow.AddHours(-3),
                Status = SubmissionStatus.InReview
            };
            db.Set<DataItemSubmission>().Add(sub3);
            await db.SaveChangesAsync();

            db.Set<Annotation>().AddRange(
                new Annotation { DataItemSubmissionId = sub3.DataItemSubmissionId, LabelId = labelPerson.LabelId, X = 0.2f, Y = 0.15f, Width = 0.12f, Height = 0.4f },
                new Annotation { DataItemSubmissionId = sub3.DataItemSubmissionId, LabelId = labelPerson.LabelId, X = 0.6f, Y = 0.2f, Width = 0.1f, Height = 0.35f }
            );

            // Submission 4: Status = Submitted (from annotator2)
            var sub4 = new DataItemSubmission
            {
                TaskItemId = taskItems2[0].TaskItemId,
                SubmittedBy = annotator2.UserId,
                SubmittedAt = DateTime.UtcNow.AddMinutes(-30),
                Status = SubmissionStatus.Submitted
            };
            db.Set<DataItemSubmission>().Add(sub4);
            await db.SaveChangesAsync();

            db.Set<Annotation>().AddRange(
                new Annotation { DataItemSubmissionId = sub4.DataItemSubmissionId, LabelId = labelSign.LabelId, X = 0.3f, Y = 0.1f, Width = 0.1f, Height = 0.12f },
                new Annotation { DataItemSubmissionId = sub4.DataItemSubmissionId, LabelId = labelCar.LabelId, X = 0.15f, Y = 0.45f, Width = 0.35f, Height = 0.25f },
                new Annotation { DataItemSubmissionId = sub4.DataItemSubmissionId, LabelId = labelPerson.LabelId, X = 0.65f, Y = 0.3f, Width = 0.08f, Height = 0.3f },
                new Annotation { DataItemSubmissionId = sub4.DataItemSubmissionId, LabelId = labelBike.LabelId, X = 0.8f, Y = 0.4f, Width = 0.12f, Height = 0.18f }
            );

            // Submission 5: Status = Submitted (from annotator2)
            var sub5 = new DataItemSubmission
            {
                TaskItemId = taskItems2[1].TaskItemId,
                SubmittedBy = annotator2.UserId,
                SubmittedAt = DateTime.UtcNow.AddMinutes(-10),
                Status = SubmissionStatus.Submitted
            };
            db.Set<DataItemSubmission>().Add(sub5);
            await db.SaveChangesAsync();

            db.Set<Annotation>().AddRange(
                new Annotation { DataItemSubmissionId = sub5.DataItemSubmissionId, LabelId = labelCar.LabelId, X = 0.1f, Y = 0.5f, Width = 0.2f, Height = 0.15f },
                new Annotation { DataItemSubmissionId = sub5.DataItemSubmissionId, LabelId = labelSign.LabelId, X = 0.4f, Y = 0.05f, Width = 0.06f, Height = 0.08f }
            );

            await db.SaveChangesAsync();
        }
    }

    private static async Task<User> GetOrCreateUser(
        AppDbContext db, IPasswordHasher<User> hasher,
        string username, string name, int roleId, string password)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user != null) return user;

        user = new User { Username = username, Name = name, RoleId = roleId };
        user.Password = hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Label> GetOrCreateLabel(
        AppDbContext db, int projectId, string name, string color)
    {
        var label = await db.Set<Label>().FirstOrDefaultAsync(
            l => l.ProjectId == projectId && l.Name == name);
        if (label != null) return label;

        label = new Label { ProjectId = projectId, Name = name, Color = color };
        db.Set<Label>().Add(label);
        await db.SaveChangesAsync();
        return label;
    }

    private static async Task<List<TaskItem>> EnsureTaskItems(
        AppDbContext db, int taskId, List<DataItem> dataItems)
    {
        var existing = await db.Set<TaskItem>()
            .Where(ti => ti.TaskId == taskId)
            .ToListAsync();

        if (existing.Count > 0) return existing;

        var items = dataItems.Select(di => new TaskItem
        {
            TaskId = taskId,
            DataItemId = di.DataItemId
        }).ToList();

        db.Set<TaskItem>().AddRange(items);
        await db.SaveChangesAsync();
        return items;
    }
}
