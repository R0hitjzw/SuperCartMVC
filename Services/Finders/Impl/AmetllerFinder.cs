using System.Text.Json;

namespace SuperCartMVC.Services.Finders.Impl;

public class AmetllerFinder : AbstractFinder
{
    private const string Url =
        // "https://www.ametllerorigen.com/api/catalog_system/pub/products/search/{0}";
        "https://www.ametllerorigen.com/api/catalog_system/pub/products/search/{0}?_from=0&_to=20";

    public AmetllerFinder(ILogger<AmetllerFinder> logger) : base(logger) { }
    public override DTOs.Market GetMarket() => DTOs.Market.AMETLLER;
    protected override string GetMarketUri() => Url;

    protected override List<DTOs.Product> GetProductList(JsonElement root)
    {
        var products = new List<DTOs.Product>();

        foreach (var item in root.EnumerateArray())
        {
            try
            {
                var name = item.GetProperty("productName").GetString() ?? "";

                if (item.TryGetProperty("Name_ES", out var nameEs) &&
                    nameEs.ValueKind == JsonValueKind.Array &&
                    nameEs.GetArrayLength() > 0)
                {
                    var es = nameEs[0].GetString();
                    if (!string.IsNullOrWhiteSpace(es))
                        name = es;
                }
                var brand = item.TryGetProperty("brand", out var b) ? b.GetString() ?? "-" : "-";

                // imagen
                string image = "";
                if (item.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
                {
                    var firstItem = items[0];
                    if (firstItem.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
                        image = images[0].GetProperty("imageUrl").GetString() ?? "";

                    // precio
                    float price = 0;
                    if (firstItem.TryGetProperty("sellers", out var sellers) && sellers.GetArrayLength() > 0)
                    {
                        var offer = sellers[0].GetProperty("commertialOffer");
                        price = offer.GetProperty("Price").GetSingle();
                    }

                    var product = new DTOs.Product
                    {
                        Market = DTOs.Market.AMETLLER,
                        Brand = brand,
                        Name = name,
                        Price = price,
                        Image = image
                    };
                    // Intentar inferir PriceUnitOrKg desde el nombre si contiene tamaño
                    var nameLower = name.ToLower();
                    var sizeMatch = System.Text.RegularExpressions.Regex.Match(nameLower, @"(\d+[\.,]?\d*)\s*(l|litre|litro|ml|kg|g)\b");
                    if (sizeMatch.Success)
                    {
                        var qty = float.Parse(sizeMatch.Groups[1].Value.Replace(",", "."),
                            System.Globalization.CultureInfo.InvariantCulture);
                        var unit = sizeMatch.Groups[2].Value;
                        if ((unit == "l" || unit == "litre" || unit == "litro") && qty > 0)
                        {
                            product.UnitPrice = product.Price / qty;
                            product.UnitType = "L";
                            product.PriceUnitOrKg = $"{product.UnitPrice:F2} €/L";
                        }
                        else if (unit == "ml" && qty > 0)
                        {
                            product.UnitPrice = product.Price / (qty / 1000f);
                            product.UnitType = "L";
                            product.PriceUnitOrKg = $"{product.UnitPrice:F2} €/L";
                        }
                        else if (unit == "kg" && qty > 0)
                        {
                            product.UnitPrice = product.Price / qty;
                            product.UnitType = "kg";
                            product.PriceUnitOrKg = $"{product.UnitPrice:F2} €/kg";
                        }
                        else if (unit == "g" && qty > 0)
                        {
                            product.UnitPrice = product.Price / (qty / 1000f);
                            product.UnitType = "kg";
                            product.PriceUnitOrKg = $"{product.UnitPrice:F2} €/kg";
                        }
                    }
                    products.Add(product);
                }
            }
            catch (Exception ex) { Console.WriteLine("[AMETLLER] Error: " + ex.Message); }
        }

        return products;
    }
}