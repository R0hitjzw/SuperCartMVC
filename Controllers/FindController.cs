// Controllers/FindController.cs
using Microsoft.AspNetCore.Mvc;
using SuperCartMVC.DTOs;
using SuperCartMVC.Services.Finders;
using SuperCartMVC.Services; // <-- per AnthropicKeyRotator
using System.Globalization;
using System.Text;

namespace SuperCartMVC.Controllers;

[ApiController] // Define las propiedades y los métodos del controlador API.
[Route("find")] // Proporciona propiedades y métodos para definir una ruta y para obtener información sobre la ruta.
public class FindController : ControllerBase
{
    private readonly IEnumerable<IFinder> _finders;
    private readonly AnthropicKeyRotator _keyRotator;

    // IEnumerable<IFinder> COLECCIO DE OBJECTES DELS FINDERS REGISTRATS A LA DI (injecció de dependències)

    // _finders és una variable privada que conté la col·lecció d'objectes IFinder que s'han registrat a la DI. 
    // Aquesta col·lecció es pot utilitzar per accedir a les funcionalitats dels diferents finders disponibles.
    // com que es privada es posa amb _ abans del nom, per convenció/nomenclatura.
    // NOMÉS EXISTEIX MENTRES EL CONTROLADOR EXISTEIX. Que es crea i destrueix amb cada petició HTTP.
    public FindController(IEnumerable<IFinder> finders, AnthropicKeyRotator keyRotator)
    {
        _finders = finders;
        _keyRotator = keyRotator;
        Console.WriteLine("FINDERS REGISTRADOS: " + finders.Count());
        foreach (var f in finders)
            Console.WriteLine("  - " + f.GetMarket());
    }

    [HttpGet] // Mètode HTTP GET, per rebre dades del servidor.


    // Métode asynchronous que rep el terme, i retorna una llista de productes que coincideixen amb el terme de cerca.
    // [FromQuery] indica d'on venen els paràmetres. 
    // En aquest cas des de la query string de la URL, per exemple: /find?term=leche&markets=Mercadona&markets=Aldi
    // FindByTerm([FromQuery] string term, [FromQuery] Market[]? markets)
    // El paràmetre "term" és el terme de cerca que l'usuari vol buscar, 
    // i "markets" és una llista OPCIONAL de mercats on buscar.

