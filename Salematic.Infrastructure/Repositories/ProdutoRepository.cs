using Dapper;
using Microsoft.Data.SqlClient;
using Salematic.Domain.Entities;
using Salematic.Domain.Interfaces;

namespace Salematic.Infrastructure.Repositories;

public class ProdutoRepository : IProdutoRepository
{
    private readonly string _connectionString;

    public ProdutoRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<Produto>> BuscarPorNomeAsync(string nome)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryAsync<Produto>(
            "SELECT * FROM Produtos WHERE Nome LIKE @Nome AND Ativo = 1",
            new { Nome = $"%{nome}%" });
    }

    public async Task<Produto?> BuscarPorIdAsync(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Produto>(
            "SELECT * FROM Produtos WHERE Id = @Id AND Ativo = 1",
            new { Id = id });
    }

    public async Task<Estoque?> ObterEstoqueAsync(int produtoId)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Estoque>(
            "SELECT * FROM Estoques WHERE ProdutoId = @ProdutoId",
            new { ProdutoId = produtoId });
    }

    public async Task<bool> DebitarEstoqueAsync(int produtoId, int quantidade)
    {
        const string sql = @"UPDATE Estoques 
                                SET Quantidade = Quantidade - @Quantidade 
                                   ,UltimaAtualizacao = GETUTCDATE()
                             WHERE ProdutoId = @ProdutoId 
                               AND Quantidade >= @Quantidade";
        using var conn = new SqlConnection(_connectionString);
        var affected = await conn.ExecuteAsync(sql,
            new { ProdutoId = produtoId, Quantidade = quantidade });
        return affected > 0;
    }
}
