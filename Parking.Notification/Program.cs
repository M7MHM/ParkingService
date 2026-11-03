using Microsoft.Extensions.DependencyInjection;
using Parking.Notification;

var builder = Host.CreateApplicationBuilder(args);
var config = builder.Configuration;

var rabbit = new RabbitMqSettings(
    Host: Required(config, "RabbitMq:Host"),
    Port: int.TryParse(config["RabbitMq:Port"], out var port) ? port : 5672,
    User: Required(config, "RabbitMq:User"),
    Password: Required(config, "RabbitMq:Password"),
    VirtualHost: config["RabbitMq:VirtualHost"] ?? "/");

var smtp = new SmtpSettings(
    Host: config["Smtp:Host"] ?? "localhost",
    Port: int.TryParse(config["Smtp:Port"], out var smtpPort) ? smtpPort : 1025,
    From: config["Smtp:From"] ?? "no-reply@parking.local",
    User: config["Smtp:User"],
    Password: config["Smtp:Password"],
    UseSsl: bool.TryParse(config["Smtp:UseSsl"], out var ssl) && ssl);

var redisConnection = config["Redis:ConnectionString"]
    ?? "localhost:6379,abortConnect=false,connectRetry=1,connectTimeout=1000,syncTimeout=750,asyncTimeout=750";

builder.Services.AddSingleton(rabbit);
builder.Services.AddSingleton(smtp);
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "ParkingNotifications:";
});
builder.Services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<NotificationConsumer>();

await builder.Build().RunAsync();

static string Required(IConfiguration configuration, string key) =>
    string.IsNullOrWhiteSpace(configuration[key])
        ? throw new InvalidOperationException($"Missing configuration value: {key}")
        : configuration[key]!;

