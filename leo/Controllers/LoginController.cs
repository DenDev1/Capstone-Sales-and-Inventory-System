
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using leo.Models;
using leo.Data;
using leo.Services;
using Microsoft.AspNetCore.Authorization;

namespace leo.Controllers
{

    public class LoginController : Controller
    {
        private readonly leoContext _context;
        private readonly PasswordResetService _passwordResetService; // Add this
        public LoginController(leoContext context, PasswordResetService passwordResetService)

        {

            _context = context;
            _passwordResetService = passwordResetService; // Initialize the service
        }


        // GET: Login/Index
        public IActionResult Index()
        {



            // Check if user is already authenticated, redirect to Home if true
            ClaimsPrincipal claimuser = HttpContext.User;
            if (claimuser.Identity.IsAuthenticated)

 
            return RedirectToAction("Index", "Dashboard");
     
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> LoginAsync([Bind("Username, Password")] Users user)
        {
            if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
            {
                ModelState.AddModelError("", "Please input all required fields");
                return View("Index", user);
            }

            string hashedPassword = HashingServices.HashData(user.Password);
            Users loginUser = _context.Users
                .FirstOrDefault(q => q.Username == user.Username && q.Password == hashedPassword);

            if (loginUser == null)
            {
                ModelState.AddModelError("", "Invalid username or password");
                return View("Index", user);
            }

            // Define claims
            List<Claim> claims = new List<Claim>()
    {
        new Claim(ClaimTypes.NameIdentifier, user.Username),
       new Claim(ClaimTypes.Email, loginUser.Email) // Change to ClaimTypes.Email
    };

            // Retrieve user role from your role management system
            var userRole = GetUserRole(loginUser.RoleId); // Replace with your method to get the role

            // Add role claim based on user role
            if (userRole == "Admin")
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
            else if (userRole == "Staff")
            {
                claims.Add(new Claim(ClaimTypes.Role, "Staff"));
            }

            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            AuthenticationProperties properties = new AuthenticationProperties()
            {
                AllowRefresh = true,
                IsPersistent = false
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), properties);

            //TempData["SuccessMessage"] = "Login successfully!";
            HttpContext.Session.SetString("FullName", loginUser.FullName);

            // Set TempData for login success
            TempData["LoginSuccess"] = "Login successful! Welcome back.";
            return RedirectToAction("Index", "Dashboard");
        }

        // Example method to get user role
        private string GetUserRole(int userId)
        {
            // Replace this with your actual logic to retrieve the user's role
            // This might involve querying a roles table or similar mechanism
            var role = _context.Role.FirstOrDefault(r => r.RoleId== userId)?.RoleName;
            return role;
        }


        // GET: Login/Logout
        public IActionResult Logout()
        {
            // Set TempData for logout success message
            TempData["LogoutSuccess"] = "Logout successful! You have been logged out.";
            return View();
        }

        // GET: Login/LogoutAsync
        [HttpGet]
        public async Task<IActionResult> LogoutAsync()
        {
            // Sign out the user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Clear session data
            HttpContext.Session.Clear();
            // Set success message


            return RedirectToAction("Index", "Login");
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError("", "No account found with this email address.");
                return View("Index");
            }

            var resetCode = new Random().Next(100000, 999999).ToString(); // Generate a 6-digit code
            user.ResetToken = resetCode;
            user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(59); // Code valid for 15 minutes
            _context.Update(user);
            await _context.SaveChangesAsync();

            await _passwordResetService.SendPasswordResetCodeAsync(email, resetCode);

            //TempData["SuccessMessage"] = "Password reset code has been sent to your email.";
            return RedirectToAction("EnterResetCode");
        }

        public IActionResult EnterResetCode()
        {
            return View();
        }

        [HttpPost]
        public IActionResult EnterResetCode(string email, string resetCode)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.ResetToken == resetCode && u.ResetTokenExpiry > DateTime.UtcNow);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid or expired reset code.");
                return View();
            }

            return RedirectToAction("ResetPassword", new { email, token = resetCode });
        }

        public IActionResult ResetPassword(string email, string token)
        {
            // Pass email and token to the view via ViewData
            ViewData["Email"] = email;
            ViewData["Token"] = token;

            var model = new PasswordResetRequest();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(PasswordResetRequest model, string email, string token)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.ResetToken == token && u.ResetTokenExpiry > DateTime.UtcNow);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid or expired reset token.");
                return View(model);
            }

            // Hash the new password before saving
            user.Password = HashingServices.HashData(model.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            _context.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
