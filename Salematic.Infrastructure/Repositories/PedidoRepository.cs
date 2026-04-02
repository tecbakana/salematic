using Dapper;
using Microsoft.Data.SqlClient;
using Salematic.Domain.Entities;
using Salematic.Domain.Interfaces;

namespace Salematic.Infrastructure.Repositories;

public class PedidoRepository : IPedidoRepository
{
    private readonly string _connectionString;

    public PedidoRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Pedido> CriarPedidoAsync(Pedido pedido)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        pedido.Id = await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO Pedidos (ClienteId, DataPedido, Status, ValorTotal)
              VALUES (@ClienteId, @DataPedido, @Status, @ValorTotal);
              SELECT SCOPE_IDENTITY();",
            pedido, tx);

        foreach (var item in pedido.Itens)
        {
            item.PedidoId = pedido.Id;
            await conn.ExecuteAsync(
                @"INSERT INTO ItensPedido (PedidoId, ProdutoId, NomeProduto, Quantidade, PrecoUnitario)
                  VALUES (@PedidoId, @ProdutoId, @NomeProduto, @Quantidade, @PrecoUnitario);
                  UPDATE Estoques SET Quantidade = Quantidade - @Quantidade, UltimaAtualizacao = GETUTCDATE()
                  WHERE ProdutoId = @ProdutoId;",
                item, tx);
        }

        tx.Commit();
        return pedido;
    }

    public async Task<Pedido?> BuscarPorIdAsync(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        var pedidos = await conn.QueryAsync<Pedido, ItemPedido, Pedido>(
            @"SELECT p.*, i.*
              FROM Pedidos p
              LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
              WHERE p.Id = @Id",
            (pedido, item) => { if (item != null) pedido.Itens.Add(item); return pedido; },
            new { Id = id }, splitOn: "Id");

        return pedidos.FirstOrDefault();
    }

    public async Task<IEnumerable<Pedido>> BuscarPorClienteAsync(int clienteId)
    {
        using var conn = new SqlConnection(_connectionString);
        var lookup = new Dictionary<int, Pedido>();

        await conn.QueryAsync<Pedido, ItemPedido, Pedido>(
            @"SELECT p.*, i.*
              FROM Pedidos p
              LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
              WHERE p.ClienteId = @ClienteId
              ORDER BY p.DataPedido DESC",
            (pedido, item) =>
            {
                if (!lookup.TryGetValue(pedido.Id, out var p))
                    lookup[pedido.Id] = p = pedido;
                if (item != null) p.Itens.Add(item);
                return p;
            },
            new { ClienteId = clienteId }, splitOn: "Id");

        return lookup.Values;
    }

    public async Task AtualizarStatusAsync(int pedidoId, string status)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(
            "UPDATE Pedidos SET Status = @Status WHERE Id = @Id",
            new { Status = status, Id = pedidoId });
    }
}
