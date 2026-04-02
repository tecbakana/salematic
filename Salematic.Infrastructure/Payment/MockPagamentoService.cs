using Salematic.Domain.Interfaces;
using Salematic.Domain.Models;

namespace Salematic.Infrastructure.Payment;

// Implementação mock — substituir por gateway real em produção
public class MockPagamentoService : IPagamentoService
{
    public Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento solicitacao)
    {
        // Cartões terminando em "0000" são recusados; demais aprovados
        // PIX e Boleto sempre aprovados
        var aprovado = solicitacao.MetodoPagamento.ToLower() switch
        {
            "cartao" => !solicitacao.NumeroCartao?.EndsWith("0000") ?? true,
            _ => true
        };

        return Task.FromResult(new ResultadoPagamento
        {
            Aprovado = aprovado,
            Mensagem = aprovado ? "Pagamento aprovado." : "Cartão recusado.",
            CodigoTransacao = aprovado ? Guid.NewGuid().ToString() : null
        });
    }
}
