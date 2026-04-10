using System.Text.Json;

namespace Salematic.Domain.Models;

public class LlmMensagem
{
    public string Role { get; set; } = string.Empty; // "user" | "assistant"
    public string Content { get; set; } = string.Empty;
}

public class LlmFerramenta
{
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public Dictionary<string, object> Parametros { get; set; } = new();
    public List<string> Obrigatorios { get; set; } = new();
}

public class LlmResponse
{
    public string TipoResposta { get; set; } = string.Empty; // "text" | "tool_use"
    public string? TextoResposta { get; set; }
    public string? NomeFerramenta { get; set; }
    public string? IdChamada { get; set; }
    public Dictionary<string, JsonElement>? Argumentos { get; set; }
}
