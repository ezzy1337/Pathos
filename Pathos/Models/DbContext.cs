using Microsoft.EntityFrameworkCore;
using Pathos.Models.Config;

namespace Pathos.Models {
    public class PathosContext : DbContext
    {
        public PathosContext(DbContextOptions<PathosContext> options) : base(options)
        {}

        public DbSet<User> Users { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Badge> Badges { get; set; }

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
    }
}
