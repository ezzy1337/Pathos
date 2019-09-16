using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pathos.Models;
using Microsoft.Extensions.DependencyInjection;

namespace PathosIntegrationTests
{
    public class PathosDbApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup: class 
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var dbOptions = new DbContextOptionsBuilder<PathosContext>();
            dbOptions.UseSqlite("DataSource=PathosTests.db");
            using (var context = new PathosContext(dbOptions.Options))
            {
                context.Database.Migrate();
            }

            builder.ConfigureServices(services => {
                services.AddDbContext<PathosContext>(
                   options => options.UseSqlite("DataSource=PathosTests.db")
                );
            });
        }
    }
}
