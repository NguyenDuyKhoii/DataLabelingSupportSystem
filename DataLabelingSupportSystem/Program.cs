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
    // Cho phép vào các trang Account (Login/Register/Logout/AccessDenied...)
    options.Conventions.AllowAnonymousToFolder("/Account"); 

    // Khóa theo role cho từng khu vực
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Annotator", "AnnotatorOnly");
    options.Conventions.AuthorizeFolder("/Manager", "ManagerOnly");
    options.Conventions.AuthorizeFolder("/Reviewer", "ReviewerOnly");
});

// 1) Register DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) DI layers
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectService, ProjectService>();


builder.Services.AddScoped<ILabelRepository, LabelRepository>();
builder.Services.AddScoped<ILabelService, LabelService>();


builder.Services.AddScoped<IDataItemService, DataItemService>();
builder.Services.AddScoped<IDataItemRepository, DataItemRepository>();

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
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Mặc định toàn bộ Razor Pages phải đăng nhập,
// nhưng /Account đã được AllowAnonymousToFolder nên vẫn truy cập được. [web:38]
app.MapRazorPages().RequireAuthorization();

app.Run();
