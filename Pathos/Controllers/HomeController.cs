using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pathos.Models;
using Pathos.Models.Settings;
using Microsoft.Extensions.Options;

namespace Pathos.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppSettings _settings;
        private readonly AppSecrets _secrets;

        public HomeController(IOptions<AppSettings> settings, IOptions<AppSecrets> secrets)
        {
            _settings = settings.Value;
            _secrets = secrets.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = $"Your super secret password for the {_settings.Environment} environemnt is {_secrets.SamplePassword}.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
