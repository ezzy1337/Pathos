# Taking Application Config from Tutorial to Production

## Introduction
Every app needs some sort of configuration. Most often I see config files being used to
distinguish a test or dev environment from the prod environment. Connection strings for
a database or credentials to an API are common things that change between a dev and prod
environment. App secrets like these need special consideration so they don't get checked
in to version control. This article shows you how to handle configuration and app secrets
in a conventional way without conditional logic and without checking secrets into version
control.

## The Nuts and Bolts
### Config Files
1. __appsettings.json__ is the base configuration file. Use this file for config values
that do not change between environments.
2. __appsettings.{env}.json__ are environment specific config files. Use these to store
the specific config values for your environment. Conventional values for ENV are 
`Development`, `Staging`, and `Production`.
3. __secrets.json__ is a file maintained for you by the `Secrets Manager` plugin. This
will be covered in more detail later.


## Selecting the environment specific config file
Dotnet Core uses the `DOTNETCORE_ENVIRONMENT` environment variable during application
start up to select a config file for this environment. The values from that enviroment
specific config file are merged into `appsettings.json` at runtime for you.

The following code is needed in `Startup.cs` to enable dynamic loading of the config
files at runtime.
```C#
public Startup(IConfiguration configuration, IHostingEnvironment env)
    var builder = new ConfigurationBuilder()
        .SetBasePath(env.ContentRootPath)
        // These next 2 lines are where the magic happens
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
        .AddEnvironmentVariables();
    Configuration = builder.Build();
```

The DOTNETCORE_ENVIRONMENT environment variable can be set in 2 places. The first and
preferred is simply as an environment variable on the system your application will be
running on. The second is in the `launchsettings.json` file found under the
`Properties` folder in the root of your application. I prefer this option for local
development. An example `launchsettings.json` file is shown below.
```JSON
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "iisSettings": {
    "windowsAuthentication": false, 
    "anonymousAuthentication": true, 
    "iisExpress": {
      "applicationUrl": "http://localhost:16853",
      "sslPort": 44335
    }
  },
  "profiles": {
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "launchUrl": "api/values",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "Pathos": {
      "commandName": "Project",
      "launchBrowser": true,
      "launchUrl": "api/values",
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

## Structuring Config Files
Most often the app settings will be used during startup to bootstrap services. In these
cases any settings can be added at the root level of the `appsettings.{env}.json` file.
A common case is the database connection string. Below is an example but since this is an
app secret that should not be stored in version control I'll show you a better way for
handling it later.
```
{
  "PathosConnectionString": "Data Source=Pathos.db"
}
```

Occasionally there is a need to make settings available after startup. This can be 
accomplished by configuring an object during startup with those values. A contrived
example for my [Coding Merit Badges project](https://docs.google.com/document/d/19xM74tFnGaxRqjSH-yxVsPDrpozsqojrKxd7_J7AVMU/edit)
is making the base url for the Github, Gitlab, and Bitbucket API's available to
controllers and any other service that may need them.

Here is an example of the `appsettings.{env}.json` file.
```
{
  "GitHostingApis": {
    "GitHubUrl": "https://api.github.com",
    "GitLabUrl": "https://gitlab.com/api",
    "BitBucketUrl": "https://api.bitbucket.org"
  }
}
```

Below is the class the above settings will be serialized to when Configure is called
during Startup.
```C#
namespace Pathos.Models
{
    public class GitHostingApis
    {
        /**
         * You can control which field from the JSON is used to populate the C# class
         * property by importing `Newtonsoft.Json` and adding the `[JsonProperty("<name>")]`
         * annotation to the target field.
         */
        public string GitHubUrl { get; set; }
        public string GitLabUrl { get; set; }
        public string BitBucketUrl { get; set; }
    }
}
```
_NOTE: I always put the classes my config values and secrets are serialized to in a
`Settings` folder under the `Models` folder._

## Configuring settings in Startup
In the `ConfigureServices` method of `Startup.cs` the following code is needed for each
settings object you want to reference in the application.
```C#
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc();

    services.Configure<GitHostingApis>(Configuration);

    // Alternatively you can select a specific section when configuring a service
    // I recommend doing it this way because it is more explicit.
    services.Configure<GitHostingApis>(Configuration.GetSection("GitHostingApis"));
}
```
_NOTE: Any fields not defined in the GitHostingApis class are ignored during serialization._

## Using Settings in a Controller
To use the configured GitHostingApis in a controller import `Microsoft.Extensions.Options`
and add an `IOptions<GitHostingApis>` argument to the controllers constructor. .NET
Core's dependency Injection framework will make sure the correct object is passed in.
I wrote a really simple controller which lists the available apis for git hosting services
to demonstrate how this works.
```C#
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
    }
}
```


You can test this change by sending a get request to `<host>/api/gitapis`.


# Application Secrets
There are some values you do not want stored in version control which means they can't
be in the config files discussed so far. To accomplish this added level of security
Microsoft provides the `Secret Manager` plugin.

## The Nuts and Bolts

1. Secrets can be shared between applications by using the UserSecretsId in the `*.csproj` file.
2. Secrets are stored unencrypted but separate from the project files.
   - Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
   - Mac/Linux: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

### Setting up the Secrets Manager

1. Add the following DotNetCli reference to the projects `.csproj` file.
```xml
<!--If there is already an ItemGroup with DotNetCliToolReference tags that just add the SecretManager.Tool tag to that ItemGroup.-->
<ItemGroup>
  <DotNetCliToolReference Include="Microsoft.Extensions.SecretManager.Tools" Version="2.0.0" />
