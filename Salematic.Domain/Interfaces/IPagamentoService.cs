using Salematic.Domain.Models;

namespace Salematic.Domain.Interfaces;

public interface IPagamentoService
{
    Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento solicitacao);
}
