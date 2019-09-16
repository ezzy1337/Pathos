using Xunit;

using Pathos.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace PathosTests.Controllers
{
    public class HealthControllerTest
    {
        [Fact]
        public void TestGetHealth()
        {
            var subject = new HealthController(null);

            var actual = subject.Get() as OkObjectResult;
            Assert.Equal(actual.Value, "healthy");
        }
    }
}
