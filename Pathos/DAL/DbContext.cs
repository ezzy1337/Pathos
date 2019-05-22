using Microsoft.EntityFrameworkCore;
using Pathos.Models;

namespace Pathos.DAL {
    public class PathosContext : DbContext
    {
        public PathosContext(DbContextOptions<PathosContext> options) : base(options)
        {}
    }
}
