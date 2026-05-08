// Services/Finders/Impl/AlcampoFinder.cs
using System.Text.Json;

namespace SuperCartMVC.Services.Finders.Impl;

public class AlcampoFinder : AbstractFinder
{
    public AlcampoFinder(ILogger<AlcampoFinder> logger) : base(logger) { }

    // tag=web es obligatorio, sin él devuelve resultados distintos
    private const string UriTemplate =
        "https://www.compraonline.alcampo.es/api/webproductpagews/v6/product-pages/search" +
        "?includeAdditionalPageInfo=true&maxPageSize=300&maxProductsToDecorate=50&tag=web&q={0}";

    public override DTOs.Market GetMarket() => DTOs.Market.ALCAMPO;

    protected override string GetMarketUri() => UriTemplate;

    // GET por defecto, no hace falta override de GetHttpMethod ni GetPostBody

    protected override void AddHeaders(HttpRequestMessage request)
    {
        // Alcampo requiere un User-Agent de navegador real, si no devuelve 403
        request.Headers.TryAddWithoutValidation("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("Referer", "https://www.compraonline.alcampo.es/");
    }

    protected override List<DTOs.Product> GetProductList(JsonElement root)
    {
        var products = new List<DTOs.Product>();

        // Estructura: { "productGroups": [ { "decoratedProducts": [ {...}, {...} ] } ] }
        if (!root.TryGetProperty("productGroups", out var groups)) return products;

        foreach (var group in groups.EnumerateArray())
        {
            if (!group.TryGetProperty("decoratedProducts", out var decoratedProducts)) continue;

            foreach (var item in decoratedProducts.EnumerateArray())
            {
                try
                {
                    var product = new DTOs.Product();
                    product.Market = DTOs.Market.ALCAMPO;

                    product.Name = item.TryGetProperty("name", out var name)
                        ? name.GetString() ?? ""
                        : "";

                    product.Brand = item.TryGetProperty("brand", out var brand)
                        ? brand.GetString() ?? "-"
                        : "-";

                    // Precio principal: price.amount es string "1.44"
                    if (item.TryGetProperty("price", out var priceObj) &&
                        priceObj.TryGetProperty("amount", out var amount))
                    {
                        var amountStr = amount.ValueKind == JsonValueKind.String
                            ? amount.GetString() ?? "0"
                            : amount.GetRawText();

                        product.Price = float.Parse(amountStr,
                            System.Globalization.CultureInfo.InvariantCulture);
                    }

                    // Precio por kg/unidad: unitPrice.price.amount + unitPrice.unit
                    if (item.TryGetProperty("unitPrice", out var unitPriceObj))
                    {
                        var unitAmt = "";
                        var unitLabel = "";

                        if (unitPriceObj.TryGetProperty("price", out var upPrice) &&
                            upPrice.TryGetProperty("amount", out var upAmount))
                        {
                            var raw = upAmount.ValueKind == JsonValueKind.String
                                ? upAmount.GetString() ?? ""
                                : upAmount.GetRawText();

                            if (float.TryParse(raw, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var upFloat))
                                unitAmt = upFloat.ToString("F2").Replace(".", ",");
                        }

                        if (unitPriceObj.TryGetProperty("unit", out var unit))
                        {
                            // "fop.price.per.kg" → "kg", "fop.price.per.l" → "L", etc.
                            var raw = unit.GetString() ?? "";
                            unitLabel = raw.Contains(".kg") ? "kg"
                                      : raw.Contains(".l") ? "L"
                                      : raw.Contains(".ud") ? "ud"
                                      : raw.Split('.').LastOrDefault() ?? raw;
                        }

                        if (!string.IsNullOrWhiteSpace(unitAmt) && !string.IsNullOrWhiteSpace(unitLabel))
                            product.PriceUnitOrKg = $"{unitAmt} €/{unitLabel}";
                            NormalizeUnitPrice(product); 
                    }

                    // Imagen: image.src (300x300)
                    if (item.TryGetProperty("image", out var imageObj) &&
                        imageObj.TryGetProperty("src", out var imgSrc))
                    {
                        product.Image = imgSrc.GetString() ?? "";
                    }

                    // Solo añadir si tiene nombre y precio > 0
                    if (!string.IsNullOrWhiteSpace(product.Name) && product.Price > 0)
                        products.Add(product);
                }
                catch { /* producto con datos incompletos, se omite */ }
            }
        }

        return products;
    }
}