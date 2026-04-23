using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using Salematic.Domain.Interfaces;
using Salematic.Domain.Models;

namespace Salematic.Application.Services;

public class AgentToolsService
{
    private readonly IProdutoRepository _produtos;
    private readonly IPedidoRepository _pedidos;
    private readonly IClienteRepository _clientes;
    private readonly IPagamentoService _pagamento;
    private readonly bool _isDevelopment;
    private readonly string _devRequestsPath;
    private static readonly HttpClient _httpClient = new();

    public AgentToolsService(
        IProdutoRepository produtos,
        IPedidoRepository pedidos,
        IClienteRepository clientes,
        IPagamentoService pagamento,
        bool isDevelopment,
        string devRequestsPath)
    {
        _produtos = produtos;
        _pedidos = pedidos;
        _clientes = clientes;
        _pagamento = pagamento;
        _isDevelopment = isDevelopment;
        _devRequestsPath = devRequestsPath;
    }

    public async Task<string> ExecutarAsync(string nomeFerramenta, Dictionary<string, JsonElement> argumentos)
    {
        return nomeFerramenta switch
        {
            "consultar_estoque" => await ConsultarEstoqueAsync(argumentos),
            "registrar_pedido" => await RegistrarPedidoAsync(argumentos),
            "consultar_pedidos" => await ConsultarPedidosAsync(argumentos),
            "cancelar_pedido" => await CancelarPedidoAsync(argumentos),
            "cadastrar_cliente" => await CadastrarClienteAsync(argumentos),
            "consultar_cliente" => await ConsultarClienteAsync(argumentos),
            "atualizar_cliente" => await AtualizarClienteAsync(argumentos),
            "atualizar_endereco" => await AtualizarEnderecoAsync(argumentos),
            "gerar_cobranca" => await GerarCobrancaAsync(argumentos),
            "solicitar_desenvolvimento" => await SolicitarDesenvolvimentoAsync(argumentos),
            _ => JsonSerializer.Serialize(new { erro = $"Ferramenta desconhecida: {nomeFerramenta}" })
        };
    }

    private async Task<string> ConsultarEstoqueAsync(Dictionary<string, JsonElement> args)
    {
        // aceita string simples ou array de termos ["teclado", "mecânico"]
        var termos = new List<string>();
        var el = GetArgElement(args, "nome_produto");
        if (el.ValueKind == JsonValueKind.Array)
            termos.AddRange(el.EnumerateArray().Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() ?? "" : e.ToString()).Where(s => s.Length > 0));
        else if (el.ValueKind != JsonValueKind.Undefined)
            termos.Add(el.ValueKind == JsonValueKind.String ? el.GetString() ?? "" : el.ToString());

        if (termos.Count == 0) termos.Add("");

        //IMPLEMENTAÇÃO MANUAL PARA QUEBRAR NOMES COMPOSTOS (ex: "teclado mecânico" deve buscar por "teclado" e "mecânico")

        var subTermos = termos
            .SelectMany(t => t.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Distinct()
            .ToList();

        var vistos = new HashSet<int>();
        var resultado = new List<object>();

        foreach (var termo in subTermos)
        {
            var produtos = await _produtos.BuscarPorNomeAsync(termo);
            foreach (var p in produtos)
            {
                if (!vistos.Add(p.Id)) continue;
                var estoque = await _produtos.ObterEstoqueAsync(p.Id);
                resultado.Add(new
                {
                    id = p.Id,
                    nome = p.Nome,
                    descricao = p.Descricao,
                    preco = p.Preco,
                    unidade = p.UnidadeMedida,
                    quantidade_em_estoque = estoque?.Quantidade ?? 0
                });
            }
        }

        return resultado.Count > 0
            ? JsonSerializer.Serialize(resultado)
            : JsonSerializer.Serialize(new { mensagem = "Nenhum produto encontrado." });
    }

