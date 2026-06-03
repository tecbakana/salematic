using MediatR;
using Salematic.Application.DTOs;
using Salematic.Domain.Entities;
using Salematic.Domain.Interfaces;
using Salematic.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Salematic.Application.Commands.Checkout;

public class ProcessarCheckoutCommandHandler
    : IRequestHandler<ProcessarCheckoutCommand, WebhookPedidoResponse>
{
    private readonly IProdutoRepository _produtos;
    private readonly IPedidoRepository _pedidos;
    private readonly IClienteRepository _clientes;
    private readonly IPagamentoService _pagamento;
    private readonly IPublisher _publisher;
    private readonly ILogger<ProcessarCheckoutCommandHandler> _logger;
    private readonly IStockLockService _stockLock;

    public ProcessarCheckoutCommandHandler(
        IProdutoRepository produtos,
        IPedidoRepository pedidos,
        IClienteRepository clientes,
        IPagamentoService pagamento,
        IPublisher publisher,
        ILogger<ProcessarCheckoutCommandHandler> logger,
        IStockLockService stockLock)
    {
        _produtos = produtos;
        _pedidos = pedidos;
        _clientes = clientes;
        _pagamento = pagamento;
        _publisher = publisher;
        _logger = logger;
        _stockLock = stockLock;
    }

    public async Task<WebhookPedidoResponse> Handle(
        ProcessarCheckoutCommand command, CancellationToken cancellationToken)
    {
        // 1. Validar cliente
        var cliente = await _clientes.BuscarPorIdAsync(command.ClienteId);
        if (cliente is null)
            return Falhou("Cliente não encontrado.");

        // Ordenar por ProdutoId evita deadlock entre requisições concorrentes
        var produtoIds = command.Itens.Select(i => i.ProdutoId).Distinct().OrderBy(id => id).ToList();
        var locks = new List<IAsyncDisposable>();

        try
        {
            foreach (var produtoId in produtoIds)
            {
                var lck = await _stockLock.AcquireAsync(produtoId, cancellationToken);
                if (lck is null)
                {
                    return Falhou("Estoque temporariamente indisponível. Tente novamente em instantes.");
                }
                locks.Add(lck);
            }

            // ... resto do Handle (verificação, pagamento, debit, pedido, notificação)


            // 2. Verificar estoque (fail-fast, sem debitar ainda)
            var itens = new List<ItemPedido>();
            decimal total = 0;
            foreach (var itemReq in command.Itens)
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

            // 3. Processar pagamento no gateway (externo — fora da transação de banco)
            var pagamento = await _pagamento.ProcessarAsync(new SolicitacaoPagamento
            {
                Valor = total,
                MetodoPagamento = command.MetodoPagamento
            });
            if (pagamento is null)
                return Falhou("Erro ao processar pagamento.");

            if (!pagamento.Aprovado)
                return Falhou(pagamento.Mensagem);

            // 4. Debitar estoque + salvar pedido (transação atômica)
            foreach (var item in itens)
            {
                var debitado = await _produtos.DebitarEstoqueAsync(item.ProdutoId, item.Quantidade);
                if (!debitado)
                    return Falhou($"Falha ao debitar estoque de {item.NomeProduto}. Possível race condition.");
            }

            var pedido = await _pedidos.CriarPedidoAsync(new Pedido
            {
                ClienteId = command.ClienteId,
                DataPedido = DateTime.UtcNow,
                Status = "confirmado",
                Evento = "pedido.confirmado",
                ValorTotal = total,
                Itens = itens
            });

            if (pedido is null)
            {
                _logger.LogError("Pedido não foi criado após pagamento aprovado. ClienteId={ClienteId}", command.ClienteId);
                return Falhou("Erro ao registrar pedido.");
            }

            // 5. Publicar notificação (in-process, assíncrono — handlers reagem após commit)
            await _publisher.Publish(
                new Notifications.Checkout.PedidoConfirmadoNotification(pedido.Id, command.ClienteId, total),
                cancellationToken);

            return new WebhookPedidoResponse
            {
                Sucesso = true,
                Mensagem = pagamento.Mensagem,
                PedidoId = pedido.Id
            };
        }
        finally
        {
            foreach (var lck in locks)
                await lck.DisposeAsync();
        }
    }

    private static WebhookPedidoResponse Falhou(string mensagem) =>
        new() { Sucesso = false, Mensagem = mensagem };
}