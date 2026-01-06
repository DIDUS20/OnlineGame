using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Potatotype.Services;
using Potatotype.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Potatotype.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AuthService _auth = new();
        private readonly SaveService _score = new();

        public HomeController(ILogger<HomeController> logger) => _logger = logger;

        // GET: /Home/Login
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Missing credentials";
                return View("Login");
            }

            // Przekazujemy surowe has³o — AuthService zajmuje siê hash/verify
            var token = await _auth.Login(username, password);

            if (token == null)
            {
                ViewBag.Error = "Invalid username or password";
                return View("Login");
            }

            Response.Cookies.Append("session", token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = Request.IsHttps, // lokalnie nie wymusza HTTPS
                Expires = DateTimeOffset.UtcNow.AddHours(24)
            });

            return RedirectToAction("Game");
        }

        // GET: /Home/SignUp
        [HttpGet]
        public IActionResult SignUp() => View();

        [HttpPost]
        public async Task<IActionResult> SignUp(string username, string password, string confirm_password)
        { 
            if (!string.Equals(password, confirm_password))
            {
                ViewBag.Error = "Passwords do not match";
                return View("SignUp");
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Missing credentials";
                return View("SignUp");
            }

            // Przekazujemy surowe has³o — AuthService zrobi hash
            var success = await _auth.Register(username, password);

            if (!success)
            {
                ViewBag.Error = "User already exists";
                return View("SignUp");
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Game()
        {
            if (!Request.Cookies.TryGetValue("session", out var token))
                return RedirectToAction("Login");

            var username = await _auth.GetUserFromToken(token);
            if (username == null)
                return RedirectToAction("Login");

            return View(model: username);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            if (Request.Cookies.TryGetValue("session", out var token))
            {
                await _auth.Logout(token);
                Response.Cookies.Delete("session");
            }

            return RedirectToAction("Login");
        }

        public IActionResult Index()
        {
            return View(_score.GetTop10());
        }
        public IActionResult Privacy() => View();
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
