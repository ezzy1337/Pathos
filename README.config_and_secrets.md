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
values for ENV are `development`, `staging`, and `production`.
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
and preferred is simplay as an environment variable on the system your application will
be running on. The second is in the `launchsettings.json` file found under the `Properties` folder in the root of your application. I prefer this option for local
development. An example `launchsettings.json` file is shown below.
```JSON
{
  "profiles": {
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
}
```
_NOTE: In this example IIS Express is the Profile Name. This can be changed and you can specify multiple profiles each with it's own environment variables so it's easy to debug with different configurations._

## Serializing JSON to C# Object
When the config values are read in they get serialized to an Plain Old C# Object (POCO).
The following code defines an AppSettings class for that purpose.

_NOTE: I usually create a `Settings` folder under the `Models` folder for storing App Settings/Secrets._
```C#
/*
 * {
 *    "AppSettings": {
 *        "Environment": "development"
 *    }
 * }
 */
public class AppSettings
{
    public string Environment { get; set; }
}
```
_NOTE: You can control which field from the JSON is used to populate the C# class property by importing `Newtonsoft.Json` and adding the `[JsonProperty("<name>")]`
annotation to the target field._

## Injecting Settings into the Application
In the `ConfigureServices` method of `Startup.cs` the following code is needed for each
settings object you want to reference in the application.
```C#
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc();

    services.Configure<AppSettings>(Configuration);

    // Alternatively you can select a specific section when configuring a service
    // I recommend doing it this way because it is more explicit.
    services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
}
```
_NOTE: Any fields not defined in the AppSettings class are ignored during serialization._

## Using AppSettings in a Controller
In any Controller that you want to use the AppSettings you will need to import
`Microsoft.Extensions.Options` then define IOptions<AppSettings> as a parameter for the
Controllers constructor as shown.
```C#
using Microsoft.Extensions.Options;

private readonly AppSettings _settings;

public HomeController(IOptions<AppSettings> settings)
{
  this._settings = settings.Value; 
}
```

The settings can then be exposed in a View through the ViewData.
```C#
# Add the value for the Key setting to the ViewData object.
public IActionResult About()
{
    ViewData["Message"] = $"Your currently running in the {_settings.Environment} environment.";

    return View();
}
```

# Application Secrets
There are some values you do not want stored in version control which means they can't be in the usual config files discussed so far. To accomplish this added level of security
Microsoft provides the `Secret Manager` plugin for Dotnet Core.

## The Nuts and Bolts
1. Add the following DotNetCli reference to the projects `.csproj` file.
```xml
<ItemGroup>
  <DotNetCliToolReference Include="Microsoft.Extensions.SecretManager.Tools" Version="2.0.0" />
<ItemGroup>
```
2. Secrets are stored unencrypted but separate from the project files in the following
directories Windows=`%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
Mac/Linux=`~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`
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


### Accessing Secrets
In much the same way as configuration values secrets need to be serialized into an object
and injected into Controllers.

First we must let the framework know to append the applications secrets to the application
settings on startup. To do this add the following to the Startup constructor in Startup.cs
```C#
var builder = new ConfigurationBuilder()
    .SetBasePath(env.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Startup>(); // Add this line
```

Next we need to setup the class that secrets will be serialized to. I like to keep this
file with the `AppSettings.cs` file created earlier. Create a new file `AppSecrets.cs`
under `Models/Settings` if you wish to organize these files the same way, but you can
store them anywhere. I've included an example of this file below.
```C#
//Example AppSecrets.cs file
namespace Pathos.Models.Settings
{
    public class AppSecrets
    {
        public string SamplePassword { get; set; }
    }
}
```

Finally the Secrets need to be mapped from the Configuration API to the newly defined
AppSecrets class. This is done in the ConfigureServices method found in Startup.cs.
```C#
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc();

    IConfiguration appSettings = Configuration.GetSection("AppSettings");
    services.Configure<AppSettings>(appSettings);

    services.Configure<AppSecrets>(Configuration);// Add this line
}
```

When using the secrets in a controller they need to be injected into the constructor the
same way the AppSettngs were.
```C#
public HomeController(IOptions<AppSettings> settings, IOptions<AppSecrets> secrets)
{
    this._settings = settings.Value;
    this._secrets = secrets.Value;
}

// Not that the secrets have been injected they are used the same as AppSettings
public IActionResult About()
{
    ViewData["Message"] = $"Your super secret password for the {_settings.Environment} environment is {_secrets.SamplePassword}.";

    return View();
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
