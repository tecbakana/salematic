using System.Text.Json;
using Salematic.Domain.Interfaces;

namespace Salematic.Application.Services;

public class AgentToolsService
{
    private readonly IProdutoRepository _produtos;
    private readonly IPedidoRepository _pedidos;
    private readonly IClienteRepository _clientes;

    public AgentToolsService(
        IProdutoRepository produtos,
        IPedidoRepository pedidos,
        IClienteRepository clientes)
    {
        _produtos = produtos;
        _pedidos = pedidos;
        _clientes = clientes;
    }

    public async Task<string> ExecutarAsync(string nomeFerramenta, Dictionary<string, object> argumentos)
    {
        return nomeFerramenta switch
        {
            "consultar_estoque" => await ConsultarEstoqueAsync(argumentos),
            "registrar_pedido" => await RegistrarPedidoAsync(argumentos),
            "consultar_pedidos" => await ConsultarPedidosAsync(argumentos),
            "cancelar_pedido" => await CancelarPedidoAsync(argumentos),
            _ => JsonSerializer.Serialize(new { erro = $"Ferramenta desconhecida: {nomeFerramenta}" })
        };
    }

    private async Task<string> ConsultarEstoqueAsync(Dictionary<string, object> args)
    {
        var nomeProduto = args["nome_produto"]?.ToString() ?? string.Empty;
        var produtos = await _produtos.BuscarPorNomeAsync(nomeProduto);

        var resultado = new List<object>();
        foreach (var p in produtos)
        {
            var estoque = await _produtos.ObterEstoqueAsync(p.Id);
            resultado.Add(new
            {
                id = p.Id,
                nome = p.Nome,
                descricao = p.Descricao,
                preco = p.Preco,
                unidade = p.UnidadeMedida,
                quantidade_em_estoque = estoque?.Quantidade ?? 0
            });
        }

        return resultado.Count > 0
            ? JsonSerializer.Serialize(resultado)
            : JsonSerializer.Serialize(new { mensagem = "Nenhum produto encontrado." });
    }

    private async Task<string> RegistrarPedidoAsync(Dictionary<string, object> args)
    {
        var clienteId = Convert.ToInt32(args["cliente_id"]);
        var cliente = await _clientes.BuscarPorIdAsync(clienteId);
        if (cliente is null)
            return JsonSerializer.Serialize(new { erro = "Cliente não encontrado." });

        var itensJson = args["itens"]?.ToString() ?? "[]";
        var itensReq = JsonSerializer.Deserialize<List<ItemPedidoRequest>>(itensJson);
        if (itensReq is null || itensReq.Count == 0)
            return JsonSerializer.Serialize(new { erro = "Nenhum item informado." });

        var itensPedido = new List<Domain.Entities.ItemPedido>();
        decimal total = 0;

        foreach (var item in itensReq)
        {
            var produto = await _produtos.BuscarPorIdAsync(item.ProdutoId);
            if (produto is null)
                return JsonSerializer.Serialize(new { erro = $"Produto {item.ProdutoId} não encontrado." });

            var estoque = await _produtos.ObterEstoqueAsync(item.ProdutoId);
            if (estoque is null || estoque.Quantidade < item.Quantidade)
                return JsonSerializer.Serialize(new { erro = $"Estoque insuficiente para {produto.Nome}. Disponível: {estoque?.Quantidade ?? 0}." });

            itensPedido.Add(new Domain.Entities.ItemPedido
            {
                ProdutoId = produto.Id,
                NomeProduto = produto.Nome,
                Quantidade = item.Quantidade,
                PrecoUnitario = produto.Preco
            });
            total += produto.Preco * item.Quantidade;
        }

        var pedido = new Domain.Entities.Pedido
        {
            ClienteId = clienteId,
            DataPedido = DateTime.UtcNow,
            Status = "aguardando_pagamento",
            ValorTotal = total,
            Itens = itensPedido
        };

        var pedidoCriado = await _pedidos.CriarPedidoAsync(pedido);
        return JsonSerializer.Serialize(new
        {
            pedido_id = pedidoCriado.Id,
            valor_total = pedidoCriado.ValorTotal,
            status = pedidoCriado.Status,
            mensagem = "Pedido registrado com sucesso."
        });
    }

    private async Task<string> ConsultarPedidosAsync(Dictionary<string, object> args)
    {
        var clienteId = Convert.ToInt32(args["cliente_id"]);
        var pedidos = await _pedidos.BuscarPorClienteAsync(clienteId);

        var resultado = pedidos.Select(p => new
        {
            id = p.Id,
            data = p.DataPedido,
            status = p.Status,
            valor_total = p.ValorTotal,
            itens = p.Itens.Select(i => new { i.NomeProduto, i.Quantidade, i.PrecoUnitario })
        });

        return JsonSerializer.Serialize(resultado);
    }

    private async Task<string> CancelarPedidoAsync(Dictionary<string, object> args)
    {
        var pedidoId = Convert.ToInt32(args["pedido_id"]);
        var pedido = await _pedidos.BuscarPorIdAsync(pedidoId);

        if (pedido is null)
            return JsonSerializer.Serialize(new { erro = "Pedido não encontrado." });

        if (pedido.Status == "cancelado")
            return JsonSerializer.Serialize(new { mensagem = "Pedido já estava cancelado." });

        await _pedidos.AtualizarStatusAsync(pedidoId, "cancelado");
        return JsonSerializer.Serialize(new { mensagem = $"Pedido {pedidoId} cancelado com sucesso." });
    }

    private record ItemPedidoRequest(int ProdutoId, int Quantidade);
}
