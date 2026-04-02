using Salematic.Domain.Models;

namespace Salematic.Application.DTOs;

public class ChatRequest
{
    public string Mensagem { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public List<LlmMensagem> Historico { get; set; } = new();
}

public class ChatResponse
{
    public string Resposta { get; set; } = string.Empty;
    public string? FerramentaUsada { get; set; }
    public List<LlmMensagem> Historico { get; set; } = new();
}
