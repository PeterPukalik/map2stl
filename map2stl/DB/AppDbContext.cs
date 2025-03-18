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
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MapModel>()
                .HasMany(m => m.Versions)
                .WithOne(m => m.Parent)
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        }


        public DbSet<User> Users { get; set; }
        public DbSet<MapModel> Models { get; set; }
    }
   
}
