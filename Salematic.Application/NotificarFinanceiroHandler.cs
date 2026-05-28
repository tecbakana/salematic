using MediatR;
using Microsoft.Extensions.Logging;

namespace Salematic.Application.Notifications.Checkout;

public class NotificarFinanceiroHandler : INotificationHandler<PedidoConfirmadoNotification>
{
    private readonly ILogger<NotificarFinanceiroHandler> _logger;

    public NotificarFinanceiroHandler(ILogger<NotificarFinanceiroHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PedidoConfirmadoNotification notification, CancellationToken cancellationToken)
    {
        // Scaffold para NF-e e conciliação financeira
        // Futuramente: integrar com serviço fiscal e sistema de conciliação
        _logger.LogInformation(
            "[Financeiro] Pedido {PedidoId} confirmado. Cliente {ClienteId}. Valor R$ {Valor:F2}. Aguardando emissão NF-e.",
            notification.PedidoId,
            notification.ClienteId,
            notification.ValorTotal);

        return Task.CompletedTask;
    }
}