using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Salematic.Application.DTOs;
using Salematic.Application.Services;
using Salematic.Domain.Interfaces;
using System.Text.Json;

namespace Salematic.Infrastructure.ServiceBus
{
    public class ServiceBusConsumer : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly IEventPublisher _publisher;
        private readonly IServiceProvider _services;
        private readonly ILogger<ServiceBusConsumer> _logger;

        public ServiceBusConsumer(IConfiguration config, IEventPublisher publisher, IServiceProvider services, ILogger<ServiceBusConsumer> logger)
        {
            _config    = config;
            _publisher = publisher;
            _services  = services;
            _logger    = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connStr = _config["ServiceBus:ConnectionString"];
            var topico  = _config["ServiceBus:TopicoPedidos"] ?? "top-pedidos";
            var sub     = _config["ServiceBus:Subscription"]  ?? "sub-pedidos-salematic";

            if (string.IsNullOrWhiteSpace(connStr))
            {
                _logger.LogWarning("ServiceBus:ConnectionString não configurada — consumer desabilitado.");
                return;
            }

            await using var client    = new ServiceBusClient(connStr);
            await using var processor = client.CreateProcessor(topico, sub, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls   = 1,
                AutoCompleteMessages = false
            });

            processor.ProcessMessageAsync += HandleMessageAsync;
            processor.ProcessErrorAsync   += HandleErrorAsync;

            await processor.StartProcessingAsync(stoppingToken);
            _logger.LogInformation("Consumer Service Bus iniciado. Tópico={Topico} Sub={Sub}", topico, sub);

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }

            await processor.StopProcessingAsync();
            _logger.LogInformation("Consumer Service Bus encerrado.");
        }

        private async Task HandleMessageAsync(ProcessMessageEventArgs args)
        {
            try
            {
                var json = args.Message.Body.ToString();
                CmsxPedidoMsg? msg = null;

                try
                {
                    msg = JsonSerializer.Deserialize<CmsxPedidoMsg>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Falha ao desserializar mensagem JSON: {Body}", json);
                    await args.CompleteMessageAsync(args.Message);
                    return;
                }

                if (msg is null || string.IsNullOrWhiteSpace(msg.Numeropedido) || string.IsNullOrWhiteSpace(msg.Aplicacaoid) || string.IsNullOrWhiteSpace(msg.Clientenome) || string.IsNullOrWhiteSpace(msg.Clienteemail))
                {
                    _logger.LogWarning("Mensagem inválida recebida: {Body}", json);
                    await args.CompleteMessageAsync(args.Message);
                    return;
                }

                _logger.LogInformation("Pedido recebido do CMSX: {Numero} Aplicacao={App} Email={Email}", msg.Numeropedido, msg.Aplicacaoid, msg.Clienteemail);

                using var scope = _services.CreateScope();
                var clienteRepo =  scope.ServiceProvider.GetRequiredService<IClienteRepository>();
                var pedidoService = scope.ServiceProvider.GetRequiredService<PedidoService>();

                var cliente = await clienteRepo.BuscarPorEmailAsync(msg.Clienteemail);
                if (cliente == null)
                {
                    _logger.LogInformation("Cliente não encontrado. Email: {Email} Numero={Numero}", msg.Clienteemail,msg.Numeropedido);
                    
                    await args.DeadLetterMessageAsync(args.Message, "Cliente não encontrado", $"Nenhum cliente com email {msg.Clienteemail} foi encontrado no Salematic.");
                    return;
                }

                var request = new WebhookPedidoRequest
                {
                    ClienteId = cliente.SalematicClienteId,
                    MetodoPagamento = "desconecido",
                    Itens = new List<WebhookItemRequest>()
                };

                WebhookPedidoResponse resultado;
                try
                {
                    resultado = await pedidoService.ProcessarAsync(request);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar pedido. Numero={Numero} Email={Email}", msg.Numeropedido, msg.Clienteemail);
                    await args.AbandonMessageAsync(args.Message);
                    return;
                }

                if(!resultado.Sucesso)
                {
                    _logger.LogWarning("Processamento do pedido falhou. Numero={Numero} Email={Email} Motivo={Motivo}", msg.Numeropedido, msg.Clienteemail, resultado.Mensagem);
                    await args.DeadLetterMessageAsync(args.Message, "Processamento falhou", resultado.Mensagem);
                    return;
                }

                // Publica confirmação de recebimento de volta ao CMSX
                await _publisher.PublishAsync(new
                {
                    aplicacaoid  = msg.Aplicacaoid,
                    numeropedido = msg.Numeropedido,
                    clientenome  = msg.Clientenome,
                    clienteemail = msg.Clienteemail,
                    valorpedido  = msg.Valorpedido,
                    status       = "entrada",
                    evento       = "pedido.recebido",
                    descricao    = "Pedido recebido pelo Salematic e em processamento."
                });

                await args.CompleteMessageAsync(args.Message);
                _logger.LogInformation("Pedido {Numero} processado.SalematicId={Id}", msg.Numeropedido,resultado.PedidoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem do Service Bus.");
                await args.AbandonMessageAsync(args.Message);
            }
        }

        private Task HandleErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Erro no processor Service Bus. Origem={Source}", args.ErrorSource);
            return Task.CompletedTask;
        }

        private sealed class CmsxPedidoMsg
        {
            public string?  Aplicacaoid  { get; set; }
            public string?  Numeropedido { get; set; }
            public string?  Clientenome  { get; set; }
            public string?  Clienteemail { get; set; }
            public decimal? Valorpedido  { get; set; }
            public string?  Status       { get; set; }
            public string?  Descricao    { get; set; }
        }
    }
}
