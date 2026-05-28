using Salematic.Application.DTOs;
using Salematic.Domain.Entities;
using Salematic.Domain.Interfaces;
using Salematic.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Salematic.Application.Services;

public class PedidoService
{
    private readonly IProdutoRepository _produtos;
    private readonly IPedidoRepository _pedidos;
    private readonly IClienteRepository _clientes;
    private readonly IPagamentoService _pagamento;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<PedidoService> _logger;

    public PedidoService(
        IProdutoRepository produtos,
        IPedidoRepository pedidos,
        IClienteRepository clientes,
        IPagamentoService pagamento,
        IEventPublisher eventPublisher,
        ILogger<PedidoService> logger)
    {
        _produtos = produtos;
        _pedidos = pedidos;
        _clientes = clientes;
        _pagamento = pagamento;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<WebhookPedidoResponse> ProcessarAsync(WebhookPedidoRequest request)
    {
        var cliente = await _clientes.BuscarPorIdAsync(request.ClienteId);
        if (cliente is null)
            return Falhou("Cliente não encontrado.");

        var itens = new List<ItemPedido>();
        decimal total = 0;

        foreach (var itemReq in request.Itens)
        {
            var produto = await _produtos.BuscarPorIdAsync(itemReq.ProdutoId);
            if (produto is null)
                return Falhou($"Produto {itemReq.ProdutoId} não encontrado.");

            var estoque = await _produtos.ObterEstoqueAsync(itemReq.ProdutoId);
            if (estoque is null || estoque.Quantidade < itemReq.Quantidade)
                return Falhou($"Estoque insuficiente para {produto.Nome}.");

            itens.Add(new ItemPedido
            {
                ProdutoId = produto.Id,
                NomeProduto = produto.Nome,
                Quantidade = itemReq.Quantidade,
                PrecoUnitario = produto.Preco
            });
            total += produto.Preco * itemReq.Quantidade;
        }


        //_logger.LogInformation("Criando pedido {Msg}", request.ToString());

        var pedido = await _pedidos.CriarPedidoAsync(new Pedido
        {
            ClienteId = request.ClienteId,
            DataPedido = DateTime.UtcNow,
            Status = "aguardando_pagamento",
            ValorTotal = total,
            Itens = itens
        });

        if(pedido==null)
        {
            _logger.LogInformation("Pedido não foi criado");
            return Falhou("Erro ao criar pedido.");
        }

        var pagamento = await _pagamento.ProcessarAsync(new SolicitacaoPagamento
        {
            PedidoId = pedido.Id,
            Valor = total,
            MetodoPagamento = request.MetodoPagamento,
        });

        if(pagamento is null)
        {
            _logger.LogInformation("Pagamento retornou nulo");
            return Falhou("Erro ao processar pagamento.");
        }

        var status = pagamento.Aprovado ? "confirmado" : "pagamento.recusado";
        var evento = pagamento.Aprovado ? "pedido.confirmado" : "pagamento.recusado";
        var atualizado = await _pedidos.AtualizarStatusAsync(pedido.Id, status,evento);
        if(atualizado)
            await _eventPublisher.PublishAsync(new { evento, PedidoId = pedido.Id, ClienteId = request.ClienteId });

        return new WebhookPedidoResponse
        {
            Sucesso = pagamento.Aprovado,
            Mensagem = pagamento.Mensagem,     
            PedidoId = pedido.Id
        };
    }

    private static WebhookPedidoResponse Falhou(string mensagem) =>
        new() { Sucesso = false, Mensagem = mensagem };
}
