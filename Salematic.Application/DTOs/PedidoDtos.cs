namespace Salematic.Application.DTOs;

public class WebhookPedidoRequest
{
    public int ClienteId { get; set; }
    public string MetodoPagamento { get; set; } = string.Empty;
    public string? NumeroCartao { get; set; }
    public List<WebhookItemRequest> Itens { get; set; } = new();
}

public class WebhookItemRequest
{
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
}

public class WebhookPedidoResponse
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public int? PedidoId { get; set; }
}
