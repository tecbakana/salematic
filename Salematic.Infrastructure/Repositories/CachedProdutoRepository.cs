using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Salematic.Domain.Entities;
using Salematic.Domain.Interfaces;

namespace Salematic.Infrastructure.Repositories;

public class CachedProdutoRepository : IProdutoRepository
{
    private readonly IProdutoRepository _inner;
    private readonly IDistributedCache _cache;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public CachedProdutoRepository(IProdutoRepository inner, IDistributedCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<IEnumerable<Produto>> BuscarPorNomeAsync(string nome)
    {
        var key = $"produtos:nome:{nome.ToLower()}";
        var cached = await _cache.GetStringAsync(key);
        if (cached != null)
            return JsonSerializer.Deserialize<List<Produto>>(cached)!;

        var result = await _inner.BuscarPorNomeAsync(nome);
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(result),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl });
        return result;
    }

    public async Task<Produto?> BuscarPorIdAsync(int id)
    {
        var key = $"produtos:id:{id}";
        var cached = await _cache.GetStringAsync(key);
        if (cached != null)
            return JsonSerializer.Deserialize<Produto>(cached);

        var result = await _inner.BuscarPorIdAsync(id);
        if (result != null)
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(result),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl });
        return result;
    }

    // Estoque não é cacheado — muda a cada pedido
    public Task<Estoque?> ObterEstoqueAsync(int produtoId) =>
        _inner.ObterEstoqueAsync(produtoId);
}
