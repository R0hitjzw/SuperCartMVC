// Services/Finders/Impl/ConsumFinder.cs
using System.Text.Json;

namespace SuperCartMVC.Services.Finders.Impl;

public class ConsumFinder : AbstractFinder
{
    private const string Url =
        "https://tienda.consum.es/api/rest/V1.0/catalog/searcher/products?q={0}&limit=20&showRecommendations=false";

    public ConsumFinder(ILogger<ConsumFinder> logger) : base(logger) { }
    public override DTOs.Market GetMarket() => DTOs.Market.CONSUM;
    protected override string GetMarketUri() => Url;

    protected override List<DTOs.Product> GetProductList(JsonElement root)
    {
        var products = new List<DTOs.Product>();
        try
        {
            var productList = root.GetProperty("catalog").GetProperty("products").EnumerateArray();
            foreach (var item in productList)
            {
                try
                {
                    var productData = item.GetProperty("productData");
                    var priceData = item.GetProperty("priceData");
                    var priceObj = priceData.GetProperty("prices")[0].GetProperty("value");
                    var centAmount = priceObj.GetProperty("centAmount").GetSingle();


                    string priceUnitOrKg = "";
                    if (priceObj.TryGetProperty("centUnitAmount", out var unitAmount) &&
                        priceData.TryGetProperty("unitPriceUnitType", out var unitType))
                    {
                        var unitVal = unitAmount.ValueKind == JsonValueKind.Number
                            ? unitAmount.GetDecimal().ToString("F2").Replace(".", ",")
                            : unitAmount.GetString()?.Replace(".", ",") ?? "";
                        priceUnitOrKg = $"{unitVal} €/{unitType.GetString()}";
                    }

                    string image = "";
                    if (item.TryGetProperty("media", out var media) && media.GetArrayLength() > 0)
                        image = media[0].GetProperty("url").GetString() ?? "";

                    products.Add(new DTOs.Product
                    {
                        Market = DTOs.Market.CONSUM,
                        Brand = "-",
                        Price = centAmount,
                        Name = productData.GetProperty("description").GetString() ?? "",
                        Image = image,
                        PriceUnitOrKg = priceUnitOrKg
                    });
                    NormalizeUnitPrice(products[^1]);
                }
                catch (Exception ex) { Console.WriteLine("[CONSUM] Error producto: " + ex.Message); }
            }
        }
        catch (Exception ex) { Console.WriteLine("[CONSUM] Error parseo: " + ex.Message); }
        return products;
    }
}