using Anthropic.SDK.Messaging;
using Azure.Messaging.ServiceBus;
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
    public class ServiceBusPublisher : IEventPublisher
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;

        public ServiceBusPublisher(string connectionString, string topicName)
        {
            _client = new ServiceBusClient(connectionString);
            _sender = _client.CreateSender(topicName);
        }

        public async Task PublishAsync(object @event)
        {
            var json = JsonSerializer.Serialize(@event);
            var message = new ServiceBusMessage(json);
            await _sender.SendMessageAsync(message);
        }

    }
}
