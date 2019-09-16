using Xunit;

using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

using Pathos;

namespace PathosIntegrationTests.Controllers
{
    public class HealthControllerIntegrationTest : IClassFixture<PathosDbApplicationFactory<Pathos.Startup>>
    {
        private readonly PathosDbApplicationFactory<Pathos.Startup> _factory;

        public HealthControllerIntegrationTest(PathosDbApplicationFactory<Pathos.Startup> factory)
        {
            this._factory = factory;
        }

        [Fact]
        public async void TestGetHealth()
        {
            // Arrange
            var subject = this._factory.CreateClient();

            // Act
            var response = await subject.GetAsync("/api/health/db");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
