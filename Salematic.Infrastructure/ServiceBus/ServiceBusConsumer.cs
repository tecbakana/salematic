using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Salematic.Application.Services;
using Salematic.Domain.Entities;
using Salematic.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Salematic.Infrastructure.ServiceBus
{
    public class ServiceBusConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly IPedidoRepository _pedidoRepository;
        private readonly string _connectionString;
        private readonly ILogger<ServiceBusConsumer> _logger;

        public ServiceBusConsumer(IServiceScopeFactory scopeFactory, IConfiguration config, IPedidoRepository pedidoRepository, string connectionString )
        {
            _scopeFactory = scopeFactory;
            _pedidoRepository = pedidoRepository;
            _connectionString = connectionString;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connStr = _config["ServiceBus:ConnectionString"];
            var topico = _config["ServiceBus:Topico"] ?? "top-pedidos";
            var sub = _config["ServiceBus:Subscription"] ?? "sub-pedidos-salematic";

            if (string.IsNullOrWhiteSpace(connStr))
            {
                _logger.LogWarning("ServiceBus:ConnectionString não configurada — consumer desabilitado.");
                return;
            }

            await using var client = new ServiceBusClient(connStr);
            await using var processor = client.CreateProcessor(topico, sub, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 1,
                AutoCompleteMessages = false
            });

            processor.ProcessMessageAsync += HandleMessageAsync;
            processor.ProcessErrorAsync += HandleErrorAsync;

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
                var msg = JsonSerializer.Deserialize<Pedido>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (msg is null || string.IsNullOrWhiteSpace(msg.Id.ToString()) || string.IsNullOrWhiteSpace(msg.ClienteId.ToString()))
                {
                    _logger.LogWarning("Mensagem inválida recebida: {Body}", json);
                    await args.CompleteMessageAsync(args.Message);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<PedidoService>();

                var pedido = await _pedidoRepository.BuscarPorIdAsync(msg.Id);

                if (pedido is null)
                {
                    pedido = new Pedido
                    {
                        Id = new int(), // Será gerado pelo EF]
                        ClienteId = 0, // Desconecido no momento
                        DataPedido = DateTime.UtcNow,
                        Status = msg.Status ?? "desconecido",
                        ValorTotal = msg.ValorTotal>0?msg.ValorTotal:0,
                        Itens = new List<ItemPedido>()
                    };
                    
                }
                else
                {
                    pedido.Status = msg.Status;
                }


                await _pedidoRepository.CriarPedidoAsync(pedido);
                await args.CompleteMessageAsync(args.Message);

                _logger.LogInformation("Pedido {Num} — status '{Status}' registrado.", msg.Id, msg.Status);
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
    }
}
