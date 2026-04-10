namespace Salematic.Domain.Models;

public class SolicitacaoPagamento
{
    public int PedidoId { get; set; }
    public decimal Valor { get; set; }
    public string MetodoPagamento { get; set; } = string.Empty; // PIX | BOLETO | CARTAO

    // Dados do cliente para criar/localizar no gateway
    public string ClienteNome { get; set; } = string.Empty;
    public string ClienteDocumento { get; set; } = string.Empty;
    public string? ClienteEmail { get; set; }
    public string? ClienteTelefone { get; set; }
}

public class ResultadoPagamento
{
    public bool Aprovado { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string? CodigoTransacao { get; set; }
    public string? LinkPagamento { get; set; }
    public string? PixCopiaCola { get; set; }
    public string? CodigoBarras { get; set; }
    public string? DataVencimento { get; set; }
}
