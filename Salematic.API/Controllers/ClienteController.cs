using Microsoft.AspNetCore.Mvc;
using Salematic.Application.Services;
using Salematic.Application.DTOs;
using Salematic.Domain.Models;

namespace Salematic.API.Controllers
{
    [ApiController]
    [Route("api/clientes")]
    public class ClienteController : ControllerBase
    {
        private readonly ClienteService _clienteService;
        public ClienteController(ClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        [HttpPost]
        public async Task<ActionResult<ClienteModel>> Post([FromBody] CriarClienteRequest request)
        {
            var clienteCriado = await _clienteService.ProcessarAsync(request);
            return Ok(clienteCriado);
        }

        [HttpGet]
        public async Task<ActionResult<ClienteModel?>> Get(int id)
        {
            // Lógica para obter um cliente pelo ID
            var cliente = await _clienteService.ObterPorIdAsync(id);
            return Ok(cliente);
        }
    }
}
