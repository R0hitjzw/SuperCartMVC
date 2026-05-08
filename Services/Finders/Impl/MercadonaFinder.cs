// Services/Finders/Impl/MercadonaFinder.cs
using System.Text;
using System.Text.Json;

namespace SuperCartMVC.Services.Finders.Impl;

public class MercadonaFinder : AbstractFinder
{

    public MercadonaFinder(ILogger<MercadonaFinder> logger) : base(logger) { }

    private const string Uri =
    "https://7uzjkl1dj0-dsn.algolia.net/1/indexes/products_prod_4315_es/query?x-algolia-application-id=7UZJKL1DJ0&x-algolia-api-key=9d8f2e39e90df472b4f2e559a116fe17";

    public override DTOs.Market GetMarket() => DTOs.Market.MERCADONA;

    protected override string GetMarketUri() => Uri;

    protected override HttpMethod GetHttpMethod() => HttpMethod.Post;

    protected override HttpContent GetPostBody(string term) =>
        new StringContent(
            $"{{\"params\":\"query={term}&clickAnalytics=true\"}}",
            Encoding.UTF8,
            "application/json"
        );

    protected override List<DTOs.Product> GetProductList(JsonElement root)
    {
        var products = new List<DTOs.Product>();

        if (!root.TryGetProperty("hits", out var hits)) return products;

        foreach (var item in hits.EnumerateArray())
        {
            try
            {
                var product = new DTOs.Product();
                product.Market = DTOs.Market.MERCADONA;
                product.Brand = "-";
                product.Name = item.GetProperty("display_name").GetString() ?? "";

                // unit_price viene como string "5.04", hay que parsearlo
                var priceObj = item.GetProperty("price_instructions");

                // BIEN:
                var priceStr = priceObj.GetProperty("unit_price").GetString() ?? "0";
                product.Price = float.Parse(priceStr, System.Globalization.CultureInfo.InvariantCulture);

                // precio por kg/unidad opcional
                if (priceObj.TryGetProperty("reference_price", out var refPrice) &&
                    priceObj.TryGetProperty("reference_format", out var refFormat))
                {
                    // var p = refPrice.ValueKind == JsonValueKind.Number
                    // ? refPrice.GetSingle().ToString("F2").Replace(".", ",")
                    // : refPrice.GetString()?.Replace(".", ",") ?? "";

                    // var u = refFormat.GetString() ?? "";
                    // product.PriceUnitOrKg = $"{p} €/{u}";

                    var rawRef = refPrice.ValueKind == JsonValueKind.Number
                    ? refPrice.GetSingle()
                    : float.Parse(refPrice.GetString() ?? "0", System.Globalization.CultureInfo.InvariantCulture);

                    var p = rawRef.ToString("F2").Replace(".", ",");
                    var u = refFormat.GetString() ?? "";
                    product.PriceUnitOrKg = $"{p} €/{u}";

                    NormalizeUnitPrice(product);
                }

                // imagen
                if (item.TryGetProperty("thumbnail", out var thumb))
                {
                    var img = thumb.GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(img))
                        product.Image = img;
                }

                products.Add(product);
            }
            catch (Exception ex) { Console.WriteLine("ERROR PRODUCTO: " + ex.Message); }
        }

        return products;
    }
}
