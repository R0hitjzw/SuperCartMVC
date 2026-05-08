using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperCartMVC.Data;
using SuperCartMVC.Models;

namespace SuperCartMVC.Controllers;

public class ReviewController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public ReviewController(AppDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(string market, string productName, string productImage, int estrellas, string? comentario)
    {
        var userId = _userManager.GetUserId(User);

        // Si ya existe una reseña de este usuario para este producto, la actualiza
        var existing = await _db.Reviews
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Market == market && r.ProductName == productName);

        if (existing != null)
        {
            existing.Estrellas = estrellas;
            existing.Comentario = comentario;
            existing.CreadaEn = DateTime.UtcNow;
        }
        else
        {
            _db.Reviews.Add(new Review
            {
                UserId = userId!,
                Market = market,
                ProductName = productName,
                ProductImage = productImage,
                Estrellas = estrellas,
                Comentario = comentario
            });
        }

        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    // Devuelve las reviews de un producto concreto (para mostrarlas en la card)
    [HttpGet]
    public async Task<IActionResult> GetProductReviews(string market, string productName)
    {
        var reviews = await _db.Reviews
            .Where(r => r.Market == market && r.ProductName == productName)
            .Select(r => new {
                r.Estrellas,
                r.Comentario,
                r.CreadaEn,
                UserEmail = r.User!.Email
            })
            .ToListAsync();

        var avgRating = reviews.Any() ? reviews.Average(r => r.Estrellas) : 0;

        // Busca la review del usuario actual si está logado
        object? myReview = null;
        var userId = _userManager.GetUserId(User);
        if (userId != null)
        {
            myReview = await _db.Reviews
                .Where(r => r.UserId == userId && r.Market == market && r.ProductName == productName)
                .Select(r => new { r.Estrellas, r.Comentario })
                .FirstOrDefaultAsync();
        }

        return Json(new { reviews, avgRating, myReview });
    }

    // Devuelve el rating medio de una lista de productos de golpe (para el sort)
    [HttpPost]
    public async Task<IActionResult> GetRatings([FromBody] List<ProductRatingRequest> products)
    {
        var ratings = new Dictionary<string, double>();

        foreach (var p in products)
        {
            var key = $"{p.Market}|{p.Name}";
            var avg = await _db.Reviews
                .Where(r => r.Market == p.Market && r.ProductName == p.Name)
                .Select(r => (double?)r.Estrellas)
                .AverageAsync() ?? 0;
            ratings[key] = avg;
        }

        return Json(ratings);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MisReviews()
    {
        var userId = _userManager.GetUserId(User);
        var reviews = await _db.Reviews
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreadaEn)
            .ToListAsync();
        return Json(reviews);
    }

    // Vista individual de producte amb reseñes
    [HttpGet]
    public async Task<IActionResult> Details(string market, string productName, string? productImage)
    {
        var reviews = await _db.Reviews
            .Include(r => r.User)
            .Where(r => r.Market == market && r.ProductName == productName)
            .OrderByDescending(r => r.CreadaEn)
            .ToListAsync();

        var avgRating = reviews.Any() ? reviews.Average(r => r.Estrellas) : 0.0;

        int myStars = 0;
        string? myComment = null;
        var userId = _userManager.GetUserId(User);
        if (userId != null)
        {
            var mine = reviews.FirstOrDefault(r => r.UserId == userId);
            if (mine != null)
            {
                myStars = mine.Estrellas;
                myComment = mine.Comentario;
            }
        }

        ViewBag.Market = market;
        ViewBag.ProductName = productName;
        ViewBag.ProductImage = productImage ?? string.Empty;
        ViewBag.AvgRating = avgRating;
        ViewBag.MyStars = myStars;
        ViewBag.MyComment = myComment;

        return View(reviews);
    }

}

public class ProductRatingRequest
{
    public string Market { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

