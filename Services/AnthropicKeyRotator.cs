namespace SuperCartMVC.Services;

public class AnthropicKeyRotator
{
    private readonly string[] _keys;
    private readonly DateTime[] _cooldownUntil;
    private int _current = 0;
    private readonly object _lock = new();

    public AnthropicKeyRotator(IConfiguration config)
    {
        var keys = new List<string>();

        var k1 = config["Anthropic:ApiKey"];
        var k2 = config["Anthropic:ApiKey2"];
        var k3 = config["Anthropic:ApiKey3"];
        var k4 = config["Anthropic:ApiKey4"];

        if (!string.IsNullOrWhiteSpace(k1)) keys.Add(k1);
        if (!string.IsNullOrWhiteSpace(k2)) keys.Add(k2);
        if (!string.IsNullOrWhiteSpace(k3)) keys.Add(k3);
        if (!string.IsNullOrWhiteSpace(k4)) keys.Add(k4);

        if (keys.Count == 0) throw new Exception("No hay ninguna Anthropic API key configurada.");

        _keys = keys.ToArray();
        _cooldownUntil = new DateTime[_keys.Length];

        Console.WriteLine($"[KeyRotator] {_keys.Length} key(s) cargadas.");
    }

    public void MarkRateLimited(string key)
    {
        lock (_lock)
        {
            var idx = Array.IndexOf(_keys, key);
            if (idx >= 0)
            {
                _cooldownUntil[idx] = DateTime.UtcNow.AddSeconds(65);
                Console.WriteLine($"[KeyRotator] Key #{idx + 1} en cooldown 65s por rate limit.");
            }
        }
    }

    public (string Key, bool Available) GetNextKey()
    {
        lock (_lock)
        {
            for (int i = 0; i < _keys.Length; i++)
            {
                var idx = (_current + i) % _keys.Length;
                if (DateTime.UtcNow >= _cooldownUntil[idx])
                {
                    _current = (idx + 1) % _keys.Length;
                    Console.WriteLine($"[KeyRotator] Usando key #{idx + 1}");
                    return (_keys[idx], true);
                }
            }
            Console.WriteLine("[KeyRotator] Todas las keys en cooldown.");
            return (string.Empty, false);
        }
    }
}