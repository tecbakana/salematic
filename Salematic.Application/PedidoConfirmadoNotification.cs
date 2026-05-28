using MediatR;

namespace Salematic.Application.Notifications.Checkout;

public record PedidoConfirmadoNotification(
    int PedidoId,
    int ClienteId,
    decimal ValorTotal
) : INotification;