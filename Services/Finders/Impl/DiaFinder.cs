// Services/Finders/Impl/DiaFinder.cs
using System.Text.Json;

namespace SuperCartMVC.Services.Finders.Impl;

public class DiaFinder : AbstractFinder
{
    public DiaFinder(ILogger<DiaFinder> logger) : base(logger) { }

    private const string Uri =
        "https://www.dia.es/api/v1/search-back/search/reduced?q={0}&page=1";

    public override DTOs.Market GetMarket() => DTOs.Market.DIA;

    protected override string GetMarketUri() => Uri;

    protected override List<DTOs.Product> GetProductList(JsonElement root)
    {
        var products = new List<DTOs.Product>();

        if (!root.TryGetProperty("search_items", out var items)) return products;

        foreach (var item in items.EnumerateArray())
        {
            try
            {
                var product = new DTOs.Product();
                product.Market = DTOs.Market.DIA;

                product.Name = item.TryGetProperty("display_name", out var name)
                    ? name.GetString() ?? ""
                    : "";

                product.Brand = item.TryGetProperty("brand", out var brand)
                    ? brand.GetString() ?? "-"
                    : "-";

                if (item.TryGetProperty("prices", out var prices))
                {
                    product.Price = prices.TryGetProperty("price", out var price)
                        ? price.GetSingle()
                        : 0f;

                    if (prices.TryGetProperty("price_per_unit", out var pricePerUnit) &&
                        prices.TryGetProperty("measure_unit", out var measureUnit))
                    {
                        var p = pricePerUnit.GetSingle().ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                        var u = measureUnit.GetString()?.ToLower() ?? "";
                        product.PriceUnitOrKg = $"{p} €/{u}";
                        NormalizeUnitPrice(product);
                    }
                }

                if (item.TryGetProperty("image", out var image))
                {
                    var img = image.GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(img))
                        product.Image = img.StartsWith("http") ? img : $"https://www.dia.es{img}";
                }

                products.Add(product);
            }
            catch (Exception ex) { Console.WriteLine("ERROR PRODUCTO DIA: " + ex.Message); }
        }

        return products;
    }
}