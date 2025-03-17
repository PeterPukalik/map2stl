using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace map2stl.DB
{

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            Users = Set<User>();
            Models = Set<MapModel>();
            Details = Set<MapDetails>();
        }

        public DbSet<User> Users { get; set; }
        public DbSet<MapModel> Models { get; set; }
        public DbSet<MapDetails> Details { get; set; }
    }
   
}
