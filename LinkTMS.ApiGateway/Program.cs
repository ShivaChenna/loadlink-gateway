using LinkTMS.ApiGateway.Helpers;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Ocelot.Authorization;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);
builder.Logging.AddConsole();

var configuration = builder.Configuration;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
configuration.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);
configuration.AddJsonFile($"ocelot.{env}.json");
configuration.AddEnvironmentVariables();

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue;
});
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = int.MaxValue;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;    
});

builder.Services.AddAuthenticationConfig(configuration);
builder.Services.AddOcelot(configuration);
builder.Services.AddLogging();
builder.Services.AddHttpContextAccessor();

builder.Services.DecorateClaimAuthoriser();

// cors
builder.Services.AddCors(options =>
{
    var allowedCors = configuration.GetSection("AllowedCors").Get<string[]>();
    options.AddPolicy("CorsPolicy", builder => builder
        .WithOrigins(allowedCors)
        .SetIsOriginAllowedToAllowWildcardSubdomains()
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
    );
});

var app = builder.Build();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("CorsPolicy");
app.UseOcelot().Wait();
app.MapGet("/", () => "LinkTMS Gateway");

app.Run();
