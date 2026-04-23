using Salematic.Domain.Entities;

namespace Salematic.Domain.Interfaces;

public interface IProdutoRepository
{
    Task<IEnumerable<Produto>> BuscarPorNomeAsync(string nome);
    Task<Produto?> BuscarPorIdAsync(int id);
    Task<Estoque?> ObterEstoqueAsync(int produtoId);
}

public interface IPedidoRepository
{
    Task<Pedido> CriarPedidoAsync(Pedido pedido);
    Task<Pedido?> BuscarPorIdAsync(int id);
    Task<IEnumerable<Pedido>> BuscarPorClienteAsync(int clienteId);
    Task<bool> AtualizarStatusAsync(int pedidoId, string status, string evento);
}


