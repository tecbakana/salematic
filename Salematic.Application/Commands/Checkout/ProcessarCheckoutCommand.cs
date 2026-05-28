using MediatR;
using Salematic.Application.DTOs;

namespace Salematic.Application.Commands.Checkout;

public record ProcessarCheckoutCommand(
    int ClienteId,
    string MetodoPagamento,
    string? NumeroCartao,
    List<WebhookItemRequest> Itens
) : IRequest<WebhookPedidoResponse>;