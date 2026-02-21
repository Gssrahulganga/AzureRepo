using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Demo.Miroservices.Azure.Shared.Models;
using Microsoft.Extensions.Configuration;

namespace Demo.Miroservices.Azure.Order.Services
{
    public class ServiceBusOrderPublisher : IOrderPublisher
    {
        private readonly ServiceBusClient _client;
        private readonly string _topicName;

        public ServiceBusOrderPublisher(ServiceBusClient client, IConfiguration configuration)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));

            // Try explicit topic name first, then fall back to a full topic URL (extract last path segment).
            var topicName = configuration.GetValue<string>("ServiceBus:TopicName");
            if (string.IsNullOrWhiteSpace(topicName))
            {
                var topicUrl = configuration.GetValue<string>("ServiceBus:TopicUrl");
                if (!string.IsNullOrWhiteSpace(topicUrl))
                {
                    if (!Uri.TryCreate(topicUrl, UriKind.Absolute, out var uri))
                        throw new InvalidOperationException("ServiceBus:TopicUrl is not a valid absolute URI.");

                    var lastSegment = uri.AbsolutePath.Trim('/');
                    if (string.IsNullOrEmpty(lastSegment))
                        throw new InvalidOperationException("ServiceBus:TopicUrl does not contain a topic name.");

                    topicName = lastSegment;
                }
            }

            _topicName = topicName ?? throw new InvalidOperationException("ServiceBus:TopicName or ServiceBus:TopicUrl must be configured.");
        }

        public async Task PublishOrderAsync(OrderDto order, CancellationToken cancellationToken = default)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            var sender = _client.CreateSender(_topicName);

            var json = JsonSerializer.Serialize(order);
            var message = new ServiceBusMessage(json)
            {
                ContentType = "application/json",
                MessageId = order.Id?.ToString() ?? Guid.NewGuid().ToString()
            };

            await sender.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }
}