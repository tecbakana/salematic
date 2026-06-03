
namespace Salematic.Domain.Interfaces;
public interface IStockLockService
{
    /// <summary>
    /// Tenta adquirir lock esclusivo sobre o estoque do produto.
    /// Retorna IAsyncdisposable que libera o lock ao ser descartado.
    /// Retorna null se não conseguir adquirir o lock após retries.
    Task<IAsyncDisposable?> AcquireAsync(int produtoId, CancellationToken ct = default);
}

