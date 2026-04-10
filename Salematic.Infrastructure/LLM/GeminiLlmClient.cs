using System.Net.Http.Json;
using System.Text.Json;
using Salematic.Domain.Interfaces;
using Salematic.Domain.Models;

namespace Salematic.Infrastructure.LLM;

public class GeminiLlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";

    public GeminiLlmClient(string apiKey, string model)
    {
        _http = new HttpClient();
        _apiKey = apiKey;
        _model = model;
    }

    public async Task<LlmResponse> EnviarAsync(string systemPrompt, List<LlmMensagem> mensagens, List<LlmFerramenta> ferramentas)
    {
        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = FiltrarMensagens(mensagens),
            tools = new[]
            {
                new
                {
                    function_declarations = ferramentas.Select(f => new
                    {
                        name = f.Nome,
                        description = f.Descricao,
                        parameters = f.Parametros
                    })
                }
            },
            generationConfig = new { thinkingConfig = new { thinkingBudget = 0 } }
        };

        var url = $"{BaseUrl}/models/{_model}:generateContent?key={_apiKey}";
        var response = await _http.PostAsJsonAsync(url, payload);
        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Gemini {(int)response.StatusCode}: {erro}");
        }

        var json = await response.Content.ReadAsStringAsync();
        return ParseGeminiResponse(json);
    }

    public async Task<LlmResponse> EnviarResultadoToolAsync(
        string systemPrompt, List<LlmMensagem> mensagens, List<LlmFerramenta> ferramentas,
        string idChamada, string nomeFerramenta, string resultado)
    {
        var contents = FiltrarMensagens(mensagens).Cast<object>().ToList();

        // Gemini exige que response seja um objeto (Struct), nunca array
        // Sempre envolve o resultado em { output: ... }
        var resultadoElement = JsonSerializer.Deserialize<JsonElement>(resultado);
        contents.Add(new
        {
            role = "user",
            parts = new[] { new { function_response = new { name = nomeFerramenta, response = new { output = resultadoElement } } } }
        });

        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents,
            tools = new[]
            {
                new
                {
                    function_declarations = ferramentas.Select(f => new
                    {
                        name = f.Nome,
                        description = f.Descricao,
                        parameters = f.Parametros
                    })
                }
            },
            generationConfig = new { thinkingConfig = new { thinkingBudget = 0 } }
        };

        var url = $"{BaseUrl}/models/{_model}:generateContent?key={_apiKey}";
        var response = await _http.PostAsJsonAsync(url, payload);
        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Gemini {(int)response.StatusCode}: {erro}");
        }

        var json = await response.Content.ReadAsStringAsync();
        return ParseGeminiResponse(json);
    }

    private static IEnumerable<object> FiltrarMensagens(List<LlmMensagem> mensagens) =>
        mensagens
            .Where(m => !string.IsNullOrWhiteSpace(m.Content))
            .Select(m => new
            {
                role = m.Role == "assistant" ? "model" : "user",
                parts = new[] { new { text = m.Content } }
            });

    private static LlmResponse ParseGeminiResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var parts = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts");

        // Gemini pode retornar texto + functionCall em parts separados — prioriza functionCall
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("functionCall", out var fc))
            {
                return new LlmResponse
                {
                    TipoResposta = "tool_use",
                    NomeFerramenta = fc.GetProperty("name").GetString(),
                    IdChamada = fc.GetProperty("name").GetString(),
                    Argumentos = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                        fc.GetProperty("args").GetRawText())
                };
            }
        }

        // Nenhuma functionCall — retorna texto do primeiro part
        return new LlmResponse
        {
            TipoResposta = "text",
            TextoResposta = parts[0].GetProperty("text").GetString() ?? string.Empty
        };
    }
}
