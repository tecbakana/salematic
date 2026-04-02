using Microsoft.AspNetCore.Mvc;
using Salematic.Application.DTOs;
using Salematic.Application.Services;

namespace Salematic.API.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;

    public ChatController(ChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost("mensagem")]
    public async Task<ActionResult<ChatResponse>> Mensagem([FromBody] ChatRequest request)
    {
        var resposta = await _chatService.ProcessarAsync(request);
        return Ok(resposta);
    }
}
