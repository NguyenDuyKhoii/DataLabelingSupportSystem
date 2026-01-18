using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace DataLabelingSupportSystem.UI.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly IAuthService _auth;

        public RegisterModel(IAuthService auth) => _auth = auth;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? Error { get; set; }

        public class InputModel
        {
            [Required]
            public string Username { get; set; } = "";

            [Required]
            [MinLength(6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = "";

            [Required]
            [DataType(DataType.Password)]
            [Compare(nameof(Password), ErrorMessage = "The password entered again does not match.")]
            public string ConfirmPassword { get; set; } = "";

            public string? Name { get; set; }
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (!ModelState.IsValid) return Page();

            try
            {
                await _auth.RegisterAsync(
                    new RegisterUserDto(Input.Username, Input.Password, Input.Name));

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return RedirectToPage("/Account/Login", new { returnUrl }); 

                return RedirectToPage("/Account/Login");
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                return Page();
            }
        }
    }
}
