// Services/Finders/Impl/AldiFinder.cs

using System.Text;
using System.Text.Json;

namespace SuperCartMVC.Services.Finders.Impl;

public class AldiFinder : AbstractFinder
{
    private const string Url =
        "https://l9knu74io7-dsn.algolia.net/1/indexes/*/queries?X-Algolia-Api-Key=19b0e28f08344395447c7bdeea32da58&X-Algolia-Application-Id=L9KNU74IO7";

    public AldiFinder(ILogger<AldiFinder> logger) : base(logger) { }
    public override DTOs.Market GetMarket() => DTOs.Market.ALDI;
    protected override string GetMarketUri() => Url;
    protected override HttpMethod GetHttpMethod() => HttpMethod.Post;

    protected override HttpContent GetPostBody(string term) =>
        new StringContent(
            "{\"requests\":[" +
            "{\"indexName\":\"prod_es_es_es_offers\",\"params\":\"hitsPerPage=12&page=0&query=" + term + "&tagFilters=\"}," +
            "{\"indexName\":\"prod_es_es_es_assortment\",\"params\":\"hitsPerPage=12&page=0&query=" + term + "&tagFilters=\"}" +
            "]}",
            Encoding.UTF8, "application/json");

    protected override List<DTOs.Product> GetProductList(JsonElement root)
    {
        var products = new List<DTOs.Product>();
        if (!root.TryGetProperty("results", out var results)) return products;

        foreach (var result in results.EnumerateArray())
        {
            if (!result.TryGetProperty("hits", out var hits)) continue;
            foreach (var item in hits.EnumerateArray())
            {
                try
                {
                    if (!item.TryGetProperty("salesPrice", out var sp)) continue;
                    var product = new DTOs.Product
                    {
                        Market = DTOs.Market.ALDI,
                        Brand = "-",
                        Price = sp.GetSingle(),
                        Name = item.TryGetProperty("productName", out var n) ? n.GetString() ?? "" : "",
                        Image = item.TryGetProperty("productPicture", out var img) ? img.GetString() ?? "" : "",
                        PriceUnitOrKg = item.TryGetProperty("basicUnit", out var bu) ? bu.GetString() ?? "" : ""
                    };
                    NormalizeUnitPrice(product);
                    if (products.Count == 0) // solo loguea el primer producto para no spamear
                        Console.WriteLine("[ALDI] Campos disponibles: " + item.ToString());
                    products.Add(product);
                }
                catch (Exception ex) { Console.WriteLine("[ALDI] Error: " + ex.Message); }
            }
        }
        return products;
    }
}