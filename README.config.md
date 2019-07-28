# Taking Application Config from Tutorial to Production

## Introduction
Every app needs some sort of configuration. Most often I see configuration being used to
distinguish a test/dev environment from the production environment. Configuration values
for things like passwords connection strings, and logging levels often change between
each environment. Additionally sensitive configuration values like passwords need special
consideration so they stay secure. This post will discuss how to handle configuration and
application secrets in a conventional way that allows for easy switching between
environments without conditional logic in code and without checking sensitive
information into version control.

## The Nuts and Bolts
### Config Files
1. __appsettings.json__ is the base configuration file used for configuration that does
not change between environments.
2. __appsettings.{ENV}.json__ is the environment specific configuration file. Conventional
values for ENV are `Development`, `Staging`, and `Production`.
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

The DOTNETCORE_ENVIRONMENT environment variable can be set in 1 of 2 places. The first
and preferred is simply as an environment variable on the system your application will
be running on. The second is in the `launchsettings.json` file found under the
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

## Structuring the config file
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

Occassionally there is a need to make settings available after startup. This can be 
accomplished by configuring an object during startup with those values. A contrived
example for my GitHub Merit Badges project is making the base url for the Github, Gitlab,
and Bitbucket API's available to controllers and any other service that may need them.
Here is an example of the `appsettings.{env}.json` file.
```
{
  "GitHostingApis": {
    "GithubUrl": "https://api.github.com",
    "GitlabUrl": "https://gitlab.com/api",
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
and add an `IOptions<GitHostingApis>` argument to the controllers constructor. Dotnet
Core's dependency Injection framework will make sure the correct object is passed in.
To demonstrate how this works we will repurpose the `ValuesController` that was generated
for us by the `dotnet new webapi` command to instead show us the available Git hosting
services.
1. Rename the the file and class from `ValuesController` to `GitApisController`.
2. Remove the `Post`, `Put`, and `delete` methods.
3. Add a private readonly GitHostingApis object to the controller.
    ```C#
    private readonly GitHostingApis _gitApis;
    ```
4. Add a constructor with an IOptions argument.
    ```C#
    public GitApisController(IOptions<GitHostingApis> hostingApis)
    {
        _gitApis = hostingApis.Value;
    }
    ```
5. Replace the body of the `Get` method with the code below.
    ```C#
    return new string[] { _gitApis.GitHubUrl, _gitApis.GitLabUrl, _gitApis.BitBucketUrl };
    ```

You can test this change by sending a get request to `<host>/api/gitapis`.


# Application Secrets
There are some values you do not want stored in version control which means they can't
be in the config files discussed so far. To accomplish this added level of security
Microsoft provides the `Secret Manager` plugin.

## The Nuts and Bolts
1. Add the following DotNetCli reference to the projects `.csproj` file.
```xml
<!--If there is already an ItemGroup with DotNetCliToolReference tags that just add the SecretManager.Tool tag to that ItemGroup.-->
<ItemGroup>
  <DotNetCliToolReference Include="Microsoft.Extensions.SecretManager.Tools" Version="2.0.0" />
</ItemGroup>
```
2. Secrets are stored unencrypted but separate from the project files in
`%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json` for Windows
and  `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json` for Linux/Mac
3. Secrets can be shared between applications.

### Setting up Secrets Manager

1. Install the secrets manager with `dotnet add package Microsoft.Extensions.SecretManager.Tools`
2. Generate a GUID and store it in the `.csproj` file.
```xml
 <PropertyGroup>
    <TargetFramework>netcoreapp2.0</TargetFramework>
    <!--The UserSecretsId is the node where your GUID should be stored-->
    <UserSecretsId>D5F689CA-B009-4944-BA86-D95D901354C0</UserSecretsId>
  </PropertyGroup>
```
_NOTE:The value of UserSecretsId is what the Secret Manager uses in the file paths for
storing application secrets._

__Protip: Using the same UserSecretsId allows different applications to share secrets.__


### Setting secrets in Secret Manager
Secrets can be set using the `dotnet cli` as shown below.
```bash
dotnet user-secrets set "PathosConnectionString" "Data Source=Pathos.db"
```

### Accessing Secrets
In much the same way as configuration values secrets need to be serialized into an object
and injected into Controllers.

First the framework must be told to append the applications secrets to the application
settings on startup. To do this add the following to the Startup constructor in Startup.cs
```C#
var builder = new ConfigurationBuilder()
    .SetBasePath(env.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Startup>(); // Add this line
```

Next we need to create the class that secrets will be serialized to. Create a new file
`AppSecrets.cs` under `Models/Settings` if you wish to organize these files the same way,
but you can store them anywhere. I've included an example of this file below.
```C#
//Example AppSecrets.cs file
namespace Pathos.Models.Settings
{
    public class AppSecrets
    {
        public string PathosConnectionString { get; set; }
    }
}
```

Finally the Secrets need to be mapped from the Configuration API to the newly defined
AppSecrets class. This is done in the ConfigureServices method found in Startup.cs.
```C#
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc();

    services.Configure<GitHostingApis>(Configuration.GetSection("GitHostingApis"));
    services.Configure<AppSecrets>(Configuration);// Add this line
}
```

When using the secrets in a controller they need to be injected into the constructor the
same way the AppSettngs were.
```C#
public GitApisController(IOptions<GitHostingApis> hostingApis, IOptions<AppSecrets> secrets)
{
    _gitApis = hostingApis.Value;
    _secrets = secrets.Value;
}
```

For demonstration purposes only modify the `Get(int id)` to return the app secret. It
should go without saying you shouldn't do this in production.
```C#
// GET api/values/5
[HttpGet("{id}")]
public ActionResult<string> Get(int id)
{
    return _secrets.PathosConnectionString;
}
```

### Fine Grained Control of App Secrets (Optional)
If you want better control of where AppSecrets are visible you can define multiple
classes that group together secrets. For example you could have a class specifically
for your database connection details and a second for a third parties API credentials.
The only real difference is how these are extracted from the Configuration API. Here
is the code showing how you would use the specialized classes.
```C#
public void ConfigureServices(IServiceCollection services)
{
    // Secret must be set as Db:<value> for this to work
    services.Configure<DbConnInfo>(Configuration.GetSection("Db"));
    // Secret must be set as AcmeApi:<value> for this to work
    services.Configure<AcmeApiCreds>(Configuration.GetSection("AcmeApi"));
}
```
