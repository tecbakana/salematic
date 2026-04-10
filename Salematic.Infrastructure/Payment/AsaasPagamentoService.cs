using System.Net.Http.Json;
using System.Text.Json;
using Salematic.Domain.Interfaces;
using Salematic.Domain.Models;

namespace Salematic.Infrastructure.Payment;

public class AsaasPagamentoService : IPagamentoService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public AsaasPagamentoService(string apiKey, string baseUrl)
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("access_token", apiKey);
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento solicitacao)
    {
        try
        {
            var customerId = await ObterOuCriarClienteAsync(solicitacao);

            var billingType = solicitacao.MetodoPagamento.ToUpper() switch
            {
                "BOLETO" => "BOLETO",
                "CARTAO" or "CREDIT_CARD" => "CREDIT_CARD",
                _ => "PIX"
            };

            var dueDate = DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd");

            var payload = new
            {
                customer = customerId,
                billingType,
                value = solicitacao.Valor,
                dueDate,
                description = $"Pedido #{solicitacao.PedidoId} — Salematic"
            };

            var resp = await _http.PostAsJsonAsync($"{_baseUrl}/payments", payload);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return new ResultadoPagamento { Aprovado = false, Mensagem = $"Erro ao gerar cobrança: {json}" };

            var doc = JsonDocument.Parse(json).RootElement;
            var pagamentoId = doc.GetProperty("id").GetString()!;
            var invoiceUrl = doc.TryGetProperty("invoiceUrl", out var inv) ? inv.GetString() : null;
            var bankSlipUrl = doc.TryGetProperty("bankSlipUrl", out var bsu) ? bsu.GetString() : null;

            string? pixCopiaCola = null;
            if (billingType == "PIX")
            {
                try
                {
                    var pixJson = await _http.GetStringAsync($"{_baseUrl}/payments/{pagamentoId}/pixQrCode");
                    var pixDoc = JsonDocument.Parse(pixJson).RootElement;
                    pixCopiaCola = pixDoc.TryGetProperty("payload", out var p) ? p.GetString() : null;
                }
                catch { /* QR code pode não estar disponível imediatamente */ }
            }

            return new ResultadoPagamento
            {
                Aprovado = true,
                Mensagem = "Cobrança gerada com sucesso.",
                CodigoTransacao = pagamentoId,
                LinkPagamento = invoiceUrl,
                PixCopiaCola = pixCopiaCola,
                CodigoBarras = bankSlipUrl,
                DataVencimento = dueDate
            };
        }
        catch (Exception ex)
        {
            return new ResultadoPagamento { Aprovado = false, Mensagem = $"Erro interno: {ex.Message}" };
        }
    }

    private async Task<string> ObterOuCriarClienteAsync(SolicitacaoPagamento sol)
    {
        var doc = LimparDocumento(sol.ClienteDocumento);

        var searchResp = await _http.GetStringAsync($"{_baseUrl}/customers?cpfCnpj={Uri.EscapeDataString(doc)}");
        var searchDoc = JsonDocument.Parse(searchResp).RootElement;

        if (searchDoc.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
            return data[0].GetProperty("id").GetString()!;

        var payload = new
        {
            name = sol.ClienteNome,
            cpfCnpj = doc,
            email = sol.ClienteEmail,
            mobilePhone = sol.ClienteTelefone
        };

        var createResp = await _http.PostAsJsonAsync($"{_baseUrl}/customers", payload);
        var createJson = await createResp.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createJson).RootElement;
        return createDoc.GetProperty("id").GetString()!;
    }

    private static string LimparDocumento(string doc) =>
        new string(doc.Where(char.IsDigit).ToArray());
}
