using Salematic.Domain.Models;

namespace Salematic.Domain.Interfaces;

public interface ILlmClient
{
    Task<LlmResponse> EnviarAsync(string systemPrompt, List<LlmMensagem> mensagens, List<LlmFerramenta> ferramentas);
    Task<LlmResponse> EnviarResultadoToolAsync(string systemPrompt, List<LlmMensagem> mensagens, List<LlmFerramenta> ferramentas, string idChamada, string nomeFerramenta, string resultadoJson);
}
