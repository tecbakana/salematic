using Moq;
using Salematic.Application.DTOs;
using Salematic.Application.Services;
using Salematic.Domain.Interfaces;
using Salematic.Domain.Models;
using Xunit;

namespace Salematic.Tests;

public class ChatServiceTests
{
    private readonly Mock<ILlmClient> _llmMock = new();
    private readonly Mock<IProdutoRepository> _produtosMock = new();
    private readonly Mock<IPedidoRepository> _pedidosMock = new();
    private readonly Mock<IClienteRepository> _clientesMock = new();

    private ChatService CriarService()
    {
        var tools = new AgentToolsService(_produtosMock.Object, _pedidosMock.Object, _clientesMock.Object);
        return new ChatService(_llmMock.Object, tools, "Você é um assistente de vendas.");
    }

    [Fact]
    public async Task ProcessarAsync_RetornaTexto_QuandoLlmRespondeComTexto()
    {
        _llmMock.Setup(x => x.EnviarAsync(It.IsAny<string>(), It.IsAny<List<LlmMensagem>>(), It.IsAny<List<LlmFerramenta>>()))
            .ReturnsAsync(new LlmResponse { TipoResposta = "text", TextoResposta = "Olá! Como posso ajudar?" });

        var service = CriarService();
        var resultado = await service.ProcessarAsync(new ChatRequest
        {
            Mensagem = "Oi",
            ClienteId = 1,
            Historico = []
        });

        Assert.Equal("Olá! Como posso ajudar?", resultado.Resposta);
        Assert.Null(resultado.FerramentaUsada);
    }

    [Fact]
    public async Task ProcessarAsync_ExecutaFerramenta_QuandoLlmChamaTool()
    {
        _llmMock.SetupSequence(x => x.EnviarAsync(It.IsAny<string>(), It.IsAny<List<LlmMensagem>>(), It.IsAny<List<LlmFerramenta>>()))
            .ReturnsAsync(new LlmResponse
            {
                TipoResposta = "tool_use",
                NomeFerramenta = "consultar_estoque",
                IdChamada = "call_1",
                Argumentos = new Dictionary<string, object> { ["nome_produto"] = "teclado" }
            });

        _llmMock.Setup(x => x.EnviarResultadoToolAsync(It.IsAny<string>(), It.IsAny<List<LlmMensagem>>(),
            It.IsAny<List<LlmFerramenta>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new LlmResponse { TipoResposta = "text", TextoResposta = "Encontrei os produtos." });

        _produtosMock.Setup(x => x.BuscarPorNomeAsync("teclado"))
            .ReturnsAsync([]);

        var service = CriarService();
        var resultado = await service.ProcessarAsync(new ChatRequest
        {
            Mensagem = "Tem teclado?",
            ClienteId = 1,
            Historico = []
        });

        Assert.Equal("consultar_estoque", resultado.FerramentaUsada);
        Assert.Equal("Encontrei os produtos.", resultado.Resposta);
    }
}
