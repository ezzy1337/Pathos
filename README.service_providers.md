# Implementing Service Providers
Before the days of dotnet core and a built in dependency injection container dependencies
were generally constructed in the controllers constructor. Fortunately with Dotnet Core
the entire framework was rebuilt from scratch allowing Microsoft to fundamentally change
the architecture to promote SOLID design principles. If you don't know what these
principles are you can read about them here. For Dotnet Core's service providers we are
interested in The 'D' of SOLID which stands for Dependency Inversion. The way Dotnet Core
handlers dependency inversion is with a dependency injection container built into the
framework. All one has to do is configure a service in the `ConfigureServices` method
found in `Startup.cs`. In this post I will cover setting up a service provider for
AutoMapper.

## Installing AutoMapper
The easiest way to install AutoMapper is through it's Nuget package. The following dotnet
cli command does this for you.
```bash
# Note the version may have changed since writing the tutorial
# check the nugest gallery to find the latest version.
dotnet add package AutoMapper --version 8.1.1
```

## Create a Configuration Profile
AutoMapper as described on it's site is a convention based object mapper. All a develper
needs to do is define a map from object A to Object B and then add the map to the mapper.
While doing time as a contractor I was introduced to this awesome way of setting up these
maps as a configuration profiles and I want to share that design now. The first thing to
add is a ConfigurationProfile class. In MVC apps I generally define this class in a file
called `ConfigurationProfiles.cs` found in the `Models/Mappings` folder. The following
code sets up a simple mapping profile from dotnet core's built in Configuration object
to AppSecrets (a class I setup for dealing with passwords and other secrets).
```C#
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Pathos.Models.Settings;

namespace Pathos.Models.Mappings
{
    public class ConfigurationProfile : Profile
    {
        public ConfigurationProfile()
        {
            CreateMap<IConfiguration, AppSecrets>()
                .ForMember(o => o.PathosConnectionString, opt => opt.MapFrom(src => src["PathosConnectionString"]));
        }
    }
}
```

Each new mapping should get it's own profile i.e. class. A common use for this is mapping
a entity used by EF Core to a View Model. For example UserEntityToUserViewModelProfile
might map all the properties from the entity excluding password so it is not visible to
users.

## Creating the mapping engine
The maps alone don't do anything. They are just the instructions for how to map an object.
We need a Mapper or Engine as I like to call it to process the maps. Create a new file,
`Engine.cs`, under `Models/Mappings` and add the following code to it.
```C#
using AutoMapper;

namespace Pathos.Models.Mappings
{
    public static class Engine
    {
        public static IMapper Mapper { get; internal set; }

        static Engine()
        {
            Mapper = new MapperConfiguration(
                cfg => {
                    cfg.AddProfile<ConfigurationProfile>();
                }
            ).CreateMapper();
        }
    }
}
```

The last step is to register the mapper as a service on app Startup. This is done by
adding the following line of code to `ConfigureServices` in `StartUp.cs`.
```C#
services.AddSingleton(Engine.Mapper);
```
