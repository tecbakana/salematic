using MediatR;
using Microsoft.AspNetCore.Mvc;
using Salematic.Application.Commands.Checkout;
using Salematic.Application.DTOs;

namespace Salematic.API.Controllers;

[ApiController]
[Route("api/pedidos")]
public class PedidoController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IConfiguration _config;

    public PedidoController(ISender sender, IConfiguration config)
    {
        _sender = sender;
        _config = config;
    }

    [HttpPost("webhook")]
    public async Task<ActionResult<WebhookPedidoResponse>> Webhook(
        [FromHeader(Name = "X-Webhook-Secret")] string? secret,
        [FromBody] WebhookPedidoRequest request)
    {
        var expectedSecret = _config["Webhook:Secret"];
        if (!string.IsNullOrEmpty(expectedSecret) && secret != expectedSecret)
            return Unauthorized();

        var command = new ProcessarCheckoutCommand(
            request.ClienteId,
            request.MetodoPagamento,
            request.NumeroCartao,
            request.Itens);

        var resultado = await _sender.Send(command);
        return resultado.Sucesso ? Ok(resultado) : BadRequest(resultado);
    }
}
