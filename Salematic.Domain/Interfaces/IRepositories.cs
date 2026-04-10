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
    Task AtualizarStatusAsync(int pedidoId, string status);
}

public interface IClienteRepository
{
    Task<Cliente?> BuscarPorIdAsync(int id);
    Task<Cliente?> BuscarPorDocumentoAsync(string documento);
    Task<Cliente> CriarClienteAsync(Cliente cliente);
    Task AtualizarEnderecoAsync(int id, string cep, string logradouro, string numero, string complemento, string bairro, string cidade, string estado);
    Task AtualizarClienteAsync(int id, string nome, string documento, string email, string telefone);
}
