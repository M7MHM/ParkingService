using Parking.Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Parking.Notification
{
    internal sealed class NotificationConsumer : BackgroundService
    {
        private const string QueueName = "parking.notifications";
        private const string DeadLetterExchange = "parking.events.dlx";
        private const string DeadLetterQueue = "parking.notifications.dlq";
        private const int MaxAttempts = 3;

        private readonly RabbitMqSettings _settings;
        private readonly IIdempotencyStore _idempotency;
        private readonly IEmailSender _email;
        private readonly ILogger<NotificationConsumer> _logger;
        public NotificationConsumer(
            RabbitMqSettings settings,
            IIdempotencyStore idempotency,
            IEmailSender email,
            ILogger<NotificationConsumer> logger)
        {
            _settings = settings;
            _idempotency = idempotency;
            _email = email;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConsumeAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Consumer stopped; reconnecting in 5 seconds.");
                    try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }
        private async Task ConsumeAsync(CancellationToken ct)
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.User,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                AutomaticRecoveryEnabled = true,
                ConsumerDispatchConcurrency = 1,
                ClientProvidedName = "parking-notification"
            };

            await using var connection = await factory.CreateConnectionAsync(ct);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

            await DeclareTopologyAsync(channel, ct);
            await channel.BasicQosAsync(0, 10, false, ct);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(channel, delivery, ct);

            await channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer, cancellationToken: ct);
            _logger.LogInformation("Listening on '{Queue}'.", QueueName);

            await Task.Delay(Timeout.Infinite, ct);
        }
        private static async Task DeclareTopologyAsync(IChannel channel, CancellationToken ct)
        {
            await channel.ExchangeDeclareAsync(MessageRoutes.Exchange, ExchangeType.Topic,
                durable: true, autoDelete: false, arguments: null, cancellationToken: ct);

            await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Fanout,
                durable: true, autoDelete: false, arguments: null, cancellationToken: ct);

            await channel.QueueDeclareAsync(DeadLetterQueue,
                durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: ct);
            await channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, routingKey: "", arguments: null, cancellationToken: ct);

            await channel.QueueDeclareAsync(QueueName,
                durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = DeadLetterExchange },
                cancellationToken: ct);

            await channel.QueueBindAsync(QueueName, MessageRoutes.Exchange, routingKey: "booking.*", arguments: null, cancellationToken: ct);
        }
        private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken ct)
        {
            BookingEventMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<BookingEventMessage>(delivery.Body.Span, MessageJson.Options);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Malformed message on '{RoutingKey}'; dead-lettering.", delivery.RoutingKey);
                await RejectAsync(channel, delivery, ct);
                return;
            }

            if (message is null || message.MessageId == Guid.Empty)
            {
                _logger.LogError("Message without MessageId on '{RoutingKey}'; dead-lettering.", delivery.RoutingKey);
                await RejectAsync(channel, delivery, ct);
                return;
            }

            if (!Guid.TryParse(delivery.BasicProperties.MessageId, out var brokerMessageId) ||
                brokerMessageId != message.MessageId)
            {
                _logger.LogError("Broker MessageId does not match payload MessageId; dead-lettering.");
                await RejectAsync(channel, delivery, ct);
                return;
            }

            if (await _idempotency.WasProcessedAsync(message.MessageId, ct))
            {
                _logger.LogInformation("Message {MessageId} was already processed; acknowledging duplicate.", message.MessageId);
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: ct);
                return;
            }

            var delivered = false;

            for (var attempt = 1; !delivered; attempt++)
            {
                try
                {
                    var (subject, body) = EmailTemplates.Build(delivery.RoutingKey, message);
                    await _email.SendAsync(message.RecipientEmail, subject, body, ct);
                    await _idempotency.MarkProcessedAsync(message.MessageId, ct);
                    delivered = true;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (IsPermanent(ex) || attempt >= MaxAttempts)
                {
                    _logger.LogError(ex, "Giving up on {MessageId} after {Attempt} attempt(s); dead-lettering.",
                        message.MessageId, attempt);
                    await RejectAsync(channel, delivery, ct);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Attempt {Attempt}/{Max} for {MessageId} failed; retrying.",
                        attempt, MaxAttempts, message.MessageId);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
                }
            }
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: ct);
            _logger.LogInformation("Sent '{RoutingKey}' email for booking {BookingId} (message {MessageId}).",
                delivery.RoutingKey, message.BookingId, message.MessageId);
        }
        private static bool IsPermanent(Exception ex) => ex is FormatException or NotSupportedException;
        private static Task RejectAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken ct) =>
            channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct).AsTask();
    }
}
