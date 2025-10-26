using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OutboxRelay.Application.Abstractions;
using OutboxRelay.Application.Features.Consumers;
using OutboxRelay.Common.Configuration;
using OutboxRelay.Common.Messaging;
using OutboxRelay.ConsumerWorkerService;
using OutboxRelay.Core.Models;
using OutboxRelay.Infrastructure;
using OutboxRelay.Infrastructure.Publisher;
using OutboxRelay.Infrastructure.Repositories.Outboxes;
using OutboxRelay.Infrastructure.Repositories.Transactions;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMqSettings"));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("MSSQL"));
});

builder.Services.AddSingleton<ConnectionFactory>(sp =>
{
    var rabbitMqSettings = sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
    return new ConnectionFactory()
    {
        HostName = rabbitMqSettings.HostName,
        Port = rabbitMqSettings.Port,
        UserName = rabbitMqSettings.UserName,
        Password = rabbitMqSettings.Password
    };
});

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IMessageHandler<CreateTransactionMessage>, TransactionConsumedHandler>();
builder.Services.AddSingleton<RabbitMqClientService>();

builder.Services.AddHostedService<ConsumerWorkerService>();

var host = builder.Build();
host.Run();
