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
            // BƯỚC 1: Xóa sạch toàn bộ trạng thái lỗi hiện tại
            // (Điều này giúp loại bỏ mọi lỗi "oan" do UpdateUserInput gây ra)
            ModelState.Clear();

            // BƯỚC 2: Validate lại thủ công CHỈ RIÊNG CreateUserInput
            // Hàm này sẽ trả về true nếu CreateUserInput hợp lệ, false nếu thiếu dữ liệu
            if (!TryValidateModel(CreateUserInput, nameof(CreateUserInput)))
            {
                // Debug: Ghi lỗi ra cửa sổ Output để bạn kiểm tra nếu cần
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi field {state.Key}: {error.ErrorMessage}");
                    }
                }

                await LoadDataAsync();
                CreateErrorMessage = "Vui lòng kiểm tra lại thông tin đã nhập (Các trường có dấu *).";
                return Page();
            }

            // BƯỚC 3: Logic nghiệp vụ (Kiểm tra Email trùng)
            if (!string.IsNullOrWhiteSpace(CreateUserInput.Email))
            {
                if (await _userService.EmailExistsAsync(CreateUserInput.Email))
                {
                    ModelState.AddModelError(nameof(CreateUserInput.Email), "Email này đã được sử dụng.");
                    await LoadDataAsync();
                    CreateErrorMessage = "Email này đã được sử dụng.";
                    return Page();
                }
            }

            // BƯỚC 4: Gọi Service tạo mới
            try
            {
                var result = await _userService.CreateUserAsync(CreateUserInput);
                if (result)
                {
                    CreateSuccessMessage = "Tạo user mới thành công!";

                    // Reset dữ liệu form
                    CreateUserInput = new CreateUserDto();
                    ModelState.Clear();

                    await LoadDataAsync();
                    return Page();
                }
                else
                {
                    CreateErrorMessage = "Không thể tạo user. Username có thể đã tồn tại.";
                    await LoadDataAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                CreateErrorMessage = $"Lỗi hệ thống: {ex.Message}";
                await LoadDataAsync();
                return Page();
            }
        }



        public async Task<IActionResult> OnPostUpdateAsync()
        {
            ModelState.Clear();

            if (UpdateUserInput == null || UpdateUserInput.UserId <= 0)
            {
                ModelState.AddModelError("", "UserId không hợp lệ.");
                await LoadDataAsync();
                return Page();
            }

            if (UpdateUserInput.RoleId <= 0)
                ModelState.AddModelError(nameof(UpdateUserInput.RoleId), "Vui lòng chọn vai trò.");

            if (!string.IsNullOrWhiteSpace(UpdateUserInput.Name) && UpdateUserInput.Name.Length < 2)
                ModelState.AddModelError(nameof(UpdateUserInput.Name), "Tên phải có ít nhất 2 ký tự.");

            if (!string.IsNullOrWhiteSpace(UpdateUserInput.Email))
            {
                if (!UpdateUserInput.Email.Contains("@") || !UpdateUserInput.Email.Contains("."))
                {
                    ModelState.AddModelError(nameof(UpdateUserInput.Email), "Email không hợp lệ.");
                }
                else
                {
                    var currentUser = await _userService.GetUserForUpdateAsync(UpdateUserInput.UserId);
                    if (currentUser != null && currentUser.Email != UpdateUserInput.Email)
                    {
                        if (await _userService.EmailExistsAsync(UpdateUserInput.Email, UpdateUserInput.UserId))
                        {
                            ModelState.AddModelError(nameof(UpdateUserInput.Email), "Email này đã được sử dụng.");
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(UpdateUserInput.Phone))
            {
                if (!UpdateUserInput.Phone.All(char.IsDigit) || UpdateUserInput.Phone.Length != 10)
                    ModelState.AddModelError(nameof(UpdateUserInput.Phone), "Số điện thoại phải đúng 10 số.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                UpdateErrorMessage = "Vui lòng kiểm tra lại thông tin đã nhập.";
                return Page();
            }

            try
            {
                var result = await _userService.UpdateUserAsync(UpdateUserInput);

                if (result)
                {
                    TempData["UpdateSuccessMessage"] = "Cập nhật user thành công!";
                    return RedirectToPage("./ListOfUser", new { Search, RoleId });
                }
                else
                {
                    UpdateErrorMessage = "Không thể cập nhật user.";
                    await LoadDataAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                UpdateErrorMessage = $"Lỗi: {ex.Message}";
                await LoadDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id);
                if (result)
                {
                    TempData["DeleteSuccessMessage"] = "Xóa user thành công!";
                    return RedirectToPage("./ListOfUser", new { Search, RoleId });
                }
                else
                {
                    DeleteSuccessMessage = "Không thể xóa user.";
                    await LoadDataAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                DeleteSuccessMessage = $"Lỗi: {ex.Message}";
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
