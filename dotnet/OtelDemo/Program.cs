using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OtelDemo.Configs;
using OtelDemo.Infrastructure;
using OtelDemo.Repositories;
using OtelDemo.Services;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

var otlpEndpoint = builder.Configuration["OTLP_ENDPOINT_URL"] ?? "http://localhost:4317/";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = otlpEndpoint;
        options.Protocol = OtlpProtocol.Grpc;
    })
    .CreateLogger();
try
{
    string serviceName = builder.Environment.ApplicationName;
    
    var redisOptions = builder.Configuration.GetSection("Redis").Get<RedisOptions>();
    if (redisOptions == null)
    {
        throw new InvalidOperationException("Redis configuration is missing.");
    }
    if (string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
    {
        throw new InvalidOperationException("Redis connection string is not configured.");
    }
    var redisManager = new RedisManager(redisOptions);

    var otel = builder.Services.AddOpenTelemetry();
    otel.ConfigureResource(resource => resource
        .AddService(serviceName));

    otel.WithMetrics(metrics =>
    {
        // Metrics provider from OpenTelemetry
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddProcessInstrumentation();
        metrics.AddRuntimeInstrumentation();
        metrics.AddHttpClientInstrumentation();
        // Metrics provides by ASP.NET Core in .NET 8
        metrics.AddMeter("Microsoft.AspNetCore.Hosting");
        metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
        // Metrics provided by System.Net libraries
        metrics.AddMeter("System.Net.Http");
        metrics.AddMeter("System.Net.NameResolution");
        //metrics.AddConsoleExporter();
        metrics.AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri(otlpEndpoint);
            o.Protocol = OtlpExportProtocol.Grpc;
        });
        metrics.AddPrometheusExporter();
    });

    ActivityService.Initialize(serviceName, "1.0.1");

    // Add Tracing for ASP.NET Core and our custom ActivitySource and export to Jaeger
    otel.WithTracing(tracing =>
    {
        var probability = builder.Configuration?.GetValue<double>("OTEL_TRACES_SAMPLER_ARG") ?? 1.0;
        tracing.SetSampler(new TraceIdRatioBasedSampler(probability));
        tracing.AddSource(ActivityService.Name);
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddRedisInstrumentation(
            redisManager.GetConnection(),
            options => options.SetVerboseDatabaseStatements = true);
        tracing.AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri(otlpEndpoint);
            o.Protocol = OtlpExportProtocol.Grpc;
        });
        //tracing.AddConsoleExporter();
    });

    builder.Services.AddSerilog();
    builder.Services
        .Configure<JwtCreationOptions>(o => o.SigningKey = JwtKey.SigningKey)
        .AddAuthenticationJwtBearer(s => s.SigningKey = JwtKey.SigningKey)
        .AddAuthorization()
        .AddFastEndpoints()
        .SwaggerDocument(); //define a swagger doc - v1 by default

    builder.Services.AddTransient<IAuthService, AuthService>();
    builder.Services.AddSingleton(redisOptions);
    builder.Services.AddSingleton(redisManager);
    builder.Services.AddTransient<IUserRepository, UserRepository>();

    var app = builder.Build();
    app.UseAuthentication()
        .UseAuthorization()
        .UseFastEndpoints();

    if (app.Environment.IsDevelopment())
    {
        //scalar by default looks for the swagger json file here: 
        app.UseOpenApi(c => c.Path = "/openapi/{documentName}.json");
        // http://localhost:{port}/scalar/v1
        app.MapScalarApiReference();
    }

    app.MapPrometheusScrapingEndpoint();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
