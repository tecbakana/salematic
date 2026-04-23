using Microsoft.AspNetCore.Mvc;
using Salematic.Application.Services;
using Salematic.Application.DTOs;
using Salematic.Domain.Models;
using System.Diagnostics.Contracts;
using Salematic.Domain.Interfaces; 

namespace Salematic.API.Controllers
{
    [ApiController]
    [Route("api/mock/pagamento")]
    public class MockConfigController : ControllerBase
    {
        private readonly IMockPaymentConfigStore _mockPaymentConfigStore;

        public MockConfigController(IMockPaymentConfigStore mockPaymentConfigService)
        {
            _mockPaymentConfigStore = mockPaymentConfigService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MockPaymentConfig>>> GetAllAsync()
        {
            var configs = await _mockPaymentConfigStore.GetAllAsync();
            return Ok(configs);
        }

        [HttpPut("{methodId}")]
        public async Task<ActionResult> UpdateAsync(string methodId, [FromBody] MockPaymentConfig config)
        {
            var existingConfigs = await _mockPaymentConfigStore.GetAllAsync();
            if (!existingConfigs.Any(c => c.Id == methodId))
            {
                return NotFound($"Método de pagamento '{methodId}' não encontrado.");
            }
            await _mockPaymentConfigStore.UpdateAsync(methodId, config);
            return NoContent();
        }
    }
}
