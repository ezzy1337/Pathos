using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pathos.Models;

namespace Pathos.Controllers
{
    public class HealthController: Controller
    {
        // private readonly PathosContext _db;
        // public HealthController(PathosContext db)
        // {
        //     this._db = db;
        // }

        [Route("api/[controller]")]
        [HttpGet]
        public IActionResult Get() {
            return Ok("healthy");
        }

        // [Route("api/[controller]/db")]
        // [HttpGet]
        // async public Task<IActionResult> GetDbHealth() {
        //     try {
        //         var result = await this._db.Users.CountAsync();
        //         return Ok("healthy");
        //     } catch (Exception) {
        //         return StatusCode(500);
        //     }
        // }
    }
}
