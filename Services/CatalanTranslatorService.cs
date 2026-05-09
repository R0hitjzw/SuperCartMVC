// Services/CatalanTranslatorService.cs
// Traduce términos de búsqueda del español al catalán para mejorar los resultados de Bonpreu.
// Usa Claude Haiku con caché en memoria: la primera búsqueda cuesta ~50 tokens, las siguientes 0.
using System.Collections.Concurrent;
using System.Text;

namespace SuperCartMVC.Services;

public class CatalanTranslatorService
{
    // Caché estática: persiste mientras corre el servidor (comparte entre requests)
    private static readonly ConcurrentDictionary<string, string> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly AnthropicKeyRotator _keyRotator;
    private readonly ILogger<CatalanTranslatorService> _logger;

    public CatalanTranslatorService(AnthropicKeyRotator keyRotator, ILogger<CatalanTranslatorService> logger)
    {
        _keyRotator = keyRotator;
        _logger     = logger;
    }

    /// <summary>
    /// Devuelve el término traducido al catalán.
    /// Si ya está en caché, retorna instantáneamente sin gastar tokens.
    /// Si la API no está disponible, devuelve el término original (fallback silencioso).
    /// </summary>
    public async Task<string> TranslateToCAAsync(string term)
    {
        // 1. Caché — sin tokens
        if (_cache.TryGetValue(term, out var cached))
        {
            _logger.LogInformation("[CatalanTranslator] Caché: '{Term}' → '{Cached}'", term, cached);
            return cached;
        }

        // 2. Llamada a Haiku
        var (apiKey, available) = _keyRotator.GetNextKey();
        if (!available)
        {
            _logger.LogWarning("[CatalanTranslator] Todas las keys en cooldown, usando término original.");
            return term;
        }

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            http.DefaultRequestHeaders.Add("x-api-key", apiKey);
            http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var body = new
            {
                model    = "claude-haiku-4-5-20251001",
                max_tokens = 30,          // el término traducido raramente supera 5-6 palabras
                system   = "Ets un traductor especialitzat en termes de supermercat. " +
                            "Tradueixes de castellà a català. " +
                            "Respons SEMPRE amb la traducció al català, sense cap text addicional.",
                messages = new[] { new { role = "user", content =
                    $"Tradueix al català aquest terme de cerca de supermercat: \"{term}\". " +
                    "Respon només amb la traducció, sense puntuació ni explicacions." } }
            };

            var response = await http.PostAsync(
                "https://api.anthropic.com/v1/messages",
                new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(body),
                    Encoding.UTF8, "application/json"));

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _keyRotator.MarkRateLimited(apiKey);
                return term;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[CatalanTranslator] HTTP {Status}", response.StatusCode);
                return term;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var translated = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text").GetString()?.Trim() ?? term;

            _logger.LogInformation("[CatalanTranslator] '{Term}' → '{Translated}'", term, translated);

            // Guardar en caché para futuras búsquedas
            _cache[term] = translated;
            return translated;
        }
        catch (Exception ex)
        {
            _logger.LogError("[CatalanTranslator] Excepción: {Msg}", ex.Message);
            return term; // fallback silencioso: nunca rompe la búsqueda
        }
    }
}
