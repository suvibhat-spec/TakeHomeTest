using ECommerce.UserService.Data;
using ECommerce.UserService.Mapping;
using ECommerce.UserService.Repositories;
using ECommerce.UserService.Services;
using ECommerce.UserService.Kafka.Handlers;
using ECommerce.UserService.Kafka.Consumers;
using ECommerce.Shared.Kafka.Consumer;
using ECommerce.Shared.Kafka.Configuration;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ECommerce.Shared.Kafka.Events;
using ECommerce.Shared.Middleware;
using ECommerce.UserService.Model;
using Confluent.Kafka;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext());

// Add services 
builder.Services.AddHealthChecks();

builder.Services.AddControllers();

builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(action=>
{
    action.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "ECommerce User Service API",
        Version = "v1"
    });
});

builder.Services.AddDbContext<UserDbContext>(options=>
    options.UseInMemoryDatabase("userdb"));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

// Configure Kafka
builder.Services.AddKafkaProducer(builder.Configuration);
builder.Services.AddScoped<IEventHandler<OrderCreatedEvent>, OrderCreatedEventHandler>();
builder.Services.AddHostedService<OrderCreatedEventConsumer>();

var app = builder.Build();

// Configure middleware

// handle unhandled exceptions
app.UseMiddleware<ExceptionHandlingMiddleware>();
// add correlation ID to requests
app.UseMiddleware<CorrelationIdMiddleware>();
// Configure swagger only in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(config=>
    {
        config.SwaggerEndpoint("/swagger/v1/swagger.json", "ECommerce User Service API V1");
    });
} 

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
//app.UseAuthentication();
//app.UseAuthorization();
app.MapControllers();
app.UseHealthChecks("/health");



app.Run();
