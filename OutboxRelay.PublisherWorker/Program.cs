using Microsoft.EntityFrameworkCore;
using OutboxRelay.Common.Configuration;
using OutboxRelay.Infrastructure.Models;
using OutboxRelay.Infrastructure.Publisher;
using OutboxRelay.Infrastructure.Repositories.Outboxes;
using OutboxRelay.PublisherWorkerService;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OutboxSettings>(builder.Configuration.GetSection("OutboxSettings"));
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMqSettings"));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("MSSQL"));
});


builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();

builder.Services.AddSingleton<RabbitMqClientService>();
builder.Services.AddScoped<IRabbitMqPublisher, RabbitMqPublisher>();

builder.Services.AddHostedService<PublisherWorkerService>();

var host = builder.Build();
host.Run();
