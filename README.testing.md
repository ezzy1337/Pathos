# Testing .NET Core Controllers

For me a well designed test suite is one of the most valuable parts of a code base.
Before I start troubleshooting a bug or extending a feature I look at the tests for that
feature. I also have found it helps new developers to a project make changes quicker and
with more confidence. I cannot overstate the importance of tests for any project whether
it be an enterprise application or open source library. All that said I consistently see
2 mistakes being made in api test suites. First is the over reliance on integration tests,
which are slow, and end up with complex setup and teardown logic so the tests run
independently. The second is using a custom base test class to share set up and tear down
logic. Most often the custom class is wrapping a base class provided by the testing
framework already. Sometimes these can be useful, but most often its a premature decision
to try and reuse code. All it really does is pidgeon hole tests and ties implementations
together causing a complex test framework.

This article will setup a balanced test suite leveraging unit and integration tests
that can be run as a full test suite or as separate test suites. It will also present
a workflow for creating specific and useful abstractions for testing that will be more
flexible and reusable than a base test class.

## Creating the unit test project
```bash
# Note execute this in the root directory so PathosTest appears
# next to Pathos
dotnet new xunit --name PathosTest
```

Add a reference to Pathos project in the PathosTest.csproj
```xml
  <ItemGroup>
    <ProjectReference Include="..\Pathos\Pathos.csproj" />
  </ItemGroup>
```

also add a reference to AspNetCore.All in .csproj file
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.All" />
</ItemGroup>
```

## Unit Tests
Unit tests are great when you want to skip all the framework magic, interaction with
external services (db, other API's etc.) and test the controllers business logic
directly. The best way to do this especially in .NET Core is dependency injection. Since
.NET Core was designed around supporting dependency injection isolating tests and
dependencies is pretty simple.

### Creating the first unit test
Each test needs to be marked with the `[Fact]` annotation otherwise .NET Core's testing
framework will not be able to discover it. Bypassing the middlewares provided by the
framework is as simple as constructing an instance of the controller to be tested and
providing the mocks for any dependencies.

Create a `HealthControllerTest.cs` file and add the following code and you'll have your
very first unit test.
```C#
using Xunit;

using Pathos.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace PathosTests.Controllers
{
    public class HealthControllerTests
    {
        [Fact]
        public void TestGetHealth()
        {
            var subject = new HealthController();

            var actual = subject.Get() as OkObjectResult;
            Assert.Equal(actual.Value, "healthy");
        }
    }
}

```

### Running Tests
use `dotnet test` to execute tests. When executed from the root directory for the test
project it will discover any function marked with the `[Fact]` annotation. A lot of other
test frameworks rely on naming conventions for test and fortunately that's not the case
with .NET Core.

## Creating Integration Tests
Integration tests are the next step up the testing pyramid and there lies the second
mistake I see in API test suite, a base test class all other tests inherit from. The
problem is there are so many middlewares like routing, authentication/authorization, etc.
that any class that tries to handle the setup for all of the different middlewares gets
bloated. Even when testing the scenario where every middleware, and integration does
exactly what it is supposed to do the test is more like an end-to-end test which is yet
another step up the testing pyramid. Good integration tests strike a balance between
using production like data and swapping out dependencies that are not the focus of the
test. For example to test a controller is behind the auth middleware the database doesn't
need to be seeded with test data, nor do actual queries need to be executed against a
database.

### Creating the First Integration Test
Microsoft recommends putting unit and integration tests in separate projects to ensure
infrastructure for testing components doesn't get included in the unit tests by accident.
Assuming the frameworks creators know the best way to use it we will go ahead and setup
another test project for these integration tests. Using the same command we did for the
unit test project create a new xunit test project for integration tests.
```bash
dotnet new xunit -n PathosIntegrationTests
```

You will need to add a project reference for the project being tested and a package
reference for the MVC Testing package before getting started with the test code.
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\Pathos\Pathos.csproj" />
</ItemGroup>
```

### Getting the test client
Importing the `Mvc.Testing` package allows you to create a custom test client that is
aware of the routes and middleware but also bypasses going out over the network. I
recommend starting integration tests by injecting a `WebApplicationFactory` into the test
class. The test class needs to implement the `IClassFixture` interface and be of type
`WebApplicationFactory<ProjectName.Startup>` where ProjectName is of course the project
name (for me this is `Pathos`). There is a way to share `WebApplicationFactory` objects
between tests but we will get to that later.
```C#
using Microsoft.AspNetCore.Mvc.Testing;

public class HealthControllerIntegrationTests : IClassFixture<WebApplicationFactory<Pathos.Startup>>
{
  private readonly WebApplicationFactory<Pathos.Startup> _factory;

  public HealthControllerIntegrationTests(WebApplicationFactory<Pathos.Startup> factory)
  {
      this._factory = factory;
  }
}
```

The factory is used in individual tests to create a customized test client. The simplest
use of the factory is calling `CreateClient()` without specifying any options which
returns a test client using all of the preconfigured middlewares and services defined in
the `ConfigureServices` method of `Startup.cs`.
```C#
[Fact]
public async void TestGetHealth()
{
  var subject = this._factory.CreateClient();
  var response = await subject.GetAsync("/api/health");

  Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

### Advanced Configuration of Test Client
In some cases you need to override the services being used in your application which
means you need to override the ConfigureServices method in `StartUp.cs`. This can be done
with a custom Application Factory. This is a great solution because each Application
Factory can be tailored for reuse in a specific test case. A common use case for me is a
factory that serves up an in memory database. The code below shows how to setup the
custom factory.
```C#
public class PathosDbApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where tStartup: class
  protected override void ConfigureWebHost(IWebHostBuilder builder) {
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
```

The trick is the `using` block that runs the database migrations. This is crucial
otherwise you will be getting errors about tables not existing.

When it comes to writing tests each test class that needs the databae will inherit
`IClassFixture<PathosDbApplicationFactory<Pathos.Startup>>` and then have an InMemory
database available for seeding data and will be automatically injected into controllers.
