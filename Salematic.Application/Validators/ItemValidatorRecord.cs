using MediatR;
using Salematic.Application.DTOs;

namespace Salematic.Application.Validators;

public record ItemValidatorRecord(
    int ClienteId,
    string MetodoPagamento,
    string? NumeroCartao,
    List<WebhookItemRequest> Itens
) : IRequest<WebhookPedidoResponse>;