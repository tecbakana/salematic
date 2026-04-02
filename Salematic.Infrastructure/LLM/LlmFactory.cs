using Microsoft.Extensions.Configuration;
using Salematic.Domain.Interfaces;

namespace Salematic.Infrastructure.LLM;

public static class LlmFactory
{
    public static ILlmClient Create(IConfiguration config)
    {
        var provider = config["Llm:Provider"] ?? "Anthropic";

        return provider.ToLower() switch
        {
            "gemini" => new GeminiLlmClient(
                config["Llm:Gemini:ApiKey"] ?? throw new InvalidOperationException("Llm:Gemini:ApiKey não configurado"),
                config["Llm:Gemini:Model"] ?? "gemini-2.5-flash"),
            _ => new AnthropicLlmClient(
                config["Llm:Anthropic:ApiKey"] ?? throw new InvalidOperationException("Llm:Anthropic:ApiKey não configurado"),
                config["Llm:Anthropic:Model"] ?? "claude-sonnet-4-6")
        };
    }
}
