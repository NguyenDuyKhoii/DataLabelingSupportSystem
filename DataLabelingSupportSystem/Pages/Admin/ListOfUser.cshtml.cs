using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using DataLabelingSupportSystem.BLL.Interface;
using DataLabelingSupportSystem.DTOs;
using DataLabelingSupportSystem.DAL.DbContext;
using Microsoft.EntityFrameworkCore;

namespace DataLabelingSupportSystem.UI.Pages.Admin
{
    public class ListOfUserModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _db;

        public ListOfUserModel(IUserService userService, AppDbContext db)
        {
            _userService = userService;
            _db = db;
        }

        public List<UserDto> Users { get; set; } = new();
        public SelectList? RoleList { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? RoleId { get; set; }

        [BindProperty]
        public CreateUserDto CreateUserInput { get; set; } = new();

        [BindProperty]
        public UpdateUserDto UpdateUserInput { get; set; } = new();

        public string? CreateErrorMessage { get; set; }
        public string? CreateSuccessMessage { get; set; }
        public string? UpdateErrorMessage { get; set; }
        public string? UpdateSuccessMessage { get; set; }
        public string? DeleteSuccessMessage { get; set; }

        public async Task OnGetAsync()

        {
            if (TempData["UpdateSuccessMessage"] != null)
                UpdateSuccessMessage = TempData["UpdateSuccessMessage"]?.ToString();
            if (TempData["DeleteSuccessMessage"] != null)
                DeleteSuccessMessage = TempData["DeleteSuccessMessage"]?.ToString();

            await LoadDataAsync();
        }

        public async Task<IActionResult> OnGetUpdateAsync(int id)
        {
            var user = await _userService.GetUserForUpdateAsync(id);
            if (user == null) return NotFound();

            UpdateUserInput = user;
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            ModelState.Clear();

            if (!TryValidateModel(CreateUserInput, nameof(CreateUserInput)))
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return new JsonResult(new { success = false, message = "Please check the entered information (Fields with *).", errors });
            }

            if (!string.IsNullOrWhiteSpace(CreateUserInput.Email))
            {
                if (await _userService.EmailExistsAsync(CreateUserInput.Email))
                {
                    return new JsonResult(new { success = false, message = "This email is already in use." });
                }
            }

            try
            {
                var result = await _userService.CreateUserAsync(CreateUserInput);
                if (result)
                {
                    TempData["CreateSuccessMessage"] = "User created successfully!";
                    return new JsonResult(new { success = true, message = "User created successfully!" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Could not create user. Username may already exist." });
                }
            }
            catch (InvalidOperationException ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"System Error: {ex.Message}" });
            }
        }




        public async Task<IActionResult> OnPostUpdateAsync()
        {
            ModelState.Clear();

            if (UpdateUserInput == null || UpdateUserInput.UserId <= 0)
                return new JsonResult(new { success = false, message = "Invalid UserId." });

            if (UpdateUserInput.RoleId <= 0)
                ModelState.AddModelError(nameof(UpdateUserInput.RoleId), "Please select a role.");

            if (!string.IsNullOrWhiteSpace(UpdateUserInput.Name) && UpdateUserInput.Name.Length < 2)
                ModelState.AddModelError(nameof(UpdateUserInput.Name), "Name must be at least 2 characters.");

            if (!string.IsNullOrWhiteSpace(UpdateUserInput.Email))
            {
                if (!UpdateUserInput.Email.Contains("@") || !UpdateUserInput.Email.Contains("."))
                {
                    ModelState.AddModelError(nameof(UpdateUserInput.Email), "Invalid email.");
                }
                else
                {
                    var currentUser = await _userService.GetUserForUpdateAsync(UpdateUserInput.UserId);
                    if (currentUser != null && currentUser.Email != UpdateUserInput.Email)
                    {
                        if (await _userService.EmailExistsAsync(UpdateUserInput.Email, UpdateUserInput.UserId))
                        {
                            ModelState.AddModelError(nameof(UpdateUserInput.Email), "This email is already in use.");
                        }
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return new JsonResult(new { success = false, message = "Please check the entered information.", errors });
            }

            try
            {
                var result = await _userService.UpdateUserAsync(UpdateUserInput);
                if (result)
                {
                    TempData["UpdateSuccessMessage"] = "User updated successfully!";
                    return new JsonResult(new { success = true, message = "User updated successfully!" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Could not update user." });
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
            }
        }


        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id);
                if (result)
                {
                    TempData["DeleteSuccessMessage"] = "User deleted successfully!";
                    return RedirectToPage("./ListOfUser", new { Search, RoleId });
                }
                else
                {
                    DeleteSuccessMessage = "Could not delete user.";
                    await LoadDataAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                DeleteSuccessMessage = $"Error: {ex.Message}";
                await LoadDataAsync();
                return Page();
            }
        }

        private async Task LoadDataAsync()
        {
            var allUsers = await _userService.GetUsersAsync(null, null, null);

            IEnumerable<UserDto> query = allUsers;

            query = query.Where(u => u.IsActive == true);

            if (!string.IsNullOrWhiteSpace(Search))
            {
                string searchTerm = Search.Trim().ToLower();
                query = query.Where(u =>
                    (!string.IsNullOrEmpty(u.Username) && u.Username.ToLower().Contains(searchTerm)) ||
                    (!string.IsNullOrEmpty(u.Name) && u.Name.ToLower().Contains(searchTerm)) ||
                    (!string.IsNullOrEmpty(u.Email) && u.Email.ToLower().Contains(searchTerm))
                );
            }

            if (RoleId.HasValue && RoleId.Value > 0)
            {
                query = query.Where(u => u.RoleId == RoleId.Value);
            }

            query = query.OrderByDescending(u => u.CreatedAt);
            Users = query.ToList();

            var roles = await _db.Roles
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            RoleList = new SelectList(roles, "RoleId", "RoleName");
        }
    }
}
