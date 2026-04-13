using Dapper;
using Microsoft.Data.SqlClient;
using Salematic.Domain.Entities;
using Salematic.Domain.Interfaces;

namespace Salematic.Infrastructure.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly string _connectionString;

    public ClienteRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Cliente?> BuscarPorIdAsync(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Cliente>(
            "SELECT * FROM Clientes WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<Cliente?> BuscarPorDocumentoAsync(string documento)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Cliente>(
            "SELECT * FROM Clientes WHERE Documento = @Documento",
            new { Documento = documento });
    }

    public async Task<Cliente> CriarClienteAsync(Cliente cliente)
    {
        using var conn = new SqlConnection(_connectionString);
        var id = await conn.ExecuteScalarAsync<int>(
            "INSERT INTO Clientes (Nome, Documento, Email, Telefone) OUTPUT INSERTED.Id VALUES (@Nome, @Documento, @Email, @Telefone)",
            new { cliente.Nome, cliente.Documento, cliente.Email, cliente.Telefone });
        cliente.Id = id;
        return cliente;
    }

    public async Task AtualizarEnderecoAsync(int id, string cep, string logradouro, string numero, string complemento, string bairro, string cidade, string estado)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(
            "UPDATE Clientes SET Cep = @Cep, Logradouro = @Logradouro, Numero = @Numero, Complemento = @Complemento, Bairro = @Bairro, Cidade = @Cidade, Estado = @Estado WHERE Id = @Id",
            new { Id = id, Cep = cep, Logradouro = logradouro, Numero = numero, Complemento = complemento, Bairro = bairro, Cidade = cidade, Estado = estado });
    }

    public async Task AtualizarClienteAsync(int id, string nome, string documento, string email, string telefone)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(
            "UPDATE Clientes SET Nome = @Nome, Documento = @Documento, Email = @Email, Telefone = @Telefone WHERE Id = @Id",
            new { Id = id, Nome = nome, Documento = documento, Email = email, Telefone = telefone });
    }

    public async Task<Cliente?> BuscarPorEmailAsync(string email)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Cliente>(
            "SELECT * FROM Clientes WHERE Email = @Email",
            new { Email = email });
    }
}
