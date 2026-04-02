using Microsoft.AspNetCore.Mvc;
using Salematic.Application.DTOs;
using Salematic.Application.Services;

namespace Salematic.API.Controllers;

[ApiController]
[Route("api/pedidos")]
public class PedidoController : ControllerBase
{
    private readonly PedidoService _pedidoService;
    private readonly IConfiguration _config;

    public PedidoController(PedidoService pedidoService, IConfiguration config)
    {
        _pedidoService = pedidoService;
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

        var resultado = await _pedidoService.ProcessarAsync(request);
        return resultado.Sucesso ? Ok(resultado) : BadRequest(resultado);
    }
}
