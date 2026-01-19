using ECommerce.OrderService.Data;
using ECommerce.OrderService.Kafka.Consumers;
using ECommerce.OrderService.Kafka.Handlers;
using ECommerce.OrderService.Mapping;
using ECommerce.OrderService.Repositories;
using ECommerce.OrderService.Service;
using ECommerce.Shared.Kafka.Configuration;
using ECommerce.Shared.Kafka.Consumer;
using ECommerce.Shared.Kafka.Events;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services 

builder.Services.AddHealthChecks();
builder.Services.AddControllers();

builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(action=>
{
    action.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "ECommerce Order Service API",
        Version = "v1"
    });
});

builder.Services.AddDbContext<OrderDbContext>(options=>
    options.UseInMemoryDatabase("orderdb"));//scoped by default

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Configure Kafka
builder.Services.AddKafkaProducer(builder.Configuration);
builder.Services.AddScoped<IEventHandler<UserCreatedEvent>, UserCreatedEventHandler>();
builder.Services.AddHostedService<UserCreatedEventConsumer>();

var app = builder.Build();
// Configure middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();
// Configure swagger only in development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(config=>
    {
        config.SwaggerEndpoint("/swagger/v1/swagger.json", "ECommerce Order Service API V1");
    });
} 
app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
//app.UseAuthentication();
//app.UseAuthorization();
app.MapControllers();
app.UseHealthChecks("/health");


app.Run();