    private async Task<string> RegistrarPedidoAsync(Dictionary<string, JsonElement> args)
    {
        var clienteId = Convert.ToInt32(GetArg(args, "cliente_id"));
        var cliente = await _clientes.BuscarPorIdAsync(clienteId);
        if (cliente is null)
            return JsonSerializer.Serialize(new { erro = "Cliente não encontrado." });

        var itensJson = GetArg(args, "itens") is { Length: > 0 } s ? s : "[]";
        var itensReq = JsonSerializer.Deserialize<List<ItemPedidoRequest>>(itensJson);
        if (itensReq is null || itensReq.Count == 0)
            return JsonSerializer.Serialize(new { erro = "Nenhum item informado." });

        var itensPedido = new List<Domain.Entities.ItemPedido>();
        decimal total = 0;

        foreach (var item in itensReq)
        {
            var produto = await _produtos.BuscarPorIdAsync(item.ProdutoId);
            if (produto is null)
                return JsonSerializer.Serialize(new { erro = $"Produto {item.ProdutoId} não encontrado." });

            var estoque = await _produtos.ObterEstoqueAsync(item.ProdutoId);
            if (estoque is null || estoque.Quantidade < item.Quantidade)
                return JsonSerializer.Serialize(new { erro = $"Estoque insuficiente para {produto.Nome}. Disponível: {estoque?.Quantidade ?? 0}." });

            itensPedido.Add(new Domain.Entities.ItemPedido
            {
                ProdutoId = produto.Id,
                NomeProduto = produto.Nome,
                Quantidade = item.Quantidade,
                PrecoUnitario = produto.Preco
            });
            total += produto.Preco * item.Quantidade;
        }

        var pedido = new Domain.Entities.Pedido
        {
            ClienteId = clienteId,
            DataPedido = DateTime.UtcNow,
            Status = "aguardando_pagamento",
            ValorTotal = total,
            Itens = itensPedido
        };

        var pedidoCriado = await _pedidos.CriarPedidoAsync(pedido);
        return JsonSerializer.Serialize(new
        {
            pedido_id = pedidoCriado.Id,
            valor_total = pedidoCriado.ValorTotal,
            status = pedidoCriado.Status,
            mensagem = "Pedido registrado com sucesso."
        });
    }

    private async Task<string> ConsultarPedidosAsync(Dictionary<string, JsonElement> args)
    {
        var clienteId = Convert.ToInt32(GetArg(args, "cliente_id"));
        var pedidos = await _pedidos.BuscarPorClienteAsync(clienteId);

        var resultado = pedidos.Select(p => new
        {
            id = p.Id,
            data = p.DataPedido,
            status = p.Status,
            valor_total = p.ValorTotal,
            itens = p.Itens.Select(i => new { i.NomeProduto, i.Quantidade, i.PrecoUnitario })
        });

        return JsonSerializer.Serialize(resultado);
    }

    private async Task<string> CancelarPedidoAsync(Dictionary<string, JsonElement> args)
    {
        var pedidoId = Convert.ToInt32(GetArg(args, "pedido_id"));
        var pedido = await _pedidos.BuscarPorIdAsync(pedidoId);

        if (pedido is null)
            return JsonSerializer.Serialize(new { erro = "Pedido não encontrado." });

        if (pedido.Status == "cancelado")
            return JsonSerializer.Serialize(new { mensagem = "Pedido já estava cancelado." });

        await _pedidos.AtualizarStatusAsync(pedidoId, "cancelado","pagamento.erro");
        return JsonSerializer.Serialize(new { mensagem = $"Pedido {pedidoId} cancelado com sucesso." });
    }

    private async Task<string> CadastrarClienteAsync(Dictionary<string, JsonElement> args)
    {
        var nome = GetArg(args, "nome");
        var documento = GetArg(args, "documento");
        var email = GetArg(args, "email");
        var telefone = GetArg(args, "telefone");

        if (string.IsNullOrWhiteSpace(nome))
            return JsonSerializer.Serialize(new { erro = "Nome é obrigatório." });

        if (string.IsNullOrWhiteSpace(documento))
            return JsonSerializer.Serialize(new { erro = "Documento (CPF/CNPJ) é obrigatório." });

        var existente = await _clientes.BuscarPorDocumentoAsync(documento);
        if (existente is not null)
            return JsonSerializer.Serialize(new { erro = "Já existe um cliente com esse documento.", cliente_id = existente.SalematicClienteId });

        var cliente = new Domain.Entities.Cliente
        {
            Nome = nome,
            Documento = documento,
            Email = email,
            Telefone = telefone
        };

        ClienteModel clienteModel = new ClienteModel
        {
            Nome = cliente.Nome,
            Documento = cliente.Documento,
            Email = cliente.Email,
            Telefone = cliente.Telefone
        };

        var criado = await _clientes.ProcessarAsync(clienteModel);
        return JsonSerializer.Serialize(new
        {
            cliente_id = criado.SalematicClienteId,
            mensagem = $"Cliente '{criado.Nome}' cadastrado com sucesso."
        });
    }

