namespace Salematic.Domain.Entities;

public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public string UnidadeMedida { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}

public class Estoque
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public DateTime UltimaAtualizacao { get; set; }
}

public class Cliente
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty; // CPF ou CNPJ
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Cep { get; set; } = string.Empty;
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Complemento { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

public class Pedido
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public DateTime DataPedido { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Evento { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public List<ItemPedido> Itens { get; set; } = new();
}

public class ItemPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int ProdutoId { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}
