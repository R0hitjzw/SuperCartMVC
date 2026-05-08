using Microsoft.AspNetCore.Identity;

namespace SuperCartMVC.Models;

public class Review
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public IdentityUser? User { get; set; }

    // Identificador del producto (market + nombre)
    public string Market { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;

    public int Estrellas { get; set; } // 1-5
    public string? Comentario { get; set; }

    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
}