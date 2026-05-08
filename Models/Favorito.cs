using Microsoft.AspNetCore.Identity;

namespace SuperCartMVC.Models;

public class Favorito
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public IdentityUser? User { get; set; }

    // Datos del producto (lo guardamos como snapshot)
    public string Market { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public float Price { get; set; }
    public string Image { get; set; } = string.Empty;
    public string? PriceUnitOrKg { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}