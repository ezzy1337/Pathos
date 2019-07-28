# Pathos

Pathos is a Greek word which means suffering, experience or emotion. It is also one of
the 3 modes of persuasion in which one appeals to their audiences emotions. If you have
ever tried to take something you've learned in a coding tutorial and apply it in a real
world scenario you know the suffering and experience I'm talking about. I got tired of
tutorials leaving out the details about managing multiple environments so I wrote "The
12 Labours of a .NET Developer" to bridge the gap from tutorial to production. I found
the 12 Labours of Hercules a fitting analogy to this struggle since Hercules' labours
made him the perfect embodiment of the Greek's idea of Pathos and therefore named the
project Pathos.

Before reading these guides make sure you have read the Getting Started section below.
1. Environment specific config files and Secrets Manager
2. Unit Testing Controllers and React Views.
3. Setting up Authentication and Authorization with Auth0
4. Handling Entity Framework Migrations in deployment
4. Adding a React.js Frontend
5. Deploying service to Azure

## The Project
Every tutorial for .NET MVC apps is always for blogs, or a classroom or something that
conceptually everyone can grasp but is kind of boring. This tutorial will walk you
through building the management app for a project I've wanted to implement for a really
long time, GitHub Merit Badges. If you want to know more about the project here is a 
[breif description](https://docs.google.com/document/d/19xM74tFnGaxRqjSH-yxVsPDrpozsqojrKxd7_J7AVMU/edit)
I wrote up for a Hack-a-thon. Unfortunately the code for detecting, and assigning merit
badges has not been started yet. That being said I still need a way to manage the users,
badges, and scoreboard as well as provide a way for people to view a users badges.

## Getting Started
### .NET Core SDK and Runtime
Below is the output from running `dotnet --info` on my system at the time of publishing
this project. To avoid inconsistencies in the write up I suggest installing the.NET Core
SDK version (`2.2.4`) and runtime version (`2.2.6`) shown below. If multiple SDKs are
installed the dotnet core cli always uses the latest regardless of what has been targeted
by the app in the *.csproj file.
```bash
.NET Core SDK (reflecting any global.json):
 Version:   2.2.401
 Commit:    729b316c13

Runtime Environment:
 OS Name:     Mac OS X
 OS Version:  10.13
 OS Platform: Darwin
 RID:         osx.10.13-x64
 Base Path:   /usr/local/share/dotnet/sdk/2.2.401/

Host (useful for support):
  Version: 2.2.6
  Commit:  7dac9b1b51

.NET Core SDKs installed:
  2.1.3 [/usr/local/share/dotnet/sdk]
  2.1.4 [/usr/local/share/dotnet/sdk]
  2.2.401 [/usr/local/share/dotnet/sdk]

.NET Core runtimes installed:
  Microsoft.AspNetCore.All 2.2.6 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.All]
  Microsoft.AspNetCore.App 2.2.6 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.NETCore.App 2.0.4 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.0.5 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.2.6 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]

To install additional .NET Core runtimes or SDKs:
  https://aka.ms/dotnet-download
```

### Generate a new WebApi Project
```bash
dotnet new webapi --name <name> # I used Pathos
```

## Exclude dotnet core artifacts from version control
Add the following `.gitignore ` file to the project. Additionally add the following to
the `.gitignore` file to exclude local entity framework artifacts.
```
# Ignore Local Database for Entity Core
*.db
```
