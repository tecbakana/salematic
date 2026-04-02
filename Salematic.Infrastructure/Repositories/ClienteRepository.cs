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
}