    public async Task<List<Product>> FindByTerm([FromQuery] string term, [FromQuery] Market[]? markets)
    {

        // Utiltzare una classe Task<T>, perque el necessito per operacions de tipo I/O, INPUT/OUTPUT, com les peticions HTTP, 
        // que no esta definit quan poden trigar.

        // I no nomes la classe Task convencional (que no retorna un valor), perque retorna un valor que es una llista de Productes, 
        // List<Product>, per això Task<List<Product>>.
        Console.WriteLine("PETICION RECIBIDA: " + term);

        var findersList = _finders
    .Where(f => markets == null || markets.Length == 0 || markets.Contains(f.GetMarket()))
    .ToList();


        // Si no se especifican mercados, se buscan en todos. 
        //Si se especifican, se filtran los finders para incluir solo los de esos mercados.

        Console.WriteLine("MARKETS : " + string.Join(", ", markets ?? new Market[0]));

        var tasks = findersList.Select(f => f.FindProductsByTermAsync(term)).ToList(); // <-- materializa el Select

        // Select() és un métode LINQ, que permet seleccionar i transformar elements individuals d'una col·lecció. 
        // Ens evita utilizar un bucle (foreach) per iterar sobre la col·lecció i aplicar una operació a cada element.
        // S'està seleccionant cada objecte IFinder de findersList i es crida a FindProductsByTermAsync(term) 
        // per obtenir una tasca (Task<List<Product>>) que representa l'operació async de cerca de productes per terme.
        // https://learn.microsoft.com/es-es/dotnet/api/system.linq.enumerable.select?view=net-8.0

        // ToList() (LINQ) és un métode que converteix una seqüpencia (IEnumerable<T>) en una llista (List<T>).
        // Així la podem iterar, saber quan totes les tasques s'han creat i completat amb Task.WhenAll.
        // https://learn.microsoft.com/es-es/dotnet/api/system.linq.enumerable.tolist?view=net-8.0
        Console.WriteLine("TASKS CREADAS: " + tasks.Count);

        try
        {
            var results = await Task.WhenAll(tasks);
            // Task.WhenAll retorna un array de (List<Product>, en aquest cas) amb el resultat de les tasques quan es completen.
            // https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.whenall?view=net-10.0

            var all = results.SelectMany(x => x).ToList();
            // SelectMany() (LINQ), és un métode que adjunta les seqüències resultants en una sola seqüència.
            // Tots els List<Product> de cada tasca es combinen en una sola llista (all) de tipus List<Product> també.
            // https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.selectmany?view=net-10.0

            Console.WriteLine("PRODUCTOS ANTES FILTRO: " + all.Count);


            var normalized = RemoveAccents(term.Trim().ToLower());
            // NORMALITXEM EL terme, eliminem ACCENTS, ESPAIS. Passem a minuscules.

            var termParts = normalized.Split(' '); // ? i aixo que faa?

            foreach (var p in all.Take(5))
            {
                var name = RemoveAccents(p.Name.ToLower());
                Console.WriteLine($"  Nombre normalizado: '{name}'");
                foreach (var t in termParts)
                {
                    var pattern = $@"(^|[\s,\-])({System.Text.RegularExpressions.Regex.Escape(t)})($|[\s,\-])";
                    var match = System.Text.RegularExpressions.Regex.IsMatch(name, pattern);
                    Console.WriteLine($"    term='{t}' pattern='{pattern}' match={match}");
                }
            }

            var filtered = all

                // ------------DOCU-----------------

                // .Where(p => termParts.Any(t => RemoveAccents(p.Name.ToLower()).Contains(t)))
                // // .Where(p => termParts.All(t => RemoveAccents(p.Name.ToLower()).Contains(t)))
                // // normalitzem el nom del producte, eliminem accents, passem a minuscules. I comprovem que conté elements de termParts.
                // .OrderBy(p => p.Price)
                // .ToList(); // Convertim a llista.

                // filtered, llista de productes que compleixen les condicions.
                // p representa cada producte de la llista all
                // t representa cada part del terme de cerca (termParts) que s'ha separat per espais.

                .Where(p =>
{
    // Bonpreu ya buscó con el término traducido al catalán (ES→CA).
    // Su API devuelve resultados relevantes directamente, no aplicar regex en español.
    if (p.Market == DTOs.Market.BONPREU) return true;

    var name = RemoveAccents(p.Name.ToLower());
    return termParts.All(t =>
    {
        var pattern = $@"(^|[\s,\-])({System.Text.RegularExpressions.Regex.Escape(t)})($|[\s,\-])";
        return System.Text.RegularExpressions.Regex.IsMatch(name, pattern);
    });
})
.OrderBy(p =>
{
    var name = RemoveAccents(p.Name.ToLower());
    var words = name.Split(new[] { ' ', ',', '-' }, StringSplitOptions.RemoveEmptyEntries);

    // Cuántas palabras del término aparecen al inicio del nombre
    var matchAtStart = termParts.Count(t => words.Length > 0 && words[0] == t);

    // Cuántas palabras del nombre son del término (pureza: "cafe molido" tiene 1/2, "cafe con leche batido sabor cafe" tiene 2/6)
    var purity = (float)termParts.Count(t => words.Contains(t)) / words.Length;

    // Ordenamos: más matchAtStart primero, más purity primero → negamos para OrderBy ascendente
    return (-matchAtStart, -purity);
})
.ThenBy(p => p.Price) // desempate por precio
.ToList();

            Console.WriteLine("PRODUCTOS DESPUES FILTRO REGEX: " + filtered.Count);
            if (filtered.Count > 8) // si hi ha mes de 8 productes, apliquem filtre per quedarnos amb els mes rellevants
                if (filtered.Count > 8)
                {
                    var capped = filtered
                        .GroupBy(p => p.Market)
                        .SelectMany(g => g.Take(10))
                        .ToList();
                    filtered = await FilterByAIRelevance(term, capped);
                }
            Console.WriteLine("PRODUCTOS DESPUES FILTRO AI: " + filtered.Count);


            return filtered;
            // retornem la llista de productes filtrats.
        }
        catch (Exception ex)
        {
            Console.WriteLine("EXCEPCION EN CONTROLLER: " + ex.Message);
            Console.WriteLine("INNER: " + ex.InnerException?.Message);
            return new List<Product>();
        }
    }

    // ── ENDPOINT: agrupa productos por relevancia (llamado desde el frontend para "Relevancia + €/kg ↑") ──
    [HttpPost("groupbyrelevance")]
    public async Task<IActionResult> GroupByRelevance([FromBody] BestByMarketRequest request)
    {
        if (request.Products == null || request.Products.Count == 0)
            return Ok(new { relevantes = new List<Product>(), dudosos = new List<Product>(), excluidos = new List<Product>() });

        var grouped = await GroupByRelevanceInternal(request.Term, request.Products);
        return Ok(new { relevantes = grouped.relevantes, dudosos = grouped.dudosos, excluidos = grouped.excluidos });
    }

