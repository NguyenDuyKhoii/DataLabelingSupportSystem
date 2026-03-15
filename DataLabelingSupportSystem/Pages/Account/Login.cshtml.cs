using DataLabelingSupportSystem.BLL.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly IAuthService _auth;

    public LoginModel(IAuthService auth) => _auth = auth;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? Error { get; set; }

    public class InputModel
    {
        [Required] public string Username { get; set; } = "";
        [Required] public string Password { get; set; } = "";
        public bool RememberMe { get; set; }
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid) return Page();

        var user = await _auth.LoginAsync(Input.Username, Input.Password);
        if (user is null)
        {
            Error = "Incorrect account or password.";
            return Page();
        }

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim("FullName", user.Name ?? ""),
        new Claim(ClaimTypes.Role, user.RoleName),
    };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = Input.RememberMe });

        string roleHome = user.RoleName switch
        {
            // Admin goes directly to ListOfUser page
            "Admin" => "/Admin/ListOfUser",
            "Annotator" => "/Annotator",
            "Manager" => "/Manager",
            "Reviewer" => "/Reviewer",
            _ => "/"
        };

        // 2) Only return to returnUrl if it's local and NOT "/"
        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl) &&
            !string.Equals(returnUrl, "/", StringComparison.OrdinalIgnoreCase))
        {
            return LocalRedirect(returnUrl);
        }

        return LocalRedirect(roleHome);
    }
}
