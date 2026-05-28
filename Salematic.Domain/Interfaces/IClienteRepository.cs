using Salematic.Domain.Models;

namespace Salematic.Domain.Interfaces
{
    public interface IClienteRepository
    {
        Task<ClienteModel?> BuscarPorIdAsync(int id);
        Task<ClienteModel?> BuscarPorDocumentoAsync(string documento);
        Task<ClienteModel> ProcessarAsync(ClienteModel cliente);
        Task AtualizarEnderecoAsync(int id, string cep, string logradouro, string numero, string complemento, string bairro, string cidade, string estado);
        Task AtualizarClienteAsync(int id, string nome, string documento, string email, string telefone);
        Task<ClienteModel?> BuscarPorEmailAsync(string email);
        Task SalvarResetTokenAsync(int clienteId, string token, DateTime expiry);
        Task<ClienteModel?> BuscarPorResetTokenAsync(string token);
        Task AtualizarSenhaAsync(int clienteId, string senhaHash);
    }
}
