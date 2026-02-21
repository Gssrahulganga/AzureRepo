using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Demo.Miroservices.Azure.Shared.Models;

namespace Demo.Miroservices.Azure.Subscriber.Services
{
    public class ServiceBusOrderSubscriber : BackgroundService
    {
        private readonly ServiceBusClient _client;
        private readonly string _topicName;
        private readonly string _subscriptionName;
        private readonly ILogger<ServiceBusOrderSubscriber> _logger;
        private ServiceBusProcessor? _processor;

        public ServiceBusOrderSubscriber(ServiceBusClient client, IConfiguration configuration, ILogger<ServiceBusOrderSubscriber> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Resolve topic name: TopicName -> TopicUrl (last segment) -> ConnectionString EntityPath
            var topicName = configuration.GetValue<string>("ServiceBus:TopicName");
            if (string.IsNullOrWhiteSpace(topicName))
            {
                var topicUrl = configuration.GetValue<string>("ServiceBus:TopicUrl");
                if (!string.IsNullOrWhiteSpace(topicUrl))
                {
                    if (!Uri.TryCreate(topicUrl, UriKind.Absolute, out var uri))
                        throw new InvalidOperationException("ServiceBus:TopicUrl is not a valid absolute URI.");
                    topicName = uri.AbsolutePath.Trim('/');
                }
            }

            if (string.IsNullOrWhiteSpace(topicName))
            {
                var connectionString = configuration.GetValue<string>("ServiceBus:ConnectionString");
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    var entityPart = parts.FirstOrDefault(p => p.Trim().StartsWith("EntityPath=", StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrWhiteSpace(entityPart))
                    {
                        var idx = entityPart.IndexOf('=');
                        if (idx >= 0 && idx < entityPart.Length - 1)
                            topicName = entityPart[(idx + 1)..].Trim();
                    }
                }
            }

            _topicName = topicName ?? throw new InvalidOperationException("ServiceBus:TopicName or ServiceBus:TopicUrl or ConnectionString with EntityPath must be configured.");

            _subscriptionName = configuration.GetValue<string>("ServiceBus:SubscriptionName")
                                 ?? throw new InvalidOperationException("ServiceBus:SubscriptionName must be configured.");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Create processor for topic/subscription
            _processor = _client.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

            _processor.ProcessMessageAsync += ProcessMessageHandler;
            _processor.ProcessErrorAsync += ProcessErrorHandler;

            _logger.LogInformation("Starting ServiceBus processor for topic {Topic} / subscription {Subscription}", _topicName, _subscriptionName);
            return _processor.StartProcessingAsync(stoppingToken);
        }

        private async Task ProcessMessageHandler(ProcessMessageEventArgs args)
        {
            try
            {
                var body = args.Message.Body.ToString();
                OrderDto? order = null;

                try
                {
                    order = JsonSerializer.Deserialize<OrderDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize message body. MessageId={MessageId}", args.Message.MessageId);
                }

                if (order != null)
                {
                    // TODO: Replace with real processing
                    _logger.LogInformation("Received order Id={OrderId} Customer={Customer} Amount={Amount} CreatedAt={CreatedAt}", order.Id, order.CustomerId, order.Amount, order.CreatedAt);
                }
                else
                {
                    _logger.LogInformation("Received message but could not parse OrderDto. MessageId={MessageId}", args.Message.MessageId);
                }

                // Complete the message
                await args.CompleteMessageAsync(args.Message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message. MessageId={MessageId}", args.Message?.MessageId);
                // Let Service Bus handle retries by NOT completing the message
            }
        }

        private Task ProcessErrorHandler(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "ServiceBus error. EntityPath={EntityPath} Operation={Operation}", args.EntityPath, args.ErrorSource);
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_processor != null)
            {
                _processor.ProcessMessageAsync -= ProcessMessageHandler;
                _processor.ProcessErrorAsync -= ProcessErrorHandler;
                await _processor.StopProcessingAsync(cancellationToken).ConfigureAwait(false);
                await _processor.DisposeAsync().ConfigureAwait(false);
                _processor = null;
            }

            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}