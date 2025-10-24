using Microsoft.Extensions.Options;
using OutboxRelay.ConsumerWorkerService;
using System;

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

builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();

builder.Services.AddSingleton<RabbitMqClientService>();
builder.Services.AddScoped<IRabbitMqPublisher, RabbitMqPublisher>();

builder.Services.AddHostedService<ConsumerWorkerService>();

var host = builder.Build();
host.Run();
