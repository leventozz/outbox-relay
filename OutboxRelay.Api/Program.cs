using Microsoft.EntityFrameworkCore;
using OutboxRelay.Api.Middlewares;
using OutboxRelay.Application.Transactions;
using OutboxRelay.Common.Configuration;
using OutboxRelay.Infrastructure.Models;
using OutboxRelay.Infrastructure.Repositories.Outboxes;
using OutboxRelay.Infrastructure.Repositories.Transactions;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MSSQL")));
builder.Services.Configure<OutboxSettings>(builder.Configuration.GetSection("OutboxSettings"));

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
