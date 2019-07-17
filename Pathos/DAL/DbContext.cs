using Microsoft.EntityFrameworkCore;
using Pathos.Models;

namespace Pathos.DAL {
    public class PathosContext : DbContext
    {
        public PathosContext(DbContextOptions<PathosContext> options) : base(options)
        {}

        public DbSet<User> Users { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Badge> Badges { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<UserBadges>()
                .HasKey(ub => new { ub.UserId, ub.BadgeId });

            builder.Entity<UserBadges>()
                .HasOne(ub => ub.User)
                .WithMany(u => u.Badges)
                .HasForeignKey(ub => ub.UserId);

            builder.Entity<UserBadges>()
                .HasOne(ub => ub.Badge)
                .WithMany(b => b.BadgeRecipients)
                .HasForeignKey(ub => ub.BadgeId);
        }
    }
}
