using DataLabelingSupportSystem;
using DataLabelingSupportSystem.BLL.Interface;
using DataLabelingSupportSystem.BLL.Services;
using DataLabelingSupportSystem.DAL.DbContext;
using DataLabelingSupportSystem.DAL.Interfaces;
using DataLabelingSupportSystem.DAL.Models;
using DataLabelingSupportSystem.DAL.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Razor Pages + Authorization conventions
builder.Services.AddRazorPages(options =>
{
    // Allow access to Account pages (Login/Register/Logout/AccessDenied...)
    options.Conventions.AllowAnonymousToFolder("/Account"); 

    // Role-based authorization for each folder
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Annotator", "AnnotatorOnly");
    options.Conventions.AuthorizeFolder("/Manager", "ManagerOnly");
    // We removed folder-level authorization for /Reviewer to allow Manager into the Review page.
    // Index.cshtml.cs and Review.cshtml.cs now manage their own [Authorize] attributes.
});

// 1) Register DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) DI layers
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectService, ProjectService>();


builder.Services.AddScoped<ILabelRepository, LabelRepository>();
builder.Services.AddScoped<ILabelService, LabelService>();


builder.Services.AddScoped<IDataItemService, DataItemService>();
builder.Services.AddScoped<IDataItemRepository, DataItemRepository>();

builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();

builder.Services.AddScoped<IAnnotationService, AnnotationService>();
builder.Services.AddScoped<IAdminService, AdminService>();

// 3) AuthN (Cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
        options.SlidingExpiration = true;
    });

// 4) AuthZ (Policies)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("AnnotatorOnly", p => p.RequireRole("Annotator"));
    options.AddPolicy("ManagerOnly", p => p.RequireRole("Manager"));
    options.AddPolicy("ReviewerOnly", p => p.RequireRole("Reviewer"));
    options.AddPolicy("ReviewerOrManager", p => p.RequireRole("Reviewer", "Manager"));
});

var app = builder.Build();


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// By default all Razor Pages require login,
// but /Account has been allowed anonymous access in its folder.
app.MapRazorPages().RequireAuthorization();

app.Run();
