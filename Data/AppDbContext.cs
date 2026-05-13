using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SuperCartMVC.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace SuperCartMVC.Data;

public class AppDbContext : IdentityDbContext, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Favorito> Favoritos { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
}