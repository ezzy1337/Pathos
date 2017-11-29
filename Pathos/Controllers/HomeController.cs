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

        public HomeController(IOptions<AppSettings> settings)
        {
            _settings = settings.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";
            ViewData["Environment"] = _settings.Environment;

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
