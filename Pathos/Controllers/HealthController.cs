using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Pathos.Controllers
{
    public class HealthController: Controller
    {
        [Authorize]
        [HttpGet]
        public IActionResult Index() {
            return Ok("healthy");
        }
    }
}
