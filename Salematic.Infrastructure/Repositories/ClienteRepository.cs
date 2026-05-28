using Dapper;
using Microsoft.Data.SqlClient;
using Salematic.Domain.Models;
using Salematic.Domain.Interfaces;

namespace Salematic.Infrastructure.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly string _connectionString;

    public ClienteRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ClienteModel?> BuscarPorIdAsync(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<ClienteModel>(
            "SELECT SalematicClienteId, AplicacaoId, Nome, Documento, Email, Telefone, Cep, Logradouro, Numero, Complemento, Bairro, Cidade, Estado FROM Clientes WHERE SalematicClienteId = @Id",
            new { Id = id });
    }

    public async Task<ClienteModel?> BuscarPorDocumentoAsync(string documento)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<ClienteModel>(
            "SELECT Id AS SalematicClienteId, AplicacaoId, Nome, Documento, Email, Telefone, Cep, Logradouro, Numero, Complemento, Bairro, Cidade, Estado FROM Clientes WHERE Documento = @Documento",
            new { Documento = documento });
    }

    public async Task<ClienteModel> ProcessarAsync(ClienteModel cliente)
    {
        using var conn = new SqlConnection(_connectionString);
        var id = await conn.ExecuteScalarAsync<int>(
            "INSERT INTO Clientes (AplicacaoId, Nome, Documento, Email, Telefone, Cep, Logradouro, Numero, Complemento, Bairro, Cidade, Estado, SenhaHash) OUTPUT INSERTED.SalematicClienteId VALUES (@AplicacaoId, @Nome, @Documento, @Email, @Telefone, @Cep, @Logradouro, @Numero, @Complemento, @Bairro, @Cidade, @Estado, @SenhaHash)",
            new { cliente.AplicacaoId, cliente.Nome, cliente.Documento, cliente.Email, cliente.Telefone, cliente.Cep, cliente.Logradouro, cliente.Numero, cliente.Complemento, cliente.Bairro, cliente.Cidade, cliente.Estado, cliente.SenhaHash });
        cliente.SalematicClienteId = id;
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

    public async Task<ClienteModel?> BuscarPorEmailAsync(string email)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<ClienteModel>(
            "SELECT SalematicClienteId, AplicacaoId, Nome, Documento, Email, Telefone, Cep, Logradouro, Numero, Complemento, Bairro, Cidade, Estado, SenhaHash FROM Clientes WHERE Email = @Email",
            new { Email = email });
    }

    public async Task SalvarResetTokenAsync(int clienteId, string token, DateTime expiry)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(
            "UPDATE Clientes SET ResetPasswordToken = @Token, ResetPasswordExpiry = @Expiry WHERE SalematicClienteId = @Id",
            new { Id = clienteId, Token = token, Expiry = expiry });
    }

    public async Task<ClienteModel?> BuscarPorResetTokenAsync(string token)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<ClienteModel>(
            "SELECT SalematicClienteId, AplicacaoId, Nome, Documento, Email, Telefone, ResetPasswordToken, ResetPasswordExpiry FROM Clientes WHERE ResetPasswordToken = @Token",
            new { Token = token });
    }

    public async Task AtualizarSenhaAsync(int clienteId, string senhaHash)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(
            "UPDATE Clientes SET SenhaHash = @SenhaHash, ResetPasswordToken = NULL, ResetPasswordExpiry = NULL WHERE SalematicClienteId = @Id",
            new { Id = clienteId, SenhaHash = senhaHash });
    }
}
