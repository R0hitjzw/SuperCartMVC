// Services/Finders/Impl/CondisFinder.cs
using System.Text.Json;

namespace SuperCartMVC.Services.Finders.Impl;

public class CondisFinder : AbstractFinder
{
    public CondisFinder(ILogger<CondisFinder> logger) : base(logger) { }

    private const string Uri =
        "https://api.empathy.co/search/v1/query/condis/search?lang=es&start=0&rows=24&query={0}&store=718";

    private const string ImageBase = "https://cdn.condis.es/fit-in/300x300/es/products/";

    public override DTOs.Market GetMarket() => DTOs.Market.CONDIS;

    protected override string GetMarketUri() => Uri;

    protected override void AddHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0 Safari/537.36");
        request.Headers.Add("Accept", "application/json, text/plain, */*");
        request.Headers.Add("Origin", "https://www.condisline.com");
        request.Headers.Add("Referer", "https://www.condisline.com/");
    }

    protected override List<DTOs.Product> GetProductList(JsonElement root)
    {
        var products = new List<DTOs.Product>();

        if (!root.TryGetProperty("catalog", out var catalog)) return products;
        if (!catalog.TryGetProperty("content", out var content)) return products;

        int count = 0;
        foreach (var item in content.EnumerateArray())
        {
            try
            {
                var product = new DTOs.Product();
                product.Market = DTOs.Market.CONDIS;

                product.Name = item.TryGetProperty("description", out var desc)
                    ? desc.GetString() ?? ""
                    : "";

                product.Brand = item.TryGetProperty("brand", out var brand)
                    ? brand.GetString() ?? "-"
                    : "-";

                if (item.TryGetProperty("price", out var priceObj) &&
                    priceObj.TryGetProperty("current", out var current))
                    product.Price = current.GetSingle();

                if (item.TryGetProperty("pum", out var pum))
                    product.PriceUnitOrKg = pum.GetString() ?? "";
                    NormalizeUnitPrice(product); 

                if (item.TryGetProperty("externalId", out var externalId))
                {
                    var id = externalId.GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(id))
                        product.Image = $"{ImageBase}{id}.jpg";
                }

                products.Add(product);
                if (++count >= 20) break;
            }
            catch (Exception ex) { Console.WriteLine("ERROR PRODUCTO CONDIS: " + ex.Message); }
        }

        return products;
    }
}