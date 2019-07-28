using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Pathos.Models.Config;

namespace Pathos.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GitApisController : ControllerBase
    {
        private readonly GitHostingApis _gitApis;

        private readonly AppSecrets _secrets;

        public GitApisController(IOptions<GitHostingApis> hostingApis, IOptions<AppSecrets> secrets)
        {
            _gitApis = hostingApis.Value;
            _secrets = secrets.Value;
        }

        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { _gitApis.GitHubUrl, _gitApis.GitLabUrl, _gitApis.BitBucketUrl };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return _secrets.PathosConnectionString;
        }
    }
}
