using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Salematic.Domain.Interfaces;

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

        public async Task PublishAsync<T>(T @event)
        {
            var message = new ServiceBusMessage(System.Text.Json.JsonSerializer.Serialize(@event));
            await _sender.SendMessageAsync(message);
        }

    }
}
