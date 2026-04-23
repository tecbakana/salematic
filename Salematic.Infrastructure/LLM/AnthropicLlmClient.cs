using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic.SDK;
using Anthropic.SDK.Common;
using Anthropic.SDK.Messaging;
using Salematic.Domain.Interfaces;
using Salematic.Domain.Models;

namespace Salematic.Infrastructure.LLM;

public class AnthropicLlmClient : ILlmClient
{
    private readonly AnthropicClient _client;
    private readonly string _model;

    public AnthropicLlmClient(string apiKey, string model)
    {
        _client = new AnthropicClient(apiKey);
        _model = model;
    }

    public async Task<LlmResponse> EnviarAsync(string systemPrompt, List<LlmMensagem> mensagens, List<LlmFerramenta> ferramentas)
    {
        var response = await _client.Messages.GetClaudeMessageAsync(new MessageParameters
        {
            Model = _model,
            MaxTokens = 1024,
            System = [new SystemMessage(systemPrompt)],
            Messages = TraduzirMensagens(mensagens),
            Tools = TraduzirFerramentas(ferramentas)
        });

        return TraduzirResposta(response);
    }

    public async Task<LlmResponse> EnviarResultadoToolAsync(
        string systemPrompt, List<LlmMensagem> mensagens, List<LlmFerramenta> ferramentas,
        string idChamada, string nomeFerramenta, string resultadoJson)
    {
        var msgs = TraduzirMensagens(mensagens);
        msgs.Add(new Message
        {
            Role = RoleType.User,
            Content = new List<ContentBase>
            {
                new ToolResultContent
                {
                    ToolUseId = idChamada,
                    Content = new List<ContentBase> { new TextContent { Text = resultadoJson } }
                }
            }
        });

        var response = await _client.Messages.GetClaudeMessageAsync(new MessageParameters
        {
            Model = _model,
            MaxTokens = 1024,
            System = [new SystemMessage(systemPrompt)],
            Messages = msgs,
            Tools = TraduzirFerramentas(ferramentas)
        });

        return TraduzirResposta(response);
    }

    private static List<Message> TraduzirMensagens(List<LlmMensagem> mensagens) =>
        mensagens.Select(m => new Message
        {
            Role = m.Role == "user" ? RoleType.User : RoleType.Assistant,
            Content = new List<ContentBase> { new TextContent { Text = m.Content } }
        }).ToList();

    private static IList<Anthropic.SDK.Common.Tool> TraduzirFerramentas(List<LlmFerramenta> ferramentas) =>
        ferramentas.Select(f => (Anthropic.SDK.Common.Tool)new Function(
            f.Nome,
            f.Descricao,
            JsonNode.Parse(JsonSerializer.Serialize(new
            {
                type = "object",
                properties = f.Parametros,
                required = f.Obrigatorios
            }))
        )).ToList();

    private static LlmResponse TraduzirResposta(MessageResponse response)
    {
        if (response.StopReason == "tool_use")
        {
            var toolUse = response.Content.OfType<ToolUseContent>().First();
            return new LlmResponse
            {
                TipoResposta = "tool_use",
                NomeFerramenta = toolUse.Name,
                IdChamada = toolUse.Id,
                Argumentos = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(toolUse.Input.ToJsonString())
            };
        }

        return new LlmResponse
        {
            TipoResposta = "text",
            TextoResposta = response.Content.OfType<TextContent>().First().Text
        };
    }
}
