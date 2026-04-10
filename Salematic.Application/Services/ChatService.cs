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
    private readonly bool _isDevelopment;

    public ChatService(ILlmClient llm, AgentToolsService tools, string systemPrompt, bool isDevelopment)
    {
        _llm = llm;
        _tools = tools;
        _systemPrompt = systemPrompt;
        _isDevelopment = isDevelopment;
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

    private List<LlmFerramenta> DefinirFerramentas()
    {
        var ferramentas = new List<LlmFerramenta>
        {
        new()
        {
            Nome = "consultar_estoque",
            Descricao = "Consulta produtos disponíveis, preço e quantidade em estoque. Aceita um termo ou uma lista de termos para busca ampliada (ex: ['teclado', 'mecânico']).",
            Parametros = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["nome_produto"] = new Dictionary<string, object>
                    {
                        ["type"] = "array",
                        ["items"] = new Dictionary<string, object> { ["type"] = "string" },
                        ["description"] = "Lista de termos de busca. Use múltiplos termos para ampliar os resultados (ex: ['teclado', 'mecânico'])."
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
            Nome = "cadastrar_cliente",
            Descricao = "Cadastra um novo cliente no sistema, retornando o ID gerado. Use quando o usuário não tiver cadastro e quiser fazer um pedido.",
            Parametros = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["nome"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Nome completo do cliente"
                    },
                    ["documento"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "CPF ou CNPJ do cliente"
                    },
                    ["email"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "E-mail do cliente (opcional)"
                    },
                    ["telefone"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Telefone do cliente (opcional)"
                    }
                }
            },
            Obrigatorios = ["nome", "documento"]
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
        },
        new()
        {
            Nome = "consultar_cliente",
            Descricao = "Retorna todos os dados cadastrais de um cliente (nome, documento, e-mail, telefone e endereço completo). Use antes de atualizar dados para verificar o que já existe.",
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
            Nome = "atualizar_cliente",
            Descricao = "Atualiza dados cadastrais de um cliente (nome, documento, e-mail, telefone). Informe apenas os campos que devem ser alterados; os demais serão mantidos.",
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
                    ["nome"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Novo nome completo do cliente (opcional)"
                    },
                    ["documento"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Novo CPF ou CNPJ (opcional)"
                    },
                    ["email"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Novo e-mail (opcional)"
                    },
                    ["telefone"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Novo telefone (opcional)"
                    }
                }
            },
            Obrigatorios = ["cliente_id"]
        },
        new()
        {
            Nome = "atualizar_endereco",
            Descricao = "Atualiza o endereço de um cliente existente. Consulta automaticamente o CEP nos Correios (ViaCEP) para preencher bairro, cidade e estado. Se o CEP não retornar logradouro (ex: CEP rural ou de condomínio), informe o campo 'logradouro' manualmente.",
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
                    ["cep"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "CEP do endereço (somente números ou formato 00000-000)"
                    },
                    ["numero"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Número do imóvel"
                    },
                    ["complemento"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Complemento do endereço (apto, sala, etc.) — opcional"
                    },
                    ["logradouro"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Logradouro manual (opcional). Use quando o CEP não retornar o nome da rua ou o cliente informar uma correção."
                    }
                }
            },
            Obrigatorios = ["cliente_id", "cep", "numero"]
        }
        };

        ferramentas.Add(new()
        {
            Nome = "gerar_cobranca",
            Descricao = "Gera uma cobrança de pagamento para um pedido via Asaas. Retorna link de pagamento, PIX copia e cola ou código de barras do boleto conforme o método escolhido. Use quando o cliente quiser pagar um pedido.",
            Parametros = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["pedido_id"] = new Dictionary<string, object>
                    {
                        ["type"] = "integer",
                        ["description"] = "ID do pedido a ser pago"
                    },
                    ["metodo_pagamento"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Método de pagamento: PIX (padrão), BOLETO ou CARTAO",
                        ["enum"] = new[] { "PIX", "BOLETO", "CARTAO" }
                    }
                }
            },
            Obrigatorios = ["pedido_id"]
        });

        if (_isDevelopment)
        {
            ferramentas.Add(new()
            {
                Nome = "solicitar_desenvolvimento",
                Descricao = "Registra uma solicitação de desenvolvimento quando uma funcionalidade ou dado necessário não está disponível. Use quando identificar ausência de ferramenta, campo ou recurso para atender o cliente.Sempre verificar se o que o cliente pediu está nas solicitações ainda não atendidas. Se não estiver mas tiver correlação, mesclar as solicitações, cancelando a anterior e gerando uma nova.Quando uma solicitação indicar ou apontar para a necessidade de utilizar uma api abrir uma solicitação de aprovação de acesso indicando url a ser acessada, e aguardar aprovação.",
                Parametros = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["descricao"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Descrição clara do que precisa ser implementado"
                        },
                        ["tipo"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Tipo: expor_campo, nova_ferramenta, novo_endpoint, correcao",
                            ["enum"] = new[] { "expor_campo", "nova_ferramenta", "novo_endpoint", "correcao" }
                        },
                        ["impacto"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Impacto estimado: baixo (só expõe dado), medio (nova lógica), alto (mudança estrutural)",
                            ["enum"] = new[] { "baixo", "medio", "alto" }
                        },
                        ["detalhes"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Detalhes adicionais, parâmetros esperados ou contexto da conversa"
                        },
                        ["url_externa"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "URL da API externa que será acessada, se houver. Quando preenchida, a solicitação ficará em aguardando_aprovacao até que o responsável autorize o acesso."
                        }
                    }
                },
                Obrigatorios = ["descricao", "tipo", "impacto"]
            });
        }

        return ferramentas;
    }
}
