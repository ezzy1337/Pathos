# Entity Framework Core 2.0	(EF Core)
For the rest of this tutorial we are assuming you will be using EF Core 2.0. That being
said in February 2019 EF Core 3.0 was announced. If you are not using EF Core 3.0 you
are missing out on smarter LINQ-to-SQL, and the ability to query DB views. 

<br />

# What is Entity Core though?
EF Core is an Object Relational Mapping (ORM) technology designed by Microsoft to be the
ultimate Data Access Technology for any .NET Core app. Currently there is Entity
Framework 6 (EF 6) which is used in the old .NET Frameworks. Worry not though EF 6 code
can run on EF Core 3.0. Microsoft maintains a
[feature comparison](https://docs.microsoft.com/en-us/ef/efcore-and-ef6/) document for
features from EF 6 to EF Core 3.0.

<br />

# Concepts
## Design Pattern
Most ORM’s use a design pattern called the Active Record Pattern. The important thing to
note about the active record pattern is that any given instance 
of an entity corresponds to a row/record in the DB.

For Example the object `me` which is an instance of the `User` class correlates to row 1
from the Database.
```C#
Public class User {
  public int ID { get; set; }
  public string Name { get; set; }
  public DateTime DOB { get; set; }
}

me = User();
me.ID = 1337;
me.Name = "John Jameson"
me.DOB = new DateTime(1959, 2, 3);
```

| row | UserID | Name | DOB |
| --- | ------ | ---- | --- |
| 1 | 1337 | John Jameson | 1959-02-03 |
| 2 | 1347 | Tester Testofferson | 1936-06-22 |

## Migrations
One of the most useful features of an ORM is built in handling of database schema changes.
These are often referred to as `Migrations`. In fact in EF Core that's the command you
will use (but more on that later) In EF 6 there are 2 ways to handle migrations Database
First which is not supported in EF Core (yet) and Code First Migrations which are
supported and what we will be using in this tutorial.

## Default Naming Convention
In order to work it's magic EF Core assumes a few naming conventions unless explicitly
changed using a Data Annotation. I think there are 3 important conventions to cover;
  - [Table Names](https://docs.microsoft.com/en-us/ef/core/modeling/relational/tables#conventions)
    ```C#
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("users")]
    public class User { ... } // will generate a table named users

    public class User { ... } // Will generate a table named User
    ```
    _NOTE: I consider it a best practice to use the data annotation for table names
    because it is more explicit and decouples your entity names from your table names._

  - [Primary Keys](https://docs.microsoft.com/en-us/ef/core/modeling/relational/primary-keys#conventions)
    ```C#
    // EF Core assumes the Primary Key is named Id or <entity>Id
    public class User {
        public int Id { get; set; }
    }

    public class User {
        public int UserId { get; set; }
    }
    ```
    _NOTE: I consider it a best practice to use the convention \<entity>Id because it is
    consistent with the convention used for Foreign Keys_

  - [Foreign Key Constraints](https://docs.microsoft.com/en-us/ef/core/modeling/relational/fk-constraints#conventions)
    ```C#
    public class User {
        public int UserId { get; set; }
    }

    // EF Core assumes the Foreign Key is named <foreign-entity>Id
    public class Address {
        public int AddressId { get; set; }  // Address PK

        public int UserId { get; set; }  // FK to user record
        public User User { get; set; }
    }
    ```
    _NOTE: The `User` property of Address is called a Reference Navigation Property and
    is used by EF Core to serialize the foreign rows into a single C# Object._


# Setting up the Data Access Layer (DAL)
EF Core uses something called a Db Context to coordinate reading and writing data from
the database, deserializing it into Plain Old C# Objects (POCOs) and serializing it into
the format expected by your database. Creating the context can happen after you create
your data model and entities, but I like to set it up first.

### Create Db Context
Create a new Folder called `DAL` at the project root directory (i.e. Pathos). Under the
`DAL` folder create a new file called `DbContext.cs`, and add the following code.
```C#
using Microsoft.EntityFrameworkCore;
using Pathos.Models;

namespace Pathos.DAL {
    public class PathosContext : DbContext
    {
        public PathosContext(DbContextOptions<PathosContext> options) : base(options)
        {}
    }
}
```

### Register Db Context at Startup
The Db Context needs to be registered as a service in the `ConfigureServices` method
defined in the `Startup.cs` file. Import `Microsoft.EntityFrameworkCore` in Startup.cs
and add the following code to the end of the `ConfigureServices` method.

_NOTE: If you haven't read the [post](README.config_and_secrets.md)
on App Config and Secrets you might want to do that now._

```C#
services.AddDbContext<PathosContext>(
    options => options.UseSqlite(Configuration["PathosConnectionString"])
);
```

### Set the database connection string to app secrets
If you have not already set the connection string in the Secret Manager do so now with
the following cli command.
```bash
dotnet user-secrets set "PathosConnectionString" "Data Source=Pathos.db"
```

Finally all the boilerplate stuff is out of the way and we can start creating the data\
model.

# Schema Modeling
_NOTE: It will be helpful for you to review the [EF Core relationship terminology](https://docs.microsoft.com/en-us/ef/core/modeling/relationships#definition-of-terms)
provided by microsoft._

There are 3 entities that need to be modeled for the Merit Badge project; `Users`,
`Badges` and `Accounts` (i.e. GitHub, Gitlab etc.). Technically there is a fourth,
`UserBadges`, but we'll get to that when we talk more indepth about many-to-many
relationships. The most important relationships for these entites are outlined below.

 - A `User` can have 1 or many `Accounts`
 - An `Account` can belong to only 1 `User`
 - A `User` can have 0 to many `Badges`
 - A `Badge` can belong to 0 or many `Users`
 - A `User` can have exactly 0 or 1 of any specifc `Badge`

__PROTIP:__
It's really easy to get caught up in the details when creating the initial migration.
While learning I recommend a more iterative approach. First define the entities and their
one-to-one relationships, then go back and add the more complex relationships later. You
will still most likely create a single migration for all of the relationships but they
can be added to the models and DbContext piece by piece .


### Data Model 1st Iteration (Entities)
Create files for each of the entities defined below under the Models folder. Remember an
entity maps to a table and a property of that entity maps to a column in the table.
```C#
// UserModel.cs
[Table("users")]
public class User {
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}

// AccountModel.cs
[Table("accounts")]
public class Account {
    public int AccountId { get; set; }
    public string Username { get; set; }
    public string ProfileUrl { get; set; }
}

// BadgeModel.cs
[Table("badges")]
public class Badge
{
    public int BadgeId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}
```

Once the entities are defined they can be added to the DbContext as DbSet objects.
```C#
public class PathosContext : DbContext
  {
      public PathosContext(DbContextOptions<PathosContext> options) : base(options)
      {}

      public DbSet<User> Users { get; set; }
      public DbSet<Account> Accounts { get; set; }
      public DbSet<Badge> Badges { get; set; }
  }
```

### Data Model 2nd Iteration (Relationships)
#### One-To-Many Relationships
The easiest relationships to start with are the one-to-many relationships. Specifically
the following schema requirements can be expressed as one-to-many relationships.
 - A `User` can have 1 or many `Accounts`
 - An `Account` can belong to only 1 `User`

To establish a one-to-many relationship the principal entity (`User`) needs
a `Collection Navigation Property` to `Accounts`. The dependant entity (`Account`) needs
a `Inverse Navigation Property`. Update the User class and Account class as shown below.
```C#
[Table("users")]
public class User {
  public int UserId { get; set; }
  public string Username { get; set; }
  public string Password { get; set; }

  // Accounts is the Reference Navigation Property
  public List<Account> Accounts { get; set; }
}

[Table("accounts")]
public class Account {
  public int AccountId { get; set; }
  public string Username { get; set; }
  public string ProfileUrl { get; set; }

  //UserId is the Foreign Key to User.Id
  public int UserId { get; set; }
  //User is the Inverse Navigation Property
  public User User { get; set; }
}
```
_NOTE: All it takes is for EF Core to build a relationship by convention is the Reference
Navigation Property in the Principal Entity, however it is recommended to define the
relationship with the Foreign Key in the dependent entity._

__WARNING: When there are multiple navigation properties between entities the relationships
must be build manually. More on this later.__

For a cleaner data annotation driven approach there is the `ForeignKey` and `InverseProperty`
annotations that can be used for one-to-many relationships that you can read about [here](https://docs.microsoft.com/en-us/ef/core/modeling/relationships#data-annotations).


#### Many-To-Many Relationships
EF Core handles many-to-many relationship by creating two one-to-many relationships with
a join table. The many-to-many relationship described by the following schema
requirements will use a join table called `user_badges` and a join entity `UserBadges`, to
represent that table.
 - A `User` can have 0 to many `Badges`
 - A `Badge` can belong to 0 or many `Users`

Create the following class in a `UserBadgesModel.cs` file
under the `Models` folder.
```C#
[Table("user_badges")] //creates the join table as user_badges
public class UserBadges { // I refer to this as a join entity but truthfully it's just another entity
  //Foreign Key to User table
  public int UserId { get; set; }
  //Reference Navigation Property to User
  public User User { get; set; }

  //FK to Badge table
  public int BadgeId { get; set; }
  //Reference Navigation Property to Badge
  public Badge Badge { get; set; }
}
```

The `User` and `Badge` classes get linked to the `UserBadges` model the same as the
one-to-many relationships.
```C#
[Table("users")]
public class User {
  public int UserId { get; set; }
  public string Username { get; set; }
  public string Password { get; set; }

  public List<Account> Accounts { get; set; }

  //Collection Navigation Property to UserBadges join entity
  public List<UserBadges> Badges { get; set; }
}

public class Badge
{
  public int BadgeId { get; set; }
  public string Name { get; set; }
  public string Description { get; set; }

  //Collection Navigation Property to UserBadges join entity
  public List<UserBadges> BadgeRecipients { get; set; }
}
```

Now recall the warning that multiple Navigation Properties must be mapped manually? Well
`UserBadges` is just a case where the relationships have to be mapped manually. To do
this the FluentAPI can be used in the DbContext. Add the `onModelCreating` method to the
PathosContext class in `DAL/DbContext.cs`.
```C#
protected override void OnModelCreating(ModelBuilder builder)
{
  //Creates primary key for the join table
  builder.Entity<UserBadges>()
      .HasKey(ub => new { ub.UserId, ub.BadgeId });

  builder.Entity<UserBadges>()
      .HasOne(ub => ub.User) //Each User
      .WithMany(u => u.Badges) // Has many badges
      .HasForeignKey(ub => ub.UserId); // with a foreignkey of UserId

  builder.Entity<UserBadges>()
      .HasOne(ub => ub.Badge) //Each Badge
      .WithMany(b => b.BadgeRecipients) // has many recipients
      .HasForeignKey(ub => ub.BadgeId); // with a foreign key of BadgeId
}
```

This configuration also handles the last schema requirement limiting each user to having
exactly 0 or 1 of any given badge. Since the primary key for the `user_badges` table is
comprised of UserId, and BadgeId and a primary key mut be unique then only one entry will
exist in this table for a user and a specific badge while still allowing multiple users to
have the same badge.

### Initial Migration
Now that the entities and relationships have been defined the 2 step process of creating
and applying migrations can be started.
```bash
dotnet ef migrations add InitialSchema # Create the initial migration titled InitialSchema
dotnet ef database update # Applies the update to the database
```
__WARNING:__
If you get the error `No executable found matching command "dotnet-ef"` add the following to your .csproj file
```xml
<!--Enables dotnet core cli tools-->
<ItemGroup>
  <DotNetCliToolReference Include="Microsoft.EntityFrameworkCore.Tools.DotNet" Version="2.0.0" />
</ItemGroup>
```

When the migration has successfully been added a new `Migrations` folder will appear
under the `Pathos` directory. After successfully updating the database a new file
`Pathos.db` appears also under the `Pathos` directory. This is a local instance of your
database, but it is exluded from version control in the `.gitignore` since you should
never commit your database to version control.

### Exploring the Migrations Folder
At this time when you expand the Migrations folder you will see 3 files
`PathosContextModelSnapshot`, and 2 that start with timestamps.
 - `<timestamp>_InitialSchema.cs`: The main migration file that defines an `Up` method
   which contains the migration code for updating the database, and a `Down` method which
   contains the code for rollingback the migration.
 - `<timestamp>_InitialSchema.Designer.cs`: A metadata file providing infor for EF Core.
 - `PathosContextModelSnapshot.cs`: A snapshot of what the database looks like some changes
   can be determined between migrations.
