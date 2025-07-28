using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PESystem.Models;
using Microsoft.AspNetCore.Identity;
using System.Net.Http.Json;
using API_WEB.Dtos.Auth;

namespace PESystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _clientFactory;

        public AccountController(ApplicationDbContext context, IHttpClientFactory clientFactory)
        {
            _context = context;
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _clientFactory.CreateClient("ApiClient");
            var response = await client.PostAsJsonAsync("api/Auth/login", new LoginDto
            {
                Username = model.Username,
                Password = model.Password
            });

            if (response.IsSuccessStatusCode)
            {
                var apiUser = await response.Content.ReadFromJsonAsync<ApiUser>();
                if (apiUser != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, apiUser.Username),
                        new Claim(ClaimTypes.Role, apiUser.Role),
                        new Claim("AllowedAreas", apiUser.AllowedAreas ?? string.Empty)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                    return RedirectToAction("Home", "Home");
                }
            }

            ModelState.AddModelError("", "Username hoặc Password không đúng.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                var userExists = await _context.Users.AnyAsync(u => u.Username == model.Username);
                if (userExists)
                {
                    ModelState.AddModelError("", "Username đã tồn tại.");
                    return View(model);
                }

                var hasher = new PasswordHasher<User>();
                var user = new User
                {
                    Username = model.Username,
                    Password = hasher.HashPassword(null, model.Password),
                    FullName = model.FullName,
                    Email = model.Email,
                    Department = model.Department,  
                    Role = "User",
                    AllowedAreas = new List<string> {""} // Giá trị mặc định
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return RedirectToAction("Login");
            }

            return View(model);
        }


        //Khi username không có quyền truy cập, sẽ điều hướng đến đây!
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); //Xóa xác thực người dùng( xóa session)

            //// Xóa cache để ngăn người dùng quay lại trang trước sau khi đăng xuất
            //Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            //Response.Headers["Pragma"] = "no-cache";
            //Response.Headers["Expires"] = "0";

            // Xóa cookie sau khi đăng xuất
            Response.Cookies.Delete(".AspNetCore.Cookies");
            return RedirectToAction("Login");
        }
    }

    public class ApiUser
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? AllowedAreas { get; set; }
    }
}
