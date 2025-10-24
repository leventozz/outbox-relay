using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OutboxRelay.Api.Middlewares;
using OutboxRelay.Application.Transactions;
using OutboxRelay.Common.Configuration;
using OutboxRelay.Infrastructure.Models;
using OutboxRelay.Infrastructure.Publisher;
using OutboxRelay.Infrastructure.Repositories.Outboxes;
using OutboxRelay.Infrastructure.Repositories.Transactions;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MSSQL")));

builder.Services.Configure<OutboxSettings>(builder.Configuration.GetSection("OutboxSettings"));
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));


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
builder.Services.AddSingleton<RabbitMqClientService>();
builder.Services.AddScoped<IRabbitMqPublisher, RabbitMqPublisher>();

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<ITransactionApplication, TransactionApplication>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
