using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperCartMVC.Data;
using SuperCartMVC.Models;

namespace SuperCartMVC.Controllers;

public class FavoritoController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public FavoritoController(AppDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var favoritos = await _db.Favoritos
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.AddedAt)
            .ToListAsync();
        return View(favoritos);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Toggle(string market, string name, string price, string image, string priceUnitOrKg)
    {
        // Parsear con cultura invariante (punto = decimal siempre)
        float parsedPrice = float.Parse(price, System.Globalization.CultureInfo.InvariantCulture);

        var userId = _userManager.GetUserId(User);

        var existing = await _db.Favoritos
            .FirstOrDefaultAsync(f => f.UserId == userId
            && f.Market == market
            && f.Name == name
            && f.Price == parsedPrice);

        if (existing != null)
        {
            _db.Favoritos.Remove(existing);
            await _db.SaveChangesAsync();
            return Json(new { isFavorito = false });
        }

        _db.Favoritos.Add(new Favorito
        {
            UserId = userId!,
            Market = market,
            Name = name,
            Price = parsedPrice,  // â† usa parsedPrice, no price
            Image = image,
            PriceUnitOrKg = priceUnitOrKg
        });
        await _db.SaveChangesAsync();
        return Json(new { isFavorito = true });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CheckFavoritos([FromBody] List<CheckFavoritoItem> items)
    {
        var userId = _userManager.GetUserId(User);
        var userFavs = await _db.Favoritos
            .Where(f => f.UserId == userId)
            .Select(f => new { f.Market, f.Name, f.Price })
            .ToListAsync();

        var result = items.Select(item =>
        {
            float parsedPrice = float.Parse(item.Price, System.Globalization.CultureInfo.InvariantCulture);
            bool isFav = userFavs.Any(f =>
                f.Market == item.Market &&
                f.Name == item.Name &&
                Math.Abs(f.Price - parsedPrice) < 0.001f);
            return new { item.Market, item.Name, item.Price, isFavorito = isFav };
        });

        return Json(result);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        var fav = await _db.Favoritos.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
        if (fav != null)
        {
            _db.Favoritos.Remove(fav);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    public class CheckFavoritoItem
    {
        public string Market { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
    }
}
