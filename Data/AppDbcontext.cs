using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Address> Addresses => Set<Address>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.RoleName)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId);

            modelBuilder.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            modelBuilder.Entity<Category>().HasIndex(c => c.CategoryId).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(p => p.ProductId).IsUnique();

            modelBuilder.Entity<Category>(e =>
            {
                e.HasKey(c => c.CategoryId);
            });

            modelBuilder.Entity<Product>(e =>
            {
                e.HasKey(p => p.ProductId);
                e.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<Cart>(e =>
            {
                e.HasKey(c => c.CartId);
                e.HasIndex(c => c.UserId).IsUnique();   // 1 cart per user
                e.HasOne(c => c.User)
                .WithOne(u => u.Cart)
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CartItem>(e =>
            {
                e.HasKey(ci => ci.CartItemId);
                e.HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);   // don't cascade delete cart when product removed

                // Same product + size + color combo merged
                e.HasIndex(ci => new { ci.CartId, ci.ProductId, ci.Size, ci.Color });
            });
         modelBuilder.Entity<Address>(e =>
        {
            e.HasKey(a => a.AddressId);
            e.HasOne(a => a.User)
            .WithMany(u => u.Addresses)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => new { a.UserId, a.IsDefault });
        });   
    }
}