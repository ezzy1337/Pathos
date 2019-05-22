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
    }
}
