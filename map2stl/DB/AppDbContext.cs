using map2stl.DB;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        Users = Set<User>();
        Models = Set<MapModel>();
        ModelShares = Set<MapModelShare>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Self-referencing versioning
        modelBuilder.Entity<MapModel>()
            .HasMany(m => m.Versions)
            .WithOne(m => m.Parent)
            .HasForeignKey(m => m.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optionally configure MapModelShare relationships if needed
        // e.g. Cascade delete or restrict
        modelBuilder.Entity<MapModelShare>()
            .HasOne(s => s.Model)
            .WithMany() // or with a new navigation property if you prefer
            .HasForeignKey(s => s.MapModelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MapModelShare>()
            .HasOne(s => s.User)
            .WithMany() // or a new navigation property on User
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public DbSet<User> Users { get; set; }
    public DbSet<MapModel> Models { get; set; }
    public DbSet<MapModelShare> ModelShares { get; set; }  
}
