using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.API.Middleware;
using OrderProcessing.Application.DTOs;
using OrderProcessing.Application.Handlers;
using OrderProcessing.Application.Validators;
using OrderProcessing.Domain.Interfaces;
using OrderProcessing.Infrastructure.Data;
using OrderProcessing.Infrastructure.Repositories;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure enhanced Serilog with more detailed logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "OrderProcessing")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/orderprocessing-.txt", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Information)
    .WriteTo.File("logs/errors/orderprocessing-errors-.txt", 
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: LogEventLevel.Error,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers(options =>
{
    // Configure model binding error messages
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor((_) => "This field is required.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((x, y) => $"The value '{x}' is not valid for field '{y}'.");
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor((x) => $"The value '{x}' is not in a valid format.");
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Order Processing Service",
        Version = "v1",
        Description = "A REST API for processing orders following DDD principles"
    });
});

// Add Entity Framework with enhanced error handling
if (builder.Environment.EnvironmentName != "Testing")
{
    var connectionString = builder.Configuration.GetConnectionString("PostgreSQLConnection") 
        ?? "Host=localhost;Database=OrderProcessingDb;Username=postgres;Password=postgres";

    builder.Services.AddDbContext<OrderProcessingDbContext>(options =>
    {
        options.UseNpgsql(connectionString);
        
        // Enable detailed errors in development
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            options.LogTo(Log.Information, LogLevel.Information);
        }
    });
}

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommandHandler).Assembly));

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();

// Add repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Add Global Exception Handler Middleware (must be first)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // This provides detailed error pages in development
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Processing Service V1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Add request logging middleware
app.Use(async (context, next) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    try
    {
        Log.Information("HTTP {RequestMethod} {RequestPath} started", 
            context.Request.Method, context.Request.Path);
        
        await next();
        
        stopwatch.Stop();
        Log.Information("HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms", 
            context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.Elapsed.TotalMilliseconds);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        Log.Error(ex, "HTTP {RequestMethod} {RequestPath} failed after {Elapsed:0.0000} ms", 
            context.Request.Method, context.Request.Path, stopwatch.Elapsed.TotalMilliseconds);
        throw;
    }
});

app.UseAuthorization();
app.MapControllers();

// Ensure database is recreated with the correct schema
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
    
    Log.Information("Checking database schema...");
    
    await context.Database.EnsureCreatedAsync();
    Log.Information("Database created successfully with new schema");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to recreate database. Application will not work properly.");
    throw; // Fail fast if database cannot be created
}

// Log application startup completion
Log.Information("Order Processing Service started successfully on {Environment}", 
    builder.Environment.EnvironmentName);

app.Run();

public partial class Program { } // Make Program class public for testing