</ItemGroup>
```
2. Install the secrets manager with `dotnet add package Microsoft.Extensions.SecretManager.Tools`
3. Generate a GUID and store it in the `.csproj` file.
```xml
 <PropertyGroup>
    <TargetFramework>netcoreapp2.0</TargetFramework>
    <!--The UserSecretsId is the node where your GUID should be stored-->
    <UserSecretsId>D5F689CA-B009-4944-BA86-D95D901354C0</UserSecretsId>
  </PropertyGroup>
```


### Setting secrets in Secret Manager
Secrets can be set using the `dotnet cli` as shown below.
```bash
dotnet user-secrets set "PathosConnectionString" "Data Source=Pathos.db"
```

### Sharing Secrets with the App
Just like configuration values from environment specific config files were merged into
the base configuration from `appsettings.json` so will any secrets stored in the Secret
Manager. Just like we did with config files we need to tell the framework to merge the
secrets at runtime. This can be achieved by adding `AddUserSecrets<Startup>()` to the
configuration builder pipeline.
```C#
var builder = new ConfigurationBuilder()
    .SetBasePath(env.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Startup>(); // Add this line
```

Now app secrets will be available from the Configuration API and can be used while
configuring services. To get at the secrets import `using Microsoft.Extensions.Configuration;` and call the Configuration API like so `Configuration["PathosConnectionString"]`.


### Using Secrets Throughout the App
Most often secrets will be used during startup but would not be needed later. There are some times when this isn't true, but these use cases escape me right now. You can just import Microsoft.Extensions.Configuration and call the Configuration directly, but that exposes all of your apps configurations and secrets to whatever component is calling it. I think its a best practice to follow the spirit of least privilege access. That means a component should only have access to the secrets it needs to get its job done.

Again just like the settings we can configure an object for each of the secrets we
want to use in the app. Lets say our app secrets are the database connection string and
client id and secret for the Acme API. It would be desirable to have a objects for the
connection string and another with the Acme API. The ConfigureServices method would looks like this.
```C#
public void ConfigureServices(IServiceCollection services)
{
  // Secret must be set as Db:<value> for this to work
  services.Configure<DbConnInfo>(Configuration.GetSection("Db"));
  // Secret must be set as AcmeApi:<value> for this to work
  services.Configure<AcmeApiCreds>(Configuration.GetSection("AcmeApi"));
}
```

For now lets just work with the AcmeApiCreds object since realistically the database
connection string would only be used to setup a DbContext. The class definition would
look like this.
```C#
namespace Pathos.Models.Config
{
  public class AcmeApiCreds
  {
    //Secret would be stored as AcmeApi:ClientId
    public string ClientId { get; set; }
    //Secret would be stored as AcmeApi:ClientSecret
    public string ClientSecret { get; set; }
  }
}
```

The AcmeApiCreds can then be injected into a controller the same way the GitHostingApis object was. The AcmeApi creds are then available like any other class property would be through the `_secret` variable.
```C#
public GitApisController(IOptions<GitHostingApis> hostingApis, IOptions<AppSecrets> secrets)
{
    _gitApis = hostingApis.Value;
    _secrets = secrets.Value;
}
```
