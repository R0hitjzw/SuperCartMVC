// Services/Finders/AbstractFinder.cs
using System.Text.Json;

namespace SuperCartMVC.Services.Finders;

public abstract class AbstractFinder : IFinder
// clase ABSTRACTA, obliga a las clases que hereden de ella 
// https://learn.microsoft.com/es-es/dotnet/csharp/language-reference/keywords/abstract ABSTRACT

// IFinder 
// https://learn.microsoft.com/es-es/dotnet/api/microsoft.visualstudio.text.operations.ifinder?view=visualstudiosdk-2022
{
    private readonly ILogger _logger; // INTERFICIE LOGGING YA ES NATIU DE ASP.NET
                                      // private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    // INTENT DE CONFIGURAR SSL Y DECOMPRESSION, PERO NO FUNCIONA EN CARREFOUR, PROBAR CON OTROS MERCADOS
    private static readonly HttpClient _httpClient = new(new HttpClientHandler
    {
        SslProtocols = System.Security.Authentication.SslProtocols.Tls13 |
                   System.Security.Authentication.SslProtocols.Tls12,
        AutomaticDecompression = System.Net.DecompressionMethods.GZip |
                             System.Net.DecompressionMethods.Deflate |
                             System.Net.DecompressionMethods.Brotli
    })
    { Timeout = TimeSpan.FromSeconds(10) };

    //  clase para enviar solicitudes HTTP y recibir respuestas HTTP de un recurso identificado por un URI.
    // https://learn.microsoft.com/es-es/dotnet/api/system.net.http.httpclient?view=net-8.0

    protected AbstractFinder(ILogger logger) => _logger = logger; // Camps privats es posa _ abans del nom _logger
                                                                  // LO MATEIX QUE - { _logger = logger; }      

    /// <summary>
    /// Hook de traducción: los finders pueden sobrescribir este método para adaptar
    /// el término al idioma de su API antes de hacer la petición.
    /// Por defecto devuelve el término sin modificar.
    /// </summary>
    protected virtual Task<string> TranslateTermAsync(string term) => Task.FromResult(term);

    public async Task<List<DTOs.Product>> FindProductsByTermAsync(string term)
    {
        var marketName = GetMarket().ToString();

        // Cada finder puede adaptar el término (ej: Bonpreu traduce ES→CA)
        term = await TranslateTermAsync(term);

        Console.WriteLine($"[{marketName}] Iniciando búsqueda: {term}");
        try
        {
            var encoded = Uri.EscapeDataString(term);
            HttpResponseMessage response;

            if (GetHttpMethod() == HttpMethod.Get)
            {
                var url = string.Format(GetMarketUri(), encoded);
                Console.WriteLine($"[{marketName}] GET {url}");
                response = await _httpClient.GetAsync(url);
            }
            else
            {
                var url = string.Format(GetMarketUri(), encoded);
                Console.WriteLine($"[{marketName}] POST {url}");
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                AddHeaders(request);
                request.Content = GetPostBody(term);
                response = await _httpClient.SendAsync(request);
            }

            Console.WriteLine($"[{marketName}] HTTP {(int)response.StatusCode}");
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[{marketName}] Body preview: {body[..Math.Min(200, body.Length)]}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[{marketName}] ERROR HTTP {(int)response.StatusCode}");
                return new List<DTOs.Product>();
            }

            var products = PostProcessResponse(PreProcessResponse(body));
            Console.WriteLine($"[{marketName}] Productos: {products.Count}");
            return products;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{marketName}] EXCEPCION: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[{marketName}] INNER: {ex.InnerException?.Message}");
            return new List<DTOs.Product>();
        }
    }

    protected virtual void AddHeaders(HttpRequestMessage request) { }
    protected virtual string PreProcessResponse(string response) => response;
    protected virtual List<DTOs.Product> PostProcessResponse(string response)
    {
        var json = JsonDocument.Parse(response).RootElement;
        return GetProductList(json);
    }

    protected abstract string GetMarketUri();
    protected abstract List<DTOs.Product> GetProductList(JsonElement root);
    public abstract DTOs.Market GetMarket();
    protected virtual HttpMethod GetHttpMethod() => HttpMethod.Get;
    protected virtual HttpContent? GetPostBody(string term) => null;

    protected static void NormalizeUnitPrice(DTOs.Product product)
    {
        if (string.IsNullOrWhiteSpace(product.PriceUnitOrKg)) return;

        var raw = product.PriceUnitOrKg.ToLower()
            .Replace(",", ".")
            .Replace("€", "")
            .Replace(" ", "");

        // Detectar unidad
        if (raw.Contains("kg")) product.UnitType = "kg";
        else if (raw.Contains("/l") || raw.Contains("litro") || raw.Contains("litre")) product.UnitType = "L";
        else if (raw.Contains("ud") || raw.Contains("unidad") || raw.Contains("un.")) product.UnitType = "ud";
        else product.UnitType = "";

        // Extraer número — busca el primer número decimal en el string
        var match = System.Text.RegularExpressions.Regex.Match(raw, @"(\d+\.?\d*)");
        if (match.Success && float.TryParse(match.Value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var val))
        {
            product.UnitPrice = val;
        }
    }
}