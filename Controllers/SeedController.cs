using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperCartMVC.Data;
using SuperCartMVC.Models;

namespace SuperCartMVC.Controllers;

public class SeedController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public SeedController(AppDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Reviews(string key)
    {
        if (key != "supercart2024")
            return Content("❌ Acceso denegado.");
        var fakeUsers = new[]
        {
            ("maria.garcia@gmail.com",    "Maria123!"),
            ("carlos.lopez@hotmail.com",  "Carlos123!"),
            ("ana.martinez@gmail.com",    "Ana12345!"),
            ("pedro.sanchez@outlook.com", "Pedro123!"),
            ("lucia.fernandez@gmail.com", "Lucia123!"),
            ("jorge.ruiz@yahoo.com",      "Jorge123!"),
            ("elena.torres@gmail.com",    "Elena123!"),
            ("david.moreno@hotmail.com",  "David123!"),
        };

        var userIds = new List<string>();
        foreach (var (email, pass) in fakeUsers)
        {
            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null) { userIds.Add(existing.Id); continue; }
            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            await _userManager.CreateAsync(user, pass);
            userIds.Add(user.Id);
        }

        var productos = new[]
{
    // Leche
    ("MERCADONA", "Leche entera Hacendado",       ""),
    ("MERCADONA", "Leche entera Asturiana",        ""),
    ("MERCADONA", "Leche entera fresca Hacendado", ""),
    // Huevos
    ("MERCADONA", "Huevos grandes L",                    ""),
    ("MERCADONA", "Huevos de gallinas camperas",          ""),
    ("MERCADONA", "Huevos medianos M",                   ""),
    ("AMETLLER",  "Huevos de codorniz Sagra - 12uds.",   ""),
    ("CONDIS",    "HUEVOS CONDIS CAMPEROS 6 UNIDADES",   ""),
    ("CONDIS",    "HUEVOS ROIG NATURELLE 10 UNIDADES",   ""),
    ("CONSUM",    "Huevos M Medianos Docena 1 Dc",       ""),
    // Naranja
    ("CONSUM",    "Naranja",                              ""),
    ("MERCADONA", "Naranja de mesa",                      ""),
    ("AMETLLER",  "Naranja - bolsa 6 kg",                 ""),
    ("AMETLLER",  "Naranja para postres categoría 1",     ""),
    ("DIA",       "Naranja Torres malla 1.5 Kg",          ""),
    ("CONSUM",    "Naranja Malla 2 Kg",                   ""),
    ("CONSUM",    "Naranja Para Zumo Malla 4 Kg",         ""),
    ("DIA",       "Naranja selección granel 1 Kg aprox.", ""),
    // Yogur
    ("MERCADONA", "Yogur natural Hacendado",                      ""),
    ("MERCADONA", "Yogur natural Danone",                          ""),
    ("MERCADONA", "Yogur griego natural Hacendado",                ""),
    ("MERCADONA", "Yogur natural de cabra Hacendado",              ""),
    ("MERCADONA", "Yogur natural con azúcar de caña Hacendado",   ""),
    ("AMETLLER",  "Yogur Skyr natural Ehrmann 150g",              ""),
    ("AMETLLER",  "Yogur natural Essencials 125g - 4uds.",        ""),
    ("AMETLLER",  "Yogur natural La Fageda 125g - 4uds.",         ""),
    ("AMETLLER",  "Yogur cremoso natural Ametller Origen 500g",   ""),
    ("AMETLLER",  "Yogur natural ecológico Pur Natur 750g",       ""),
    ("AMETLLER",  "Yogur natural cremoso Ametller Origen 125g - 4uds.", ""),
    ("AMETLLER",  "Yogur firme natural Ametller Origen 125g - 4uds.",   ""),
    ("CONDIS",    "YOGUR DANONE NATURAL 4 UNIDADES",              ""),
    ("CONDIS",    "YOGUR PASTORET NATURAL 500 G",                 ""),
    ("CONDIS",    "YOGUR DANONE ORIGINAL NATURAL 2 UNIDADES",     ""),
    ("CONDIS",    "YOGUR LA FAGEDA NATURAL 125G 4 UNIDADES",      ""),
    ("CONDIS",    "YOGUR LA FAGEDA NATURAL ORIGENS 4 UNIDADES",   ""),
    ("CONSUM",    "Yogur Natural Artesano 500 Gr",                ""),
    ("CONSUM",    "Yogur Líquido Natural Azucarado 1000 Gr",      ""),
    ("CONSUM",    "Yogur Natural 4 x 125 Gr",                     ""),
    ("CONSUM",    "Yogur Natural Azucarado 2 x 125 Gr",           ""),
};

        var comentarios = new[]
        {
            "Muy buena relación calidad-precio, lo compro siempre.",
            "Excelente producto, totalmente recomendable.",
            "Cumple perfectamente con lo que promete.",
            "Me sorprendió gratamente, mucho mejor de lo esperado.",
            "Calidad correcta para el precio que tiene.",
            "Lo uso a diario, nunca me ha fallado.",
            "Buen producto aunque he probado mejores.",
            "La calidad ha bajado un poco últimamente.",
            "Para el precio está bien, no esperes maravillas.",
            "No me convenció del todo, esperaba más.",
            "Sabor muy natural, sin artificios. Me encanta.",
            "Ideal para toda la familia, tamaño perfecto.",
            "Llevo años comprándolo y nunca defrauda.",
            "La presentación podría mejorar pero el producto es bueno.",
            "Muy fresco y de buena calidad.",
            "Un clásico que siempre está en mi nevera.",
            "Precio competitivo y buena calidad.",
            "Lo recomiendo sin dudarlo.",
            "Justo lo que necesitaba, perfecto.",
            "Podría mejorar el packaging pero el producto es correcto.",
        };

        var rnd = new Random(42);
        int added = 0;

        foreach (var (market, name, image) in productos)
        {
            int count = rnd.Next(4, 9);
            var shuffledUsers = userIds.OrderBy(_ => rnd.Next()).Take(count).ToList();

            foreach (var uid in shuffledUsers)
            {
                bool exists = await _db.Reviews.AnyAsync(r =>
                    r.UserId == uid && r.Market == market && r.ProductName == name);
                if (exists) continue;

                _db.Reviews.Add(new Review
                {
                    UserId = uid,
                    Market = market,
                    ProductName = name,
                    ProductImage = image,
                    Estrellas = rnd.Next(3, 6),
                    Comentario = rnd.Next(0, 3) == 0 ? null : comentarios[rnd.Next(comentarios.Length)],
                    CreadaEn = DateTime.UtcNow.AddDays(-rnd.Next(1, 120))
                });
                added++;
            }
        }

        await _db.SaveChangesAsync();
        return Content($"✅ Seed completado: {added} reseñas añadidas para {productos.Length} productos con {userIds.Count} usuarios.");
    }
}