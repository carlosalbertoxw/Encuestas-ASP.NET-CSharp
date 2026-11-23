using Encuestas.Web.Services;

namespace Encuestas.Tests;

/// <summary>Almacén de contadores en memoria para las pruebas unitarias (sin MySQL).</summary>
public sealed class InMemoryAttemptCounterStore : IAttemptCounterStore
{
    private readonly Dictionary<string, (int Count, DateTime WindowStart)> _counters = [];

    /// <summary>Instante actual; las pruebas lo adelantan para simular que la ventana caduca.</summary>
    public DateTime Now { get; set; } = DateTime.UtcNow;

    public Task<int> CountAsync(string key, TimeSpan window) =>
        Task.FromResult(_counters.TryGetValue(key, out var entry) && entry.WindowStart > Now - window ? entry.Count : 0);

    public Task IncrementAsync(string key, TimeSpan window)
    {
        _counters[key] = _counters.TryGetValue(key, out var entry) && entry.WindowStart > Now - window
            ? (entry.Count + 1, entry.WindowStart)
            : (1, Now);
        return Task.CompletedTask;
    }

    public Task ResetAsync(string key)
    {
        _counters.Remove(key);
        return Task.CompletedTask;
    }
}
