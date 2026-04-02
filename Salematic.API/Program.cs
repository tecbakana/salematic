using Salematic.Application.Services;
using Salematic.Domain.Interfaces;
using Salematic.Infrastructure.LLM;
using Salematic.Infrastructure.Payment;
using Salematic.Infrastructure.Repositories;
using Salematic.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cache — Redis em produção, Memory local
var redisConn = config.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConn))
    builder.Services.AddStackExchangeRedisCache(o => o.Configuration = redisConn);
else
    builder.Services.AddDistributedMemoryCache();

// Repositórios
var dbConn = config.GetConnectionString("SalematicDB")
    ?? throw new InvalidOperationException("ConnectionStrings:SalematicDB não configurado");

builder.Services.AddScoped<IProdutoRepository>(sp =>
{
    var inner = new ProdutoRepository(dbConn);
    var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
    return new CachedProdutoRepository(inner, cache);
});
builder.Services.AddScoped<IPedidoRepository>(_ => new PedidoRepository(dbConn));
builder.Services.AddScoped<IClienteRepository>(_ => new ClienteRepository(dbConn));

// Pagamento
builder.Services.AddScoped<IPagamentoService, MockPagamentoService>();

// LLM
builder.Services.AddSingleton<ILlmClient>(_ => LlmFactory.Create(config));

// Application services
builder.Services.AddScoped<AgentToolsService>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<ChatService>(sp => new ChatService(
    sp.GetRequiredService<ILlmClient>(),
    sp.GetRequiredService<AgentToolsService>(),
    config["Agent:SystemPrompt"] ?? "Você é um assistente de vendas. Ajude o cliente a consultar produtos, fazer e acompanhar pedidos."
));

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.MapControllers();
app.Run();

public partial class Program { }
