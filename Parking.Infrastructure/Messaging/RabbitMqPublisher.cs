using Microsoft.Extensions.Logging;
using Parking.Contracts;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Infrastructure.Messaging
{
    internal sealed class RabbitMqPublisher : IAsyncDisposable
    {
        private readonly RabbitMqSettings _settings;
        private readonly ILogger<RabbitMqPublisher> _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMqPublisher(RabbitMqSettings settings, ILogger<RabbitMqPublisher> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public async Task PublishAsync(string routingKey, Guid messageId, string payload, CancellationToken ct)
        {
            await _gate.WaitAsync(ct);
            try
            {
                var channel = await GetChannelAsync(ct);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    MessageId = messageId.ToString(),
                    Type = routingKey
                };

                await channel.BasicPublishAsync(
                    exchange: MessageRoutes.Exchange,
                    routingKey: routingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: Encoding.UTF8.GetBytes(payload),
                    cancellationToken: ct);
            }
            catch
            {
                await ResetAsync();
                throw;
            }
            finally
            {
                _gate.Release();
            }
        }
        private async Task<IChannel> GetChannelAsync(CancellationToken ct)
        {
            if (_channel is { IsOpen: true })
                return _channel;

            await ResetAsync();

            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.User,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                ClientProvidedName = "parking-api"
            };

            _connection = await factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(
                new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true),
                ct);

            await _channel.ExchangeDeclareAsync(
                exchange: MessageRoutes.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);

            _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}.", _settings.Host, _settings.Port);
            return _channel;
        }
        private async Task ResetAsync()
        {
            try { if (_channel is not null) await _channel.DisposeAsync(); } catch { }
            try { if (_connection is not null) await _connection.DisposeAsync(); } catch { }
            _channel = null;
            _connection = null;
        }
        public async ValueTask DisposeAsync()
        {
            await ResetAsync();
            _gate.Dispose();
        }
    }
}
