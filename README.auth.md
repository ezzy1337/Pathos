# Authentication and Authorization
Authentication (who the user is) and Authorization (what the user is allowed to do) are
critical components for any public facing api. These concepts are deceptively simple and
in reality very complex to implement. For brevity of this guide I chose to use Auth0 for
the applications authentication and authorization middleware. Check out
[Auth0's site](https://auth0.com/) for more information of their services.


## Configuring Auth0 service
Ok yea you need an account with Auth0 to complete this tutorial but it's free, and simple
to sign up for. This guide isn't about Auth0 so I'll just breeze through this setup stuff
since I'd rather focus on the code you will be adding to your app.

1. Sign into yout Auth0 portal
2. Select the `APIs` option on the left side navigation menu
3. Click on `Create API` button in the top left corner.
4. Fill out the form. see the example below
    - Name: Pathos 
    - Identifier: https://pathos/
    - Signing Algorithm: RS256

    _NOTE: Identifier cannot be changed and is used in the application middleware as the
    audience._
5. Click `Create`

We will worry about specific permissions and scopes later when we get to that use case.


## Configuring the App to use Auth0
Here is where the bulk of the work happens. There are 3 steps to fully configuring the
app to use Auth0. Here is the outline of what we will be doing.
 - Add Auth0 config values as App Secrets
 - Setup authentication services
 - Setup authorization services

### Add Auth0 config values as App Secrets
_NOTE: It is assumed you have already setup the app config and secret manager as
discussed previosuly. You can see how it was done [here](./README.config_and_secrets.md).
_

Using the dotnet cli set the Auth0 config values that you used in the previous step when
creating the Api in Auth0's management portal.
```bash
# This is the tenant id assigned when you created the Auth0 account
dotnet user-secrets set Auth0:domain <your-auht0-tenant>
# This is the Identifier setup earlier that will be used as the audience
dotnet user-secrets set Auth0:Identifier <your-apis-identifier>
```

### Setup authentication services
The following code registers the authentication services with the app.
```C#
// Add to ConfigureServices method in Startup.cs
services.AddAuthentication(opts => {
    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(opts => {
    opts.Authority = $"https://{Configuration["Auth0:domain"]}/";
    opts.Audience = Configuration["Auth0:Identifier"];
});
```

Although the services are registered they will not be used until `useAuthentication` is
called in the middleware pipeline.
```C#
// Add to the Configure method in Startup.cs
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.UseAuthentication(); // Add this line

    // some code omitted for brevity

    app.UseMvc(routes =>
    {
        routes.MapRoute(
            name: "default",
            template: "{controller=Home}/{action=Index}/{id?}");
    });
```
_NOTE: I always add useAuthentication to the very top of this method because it is
logically the first thing that happens in the pipeline. As long as it is called before
UseMvc everything will work._


### Setup Authorization services
Auth0 supports Policy-Based Authorization which uses scopes (permissions basically) to
limited user actions. Start by adding the `HasScopeRequirement` and `ScopeHandler`
classes shown below.
```C#
// HasScopeRequirements.cs

/**
 * Enforces the JWT has a scope claim
 */
public class HasScopeRequirement : IAuthorizationRequirement
{
    public string Issuer { get; }
    public string Scope { get; }

    public HasScopeRequirement(string scope, string issuer)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
    }
}


// ScopeHandler.cs

/**
 * Handles parsing scope claims from the JWT and verifying the required scope is included
 * in the claims from the JWT
 */
public class ScopeHandler : AuthorizationHandler<HasScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasScopeRequirement requirement)
    {
        if (!context.User.HasClaim(c => c.Type == "scope" && c.Issuer == requirement.Issuer))
            return Task.CompletedTask;

        var scopes = context.User.FindFirst(c => c.Type == "scope" && c.Issuer == requirement.Issuer).Value.Split(' ');

        if (scopes.Any(s => s == requirement.Scope))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
```

Just like the authentication services the authorization services need to be registered
with the app. Add the following code to the same ConfigureServices method in Startup.cs.
```C#
services.AddAuthorization(options =>
{
    options.AddPolicy("read:messages", policy => policy.Requirements.Add(new HasScopeRequirement("read:messages", domain)));
});

services.AddSingleton<IAuthorizationHandler, ScopeHandler>();
```

### Securing Endpoints
Now that the middlewares are in place it is as simple as adding the `[Authorize]` data
annotation above a controller action and it is behind the authentication middleware. To
add authorization the data annotation changes to `[Authorize(<permission-name>)]` where
`<permission-name>` gets replaced by with the name of a scope setup in Auth0. Since the
only endpoint currently setup is the health endpoint we will create a `health:check`
scope to demonstrate how authorization works.

#### Create the scope in Auth0
Log back into your Auth0 account, navigate to your Api and select the `Permissions` tab.
Add `health:check` as the permission name and something along the lines of `Allows a user
to run the apps health check` as the description.

#### Adding authorization to health endpoint
As mentioned earlier it's now as simple as adding the permission name to the [Authorize]
annotation as shown below.
```C#
[Authorize("health:check")]
public IAction Health() {
    return Ok({"status": "healthy"})
}
```
