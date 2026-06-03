using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Salematic.API.Middlewares;
using Salematic.Application.Behaviors;
using Salematic.Application.Notifications.Checkout;
using Salematic.Application.Services;
using Salematic.Application.Validators;
using Salematic.Domain.Entities;
using Salematic.Domain.Interfaces;
using Salematic.Infrastructure.LLM;
using Salematic.Infrastructure.Payment;
using Salematic.Infrastructure.Repositories;
using Salematic.Infrastructure.ServiceBus;
using Salematic.Infrastructure.Services;
using StackExchange.Redis;
using Salematic.Infrastructure.Locking;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT
var jwtKey = config["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key não configurado (use dotnet user-secrets).");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"] ?? "Salematic",
            ValidAudience = config["Jwt:Audience"] ?? "SalematicClientes",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// Cache — Redis em produção, Memory local
var redisConn = config.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConn))
    builder.Services.AddStackExchangeRedisCache(o => o.Configuration = redisConn);
else
    builder.Services.AddDistributedMemoryCache();

// Distributed Lock — Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connStr = config.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionStrings:Redis não configurado.");

    var options = ConfigurationOptions.Parse(connStr);
    options.LoggerFactory = sp.GetRequiredService<ILoggerFactory>();
    options.AbortOnConnectFail = false;

    return ConnectionMultiplexer.Connect(options);
});
builder.Services.AddSingleton<IStockLockService, RedisStockLockService>();

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
builder.Services.AddScoped<IEmailService>(sp => new EmailService(config));



// Configurações mock para desenvolvimento e testes
builder.Services.AddSingleton<IMockPaymentConfigStore, MockPaymentConfigStore>();

// Pagamento
/*var asaasKey = config["Asaas:ApiKey"] ?? string.Empty;
var asaasUrl = config["Asaas:BaseUrl"] ?? "https://sandbox.asaas.com/api/v3";
builder.Services.AddScoped<IPagamentoService>(_ =>
    string.IsNullOrWhiteSpace(asaasKey)
        ? new MockPagamentoService()
        : new AsaasPagamentoService(asaasKey, asaasUrl));*/

// LLM
builder.Services.AddSingleton<ILlmClient>(_ => LlmFactory.Create(config));

// Application services
var isDevelopment = builder.Environment.IsDevelopment();
var devRequestsPath = config["DevRequests:Dir"]
    ?? Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "dev-requests"));

// Chat service
builder.Services.AddScoped<AgentToolsService>(sp => new AgentToolsService(
    sp.GetRequiredService<IProdutoRepository>(),
    sp.GetRequiredService<IPedidoRepository>(),
    sp.GetRequiredService<IClienteRepository>(),
    sp.GetRequiredService<IPagamentoService>(),
    isDevelopment,
    devRequestsPath
));

builder.Services.AddSingleton<IEventPublisher, NullEventPublisher>();

builder.Services.AddScoped<IPagamentoService, MockPagamentoService>();

builder.Services.AddScoped<ClienteService>(sp =>
    new ClienteService(
        sp.GetRequiredService<IClienteRepository>(),
        sp.GetRequiredService<IEmailService>(),
        sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ClienteService>>(),
        config));
builder.Services.AddScoped<PedidoService>();

builder.Services.AddScoped<ChatService>(sp => new ChatService(
    sp.GetRequiredService<ILlmClient>(),
    sp.GetRequiredService<AgentToolsService>(),
    config["Agent:SystemPrompt"] ?? "Você é um assistente de vendas. Ajude o cliente a consultar produtos, fazer e acompanhar pedidos.",
    isDevelopment
));

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddValidatorsFromAssembly(typeof(ItemValidator).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Salematic.Application.Commands.Checkout.ProcessarCheckoutCommand).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