    private async Task<string> ConsultarClienteAsync(Dictionary<string, JsonElement> args)
    {
        var clienteId = Convert.ToInt32(GetArg(args, "cliente_id"));
        var cliente = await _clientes.BuscarPorIdAsync(clienteId);
        if (cliente is null)
            return JsonSerializer.Serialize(new { erro = "Cliente não encontrado." });

        return JsonSerializer.Serialize(new
        {
            id = cliente.SalematicClienteId,
            nome = cliente.Nome,
            documento = cliente.Documento,
            email = cliente.Email,
            telefone = cliente.Telefone,
            endereco = new
            {
                cep = cliente.Cep,
                logradouro = cliente.Logradouro,
                numero = cliente.Numero,
                complemento = cliente.Complemento,
                bairro = cliente.Bairro,
                cidade = cliente.Cidade,
                estado = cliente.Estado
            }
        });
    }

    private async Task<string> AtualizarClienteAsync(Dictionary<string, JsonElement> args)
    {
        var clienteId = Convert.ToInt32(GetArg(args, "cliente_id"));
        var cliente = await _clientes.BuscarPorIdAsync(clienteId);
        if (cliente is null)
            return JsonSerializer.Serialize(new { erro = "Cliente não encontrado." });

        var nome = GetArg(args, "nome");
        var documento = GetArg(args, "documento");
        var email = GetArg(args, "email");
        var telefone = GetArg(args, "telefone");

        // aplica apenas os campos informados
        var novoNome = string.IsNullOrWhiteSpace(nome) ? cliente.Nome : nome;
        var novoDocumento = string.IsNullOrWhiteSpace(documento) ? cliente.Documento : documento;
        var novoEmail = string.IsNullOrWhiteSpace(email) ? cliente.Email : email;
        var novoTelefone = string.IsNullOrWhiteSpace(telefone) ? cliente.Telefone : telefone;

        await _clientes.AtualizarClienteAsync(clienteId, novoNome, novoDocumento, novoEmail, novoTelefone);

        return JsonSerializer.Serialize(new
        {
            mensagem = $"Dados do cliente '{novoNome}' atualizados com sucesso.",
            cliente = new { id = clienteId, nome = novoNome, documento = novoDocumento, email = novoEmail, telefone = novoTelefone }
        });
    }

    private async Task<string> AtualizarEnderecoAsync(Dictionary<string, JsonElement> args)
    {
        var clienteId = Convert.ToInt32(GetArg(args, "cliente_id"));
        var cep = GetArg(args, "cep").Replace("-", "").Replace(".", "").Trim();
        var numero = GetArg(args, "numero");
        var complemento = GetArg(args, "complemento");
        var logradouroManual = GetArg(args, "logradouro");

        if (string.IsNullOrWhiteSpace(cep) || cep.Length != 8)
            return JsonSerializer.Serialize(new { erro = "CEP inválido. Informe 8 dígitos." });

        var cliente = await _clientes.BuscarPorIdAsync(clienteId);
        if (cliente is null)
            return JsonSerializer.Serialize(new { erro = "Cliente não encontrado." });

        string logradouro, bairro, cidade, estado;
        try
        {
            var json = await _httpClient.GetStringAsync($"https://viacep.com.br/ws/{cep}/json/");
            var node = JsonNode.Parse(json);
            if (node?["erro"]?.GetValue<bool>() == true)
                return JsonSerializer.Serialize(new { erro = "CEP não encontrado nos Correios." });

            logradouro = string.IsNullOrWhiteSpace(logradouroManual)
                ? node?["logradouro"]?.GetValue<string>() ?? string.Empty
                : logradouroManual;
            bairro = node?["bairro"]?.GetValue<string>() ?? string.Empty;
            cidade = node?["localidade"]?.GetValue<string>() ?? string.Empty;
            estado = node?["uf"]?.GetValue<string>() ?? string.Empty;
        }
        catch
        {
            return JsonSerializer.Serialize(new { erro = "Não foi possível consultar o CEP nos Correios. Tente novamente." });
        }

        await _clientes.AtualizarEnderecoAsync(clienteId, cep, logradouro, numero, complemento, bairro, cidade, estado);

        return JsonSerializer.Serialize(new
        {
            mensagem = $"Endereço do cliente '{cliente.Nome}' atualizado com sucesso.",
            endereco = new { cep, logradouro, numero, complemento, bairro, cidade, estado }
        });
    }

