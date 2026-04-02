using System.Text.Json;
using Salematic.Application.DTOs;
using Salematic.Domain.Interfaces;
using Salematic.Domain.Models;

namespace Salematic.Application.Services;

public class ChatService
{
    private readonly ILlmClient _llm;
    private readonly AgentToolsService _tools;
    private readonly string _systemPrompt;

    public ChatService(ILlmClient llm, AgentToolsService tools, string systemPrompt)
    {
        _llm = llm;
        _tools = tools;
        _systemPrompt = systemPrompt;
    }

    public async Task<ChatResponse> ProcessarAsync(ChatRequest request)
    {
        var historico = new List<LlmMensagem>(request.Historico)
        {
            new() { Role = "user", Content = request.Mensagem }
        };

        var ferramentas = DefinirFerramentas();
        string? ferramentaUsada = null;

        while (true)
        {
            var resposta = await _llm.EnviarAsync(_systemPrompt, historico, ferramentas);

            if (resposta.TipoResposta == "text")
            {
                historico.Add(new() { Role = "assistant", Content = resposta.TextoResposta! });
                return new ChatResponse
                {
                    Resposta = resposta.TextoResposta!,
                    FerramentaUsada = ferramentaUsada,
                    Historico = historico
                };
            }

            if (resposta.TipoResposta == "tool_use")
            {
                ferramentaUsada = resposta.NomeFerramenta;
                var resultadoTool = await _tools.ExecutarAsync(resposta.NomeFerramenta!, resposta.Argumentos!);

                var respostaContinuacao = await _llm.EnviarResultadoToolAsync(
                    _systemPrompt, historico, ferramentas, resposta.IdChamada!, resposta.NomeFerramenta!, resultadoTool);

                if (respostaContinuacao.TipoResposta == "text")
                {
                    historico.Add(new() { Role = "assistant", Content = respostaContinuacao.TextoResposta! });
                    return new ChatResponse
                    {
                        Resposta = respostaContinuacao.TextoResposta!,
                        FerramentaUsada = ferramentaUsada,
                        Historico = historico
                    };
                }
            }
        }
    }

    private static List<LlmFerramenta> DefinirFerramentas() =>
    [
        new()
        {
            Nome = "consultar_estoque",
            Descricao = "Consulta produtos disponíveis e quantidade em estoque pelo nome.",
            Parametros = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["nome_produto"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Nome ou parte do nome do produto a buscar"
                    }
                }
            },
            Obrigatorios = ["nome_produto"]
        },
        new()
        {
            Nome = "registrar_pedido",
            Descricao = "Registra um novo pedido para o cliente com os itens informados.",
            Parametros = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["cliente_id"] = new Dictionary<string, object>
                    {
                        ["type"] = "integer",
                        ["description"] = "ID do cliente"
                    },
                    ["itens"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "JSON com lista de itens: [{\"ProdutoId\": 1, \"Quantidade\": 2}]"
                    }
                }
            },
            Obrigatorios = ["cliente_id", "itens"]
        },
        new()
        {
            Nome = "consultar_pedidos",
            Descricao = "Lista os pedidos de um cliente.",
            Parametros = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["cliente_id"] = new Dictionary<string, object>
                    {
                        ["type"] = "integer",
                        ["description"] = "ID do cliente"
                    }
                }
            },
            Obrigatorios = ["cliente_id"]
        },
        new()
        {
            Nome = "cancelar_pedido",
            Descricao = "Cancela um pedido existente pelo ID.",
            Parametros = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["pedido_id"] = new Dictionary<string, object>
                    {
                        ["type"] = "integer",
                        ["description"] = "ID do pedido a cancelar"
                    }
                }
            },
            Obrigatorios = ["pedido_id"]
        }
    ];
}
