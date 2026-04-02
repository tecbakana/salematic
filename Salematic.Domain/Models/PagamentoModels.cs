namespace Salematic.Domain.Models;

public class SolicitacaoPagamento
{
    public int PedidoId { get; set; }
    public decimal Valor { get; set; }
    public string MetodoPagamento { get; set; } = string.Empty;
    public string? NumeroCartao { get; set; }
}

public class ResultadoPagamento
{
    public bool Aprovado { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string? CodigoTransacao { get; set; }
}
