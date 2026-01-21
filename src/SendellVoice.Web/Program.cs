using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using SendellVoice.Application;
using SendellVoice.Application.Common.Interfaces;
using SendellVoice.Infrastructure;
using SendellVoice.Infrastructure.Data;
using SendellVoice.Web.HealthChecks;
using SendellVoice.Web.Middleware;
using Serilog;
using Serilog.Events;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/sendellvoice-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    Log.Information("Starting SendellVoice API");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "SendellVoice API",
            Version = "v1",
            Description = "Intelligent Contact Center API with speech processing, intent classification, and RAG capabilities"
        });

        // Add API key header
        options.AddSecurityDefinition("ApiKey", new()
        {
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Name = "X-API-Key",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "API Key for authentication"
        });

        options.AddSecurityRequirement(new()
        {
            {
                new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "ApiKey" } },
                Array.Empty<string>()
            }
        });
    });

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Add HttpClient for health checks
    builder.Services.AddHttpClient();

    // Add Application and Infrastructure services
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // Add exception handler
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Add health checks
    var healthCheckBuilder = builder.Services.AddHealthChecks();

    // Add SQL Server health check if connection string is configured
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connectionString))
    {
        healthCheckBuilder.AddSqlServer(connectionString, name: "sqlserver", tags: new[] { "db", "ready" });
    }

    // Add provider-specific health checks
    var llmProvider = builder.Configuration["Providers:LLM"] ?? "Ollama";
    var vectorProvider = builder.Configuration["Providers:VectorStore"] ?? "InMemory";

    if (llmProvider == "Ollama")
    {
        healthCheckBuilder.AddCheck<OllamaHealthCheck>("ollama", tags: new[] { "ai", "ready" });
    }

    if (vectorProvider == "Qdrant")
    {
        healthCheckBuilder.AddCheck<QdrantHealthCheck>("qdrant", tags: new[] { "vectordb", "ready" });
    }

    healthCheckBuilder.AddCheck<VectorStoreHealthCheck>("vectorstore", tags: new[] { "vectordb" });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "SendellVoice API v1");
            options.RoutePrefix = "swagger";
        });
    }

    // Use Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "{RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    // Exception handling
    app.UseExceptionHandler();

    // CORS
    app.UseCors();

    // API Key authentication (optional based on configuration)
    app.UseApiKeyAuth();

    app.UseAuthorization();

    // Health check endpoints
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false // Just checks if app is running
    });

    app.MapControllers();

    // Root endpoint
    app.MapGet("/", () => Results.Ok(new
    {
        name = "SendellVoice API",
        version = "1.0.0",
        status = "running",
        docs = "/swagger"
    }));

    // Apply migrations on startup (development only)
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            await dbContext.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not apply migrations. Database may not be available yet.");
        }

        // Initialize vector store
        var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStoreService>();
        try
        {
            await vectorStore.InitializeAsync();
            Log.Information("Vector store initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not initialize vector store");
        }
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
