using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperCartMVC.Data;
using SuperCartMVC.Models;

namespace SuperCartMVC.Controllers;

public class CartController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public CartController(AppDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var items = await _db.CartItems
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.AddedAt)
            .ToListAsync();
        return View(items);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Toggle(string market, string name, string price, string image, string priceUnitOrKg)
    {
        float parsedPrice = float.Parse(price, System.Globalization.CultureInfo.InvariantCulture);
        var userId = _userManager.GetUserId(User);

        var existing = await _db.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId
                && c.Market == market
                && c.Name == name
                && c.Price == parsedPrice);

        if (existing != null)
        {
            _db.CartItems.Remove(existing);
            await _db.SaveChangesAsync();
            return Json(new { inCart = false });
        }

        _db.CartItems.Add(new CartItem
        {
            UserId = userId!,
            Market = market,
            Name = name,
            Price = parsedPrice,
            Image = image,
            PriceUnitOrKg = priceUnitOrKg,
            Quantity = 1
        });
        await _db.SaveChangesAsync();
        return Json(new { inCart = true });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int id, int quantity)
    {
        var userId = _userManager.GetUserId(User);
        var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (item != null && quantity > 0)
        {
            item.Quantity = quantity;
            await _db.SaveChangesAsync();
        }
        return Json(new { ok = true });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (item != null)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Clear()
    {
        var userId = _userManager.GetUserId(User);
        var items = _db.CartItems.Where(c => c.UserId == userId);
        _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CheckCart([FromBody] List<CheckCartItem> items)
    {
        var userId = _userManager.GetUserId(User);
        var userCart = await _db.CartItems
            .Where(c => c.UserId == userId)
            .Select(c => new { c.Market, c.Name, c.Price })
            .ToListAsync();

        var result = items.Select(item =>
        {
            float parsedPrice = float.Parse(item.Price, System.Globalization.CultureInfo.InvariantCulture);
            bool inCart = userCart.Any(c =>
                c.Market == item.Market &&
                c.Name == item.Name &&
                Math.Abs(c.Price - parsedPrice) < 0.001f);
            return new { item.Market, item.Name, item.Price, inCart };
        });

        return Json(result);
    }
}

public class CheckCartItem
{
    public string Market { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
}