    private async Task<string> GerarCobrancaAsync(Dictionary<string, JsonElement> args)
    {
        var pedidoId = Convert.ToInt32(GetArg(args, "pedido_id"));
        var metodo = GetArg(args, "metodo_pagamento");
        if (string.IsNullOrWhiteSpace(metodo)) metodo = "PIX";

        var pedido = await _pedidos.BuscarPorIdAsync(pedidoId);
        if (pedido is null)
            return JsonSerializer.Serialize(new { erro = "Pedido não encontrado." });

        var cliente = await _clientes.BuscarPorIdAsync(pedido.ClienteId);
        if (cliente is null)
            return JsonSerializer.Serialize(new { erro = "Cliente do pedido não encontrado." });

        var resultado = await _pagamento.ProcessarAsync(new SolicitacaoPagamento
        {
            PedidoId = pedidoId,
            Valor = pedido.ValorTotal,
            MetodoPagamento = metodo,
            ClienteNome = cliente.Nome,
            ClienteDocumento = cliente.Documento,
            ClienteEmail = cliente.Email,
            ClienteTelefone = cliente.Telefone
        });

        if (!resultado.Aprovado)
            return JsonSerializer.Serialize(new { erro = resultado.Mensagem });

        return JsonSerializer.Serialize(new
        {
            mensagem = resultado.Mensagem,
            pedido_id = pedidoId,
            valor = pedido.ValorTotal,
            metodo = metodo.ToUpper(),
            codigo_transacao = resultado.CodigoTransacao,
            link_pagamento = resultado.LinkPagamento,
            pix_copia_e_cola = resultado.PixCopiaCola,
            codigo_barras = resultado.CodigoBarras,
            vencimento = resultado.DataVencimento
        });
    }

    private async Task<string> SolicitarDesenvolvimentoAsync(Dictionary<string, JsonElement> args)
    {
        if (!_isDevelopment)
            return JsonSerializer.Serialize(new { erro = "Funcionalidade disponível apenas em ambiente de desenvolvimento." });

        var descricao  = GetArg(args, "descricao");
        var tipo       = GetArg(args, "tipo");
        var impacto    = GetArg(args, "impacto");
        var detalhes   = GetArg(args, "detalhes");
        var urlExterna = GetArg(args, "url_externa");

        var status = string.IsNullOrWhiteSpace(urlExterna) ? "pending" : "aguardando_aprovacao";
        var id     = Guid.NewGuid().ToString();

        var request = new
        {
            id,
            api            = "salematic",
            tipo,
            impacto,
            descricao,
            detalhes,
            url_externa    = urlExterna,
            status,
            diretorio_alvo = "T:\\Developer\\Salematic",
            timestamp      = DateTime.UtcNow,
            origem         = "chat"
        };

        Directory.CreateDirectory(_devRequestsPath);
        var filePath = Path.Combine(_devRequestsPath, $"{id}.json");
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true }));

        return JsonSerializer.Serialize(new { mensagem = "Solicitação de desenvolvimento registrada com sucesso.", id });
    }

    // Gemini pode retornar args em camelCase (nomeProduto) ou snake_case (nome_produto)
    private static string GetArg(Dictionary<string, JsonElement> args, string snakeKey)
    {
        if (!args.TryGetValue(snakeKey, out var v))
        {
            var camel = System.Text.RegularExpressions.Regex.Replace(snakeKey, "_([a-z])", m => m.Groups[1].Value.ToUpper());
            if (!args.TryGetValue(camel, out v)) return string.Empty;
        }
        return v.ValueKind == JsonValueKind.String ? v.GetString() ?? string.Empty : v.ToString();
    }

    private static JsonElement GetArgElement(Dictionary<string, JsonElement> args, string snakeKey)
    {
        if (args.TryGetValue(snakeKey, out var v)) return v;
        var camel = System.Text.RegularExpressions.Regex.Replace(snakeKey, "_([a-z])", m => m.Groups[1].Value.ToUpper());
        return args.TryGetValue(camel, out v) ? v : default;
    }

    private record ItemPedidoRequest(int ProdutoId, int Quantidade);
}