    // ── Wrapper para mantener compatibilidad con el flujo de búsqueda normal ──
    private async Task<List<Product>> FilterByAIRelevance(string term, List<Product> products)
    {
        var (relevantes, dudosos, _) = await GroupByRelevanceInternal(term, products);
        // En la búsqueda normal incluimos relevantes + dudosos; sólo excluimos los claramente irrelevantes
        return relevantes.Concat(dudosos).ToList();
    }

    // ── Motor de clasificación por relevancia (tres niveles) ──
    private async Task<(List<Product> relevantes, List<Product> dudosos, List<Product> excluidos)>
        GroupByRelevanceInternal(string term, List<Product> products)
    {
        if (products.Count == 0) return (products, new List<Product>(), new List<Product>());

        // Incluimos supermercado, nombre, precio y precio/unidad para dar contexto completo a la IA
        var lines = products.Select((p, i) =>
            $"{i}:{p.Market}|{p.Brand} {p.Name}|{p.Price:F2}€|{p.PriceUnitOrKg}").ToList();
        var productText = string.Join("\n", lines);

        var systemPrompt =
            "Eres un clasificador de relevancia para SuperCart, comparador de precios de supermercados españoles. " +
            "Tu respuesta es SIEMPRE JSON puro y válido, sin markdown, sin texto adicional, sin explicaciones.";

        var userPrompt = $@"El usuario buscó: ""{term}""

Lista de productos (índice:supermercado|nombre|precio|precio/unidad):
{productText}

Clasifica TODOS los índices en exactamente estas tres categorías:
- ""relevantes"": el producto ES directamente lo que busca el usuario
- ""dudosos"": tiene relación pero no es el foco principal (variante, formato especial, elaborado)
- ""excluidos"": el término aparece como ingrediente secundario o es un producto claramente diferente

Ejemplos de clasificación:
- Búsqueda ""leche"": relevantes=[leche entera, semidesnatada, desnatada, sin lactosa], dudosos=[batido de leche, leche condensada], excluidos=[leche corporal Nivea, galletas ""con leche""]
- Búsqueda ""café"": relevantes=[café molido, en grano, soluble], dudosos=[capuchino, café con leche listo], excluidos=[crema corporal café, galletas sabor café]
- Búsqueda ""huevos"": relevantes=[huevos M, L, XL, camperos, ecológicos], dudosos=[huevos de codorniz], excluidos=[mayonesa, pasta al huevo]
- Búsqueda ""pollo"": relevantes=[pechuga, muslos, pollo entero, contramuslos], dudosos=[nuggets, hamburguesa de pollo], excluidos=[caldo de pollo, sopa de pollo]

Responde ÚNICAMENTE con este JSON (sin ningún texto antes ni después):
{{""relevantes"":[índices...],"" dudosos"":[índices...],"" excluidos"":[índices...]}}";

        try
        {
            var (apiKey, available) = _keyRotator.GetNextKey();
            if (!available)
            {
                Console.WriteLine("GroupByRelevance: todas las keys en cooldown, sin filtrar.");
                return (products, new List<Product>(), new List<Product>());
            }

            using var http = new System.Net.Http.HttpClient();
            http.Timeout = TimeSpan.FromSeconds(15);
            http.DefaultRequestHeaders.Add("x-api-key", apiKey);
            http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var body = new
            {
                model = "claude-haiku-4-5-20251001",
                max_tokens = 600,
                system = systemPrompt,
                messages = new[] { new { role = "user", content = userPrompt } }
            };

            var response = await http.PostAsync(
                "https://api.anthropic.com/v1/messages",
                new System.Net.Http.StringContent(
                    System.Text.Json.JsonSerializer.Serialize(body),
                    Encoding.UTF8, "application/json"));

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _keyRotator.MarkRateLimited(apiKey);
                return (products, new List<Product>(), new List<Product>());
            }
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("AI GROUP ERROR: " + response.StatusCode);
                return (products, new List<Product>(), new List<Product>());
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var text = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text").GetString() ?? "{}";

            Console.WriteLine("AI GROUP RESPONSE: " + text);

            // Limpiar si la IA envuelve en ```json ... ```
            var trimmed = text.Trim();
            if (trimmed.StartsWith("```"))
            {
                var s = trimmed.IndexOf('{');
                var e = trimmed.LastIndexOf('}');
                if (s >= 0 && e > s) trimmed = trimmed.Substring(s, e - s + 1);
            }

            using var parsed = System.Text.Json.JsonDocument.Parse(trimmed);
            var root = parsed.RootElement;

            List<int> GetIndices(string key)
            {
                if (!root.TryGetProperty(key, out var el)) return new List<int>();
                return el.EnumerateArray()
                    .Where(x => x.ValueKind == System.Text.Json.JsonValueKind.Number)
                    .Select(x => x.GetInt32())
                    .Where(i => i >= 0 && i < products.Count)
                    .Distinct()
                    .ToList();
            }

            var relevantes = GetIndices("relevantes").Select(i => products[i]).ToList();
            var dudosos    = GetIndices("dudosos").Select(i => products[i]).ToList();
            var excluidos  = GetIndices("excluidos").Select(i => products[i]).ToList();

            // Fallback: si la IA no clasificó nada, devolver todo como relevante
            if (relevantes.Count == 0 && dudosos.Count == 0)
            {
                Console.WriteLine("AI GROUP: clasificación vacía, usando fallback completo.");
                return (products, new List<Product>(), new List<Product>());
            }

            return (relevantes, dudosos, excluidos);
        }
        catch (Exception ex)
        {
            Console.WriteLine("AI GROUP EXCEPTION: " + ex.Message);
            return (products, new List<Product>(), new List<Product>()); // fallback silencioso
        }
    }

    [HttpPost("bestbymarket")]
    public async Task<IActionResult> BestByMarket([FromBody] BestByMarketRequest request)
    {
        // Console.WriteLine($"BESTBYMARKET recibido: term='{request.Term}', productos={request.Products.Count}");
        if (request.Products == null || request.Products.Count == 0)
            return Ok(new List<object>());

        var lines = request.Products.Select((p, i) =>
            $"{i}:{p.Market} | {p.Name} | {p.Price:F2}€ | {p.PriceUnitOrKg}").ToList();
        var productText = string.Join("\n", lines);

        var prompt = $@"Eres un experto comparador de precios de supermercados españoles.
    El usuario buscó: ""{request.Term}""

    Lista de productos por supermercado (índice:supermercado|nombre|precio|precio por unidad):
    {productText}

    Tu tarea:
    1. Para cada supermercado, elige UN solo producto que sea la mejor opción real para alguien que busca ""{request.Term}"":
    - Debe ser el producto MÁS RELEVANTE para la búsqueda (no variantes raras como codorniz si busca huevos)
    - Entre los relevantes, el más barato por unidad (€/kg, €/L, €/docena...). Si no hay precio por unidad, usa el precio total.
    2. Ignora productos claramente irrelevantes o de variantes no buscadas.

    Responde ÚNICAMENTE con un array JSON de índices, uno por supermercado (el mejor de cada uno).
    Ejemplo: [2,15,34,67]
    Sin texto adicional.";

        try
        {
            var (apiKey, available) = _keyRotator.GetNextKey();
            if (!available) return Ok(new List<object>());
            using var http = new System.Net.Http.HttpClient();
            http.Timeout = TimeSpan.FromSeconds(50);
            http.DefaultRequestHeaders.Add("x-api-key", apiKey);
            http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var body = new
            {
                model = "claude-haiku-4-5-20251001",
                max_tokens = 200,
                messages = new[] { new { role = "user", content = prompt } }
            };

            var response = await http.PostAsync(
                "https://api.anthropic.com/v1/messages",
                new System.Net.Http.StringContent(
                    System.Text.Json.JsonSerializer.Serialize(body),
                    Encoding.UTF8, "application/json"));

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _keyRotator.MarkRateLimited(apiKey);
                return Ok(new List<object>());
            }
            if (!response.IsSuccessStatusCode) return Ok(new List<object>());

            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var text = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text").GetString() ?? "[]";

            Console.WriteLine("BEST BY MARKET AI: " + text);

            var indices = System.Text.Json.JsonSerializer.Deserialize<List<int>>(text.Trim());
            if (indices == null) return Ok(new List<object>());

            var result = indices
                .Where(i => i >= 0 && i < request.Products.Count)
                .Select(i => request.Products[i])
                .ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine("BEST BY MARKET ERROR: " + ex.Message);
            return Ok(new List<object>());
        }
    }

    private static string RemoveAccents(string text) // La funció amb la que normalitzem el text, eliminant accents i caràcters especials.
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        return new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }
}