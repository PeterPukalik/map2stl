using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace map2stl
{

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            Users = Set<User>();
            Models = Set<MapModel>();
        }

        public DbSet<User> Users { get; set; }
        public DbSet<MapModel> Models { get; set; }
    }
   
}
