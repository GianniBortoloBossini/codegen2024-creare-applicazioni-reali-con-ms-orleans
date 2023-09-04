using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Orleans.UrlShortner.Filters;
using Orleans.UrlShortner.Grains;
using Orleans.UrlShortner.Grains.Stateless;
using Orleans.UrlShortner.Infrastructure.Exceptions;
using Orleans.UrlShortner.Models;
using Orleans.UrlShortner.StatelessWorkers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// *** REGISTRAZIONE E CONFIGURAZIONE DI ORLEANS ***
builder.Host.UseOrleans(siloBuilder =>
{
    if (builder.Environment.IsDevelopment())
    {
        siloBuilder.UseLocalhostClustering();
        siloBuilder.ConfigureLogging(loggingConfig =>
        {
            Log.Logger = new LoggerConfiguration()
                        .WriteTo.File("logs/log-.txt", 
                                      outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", 
                                      rollingInterval: RollingInterval.Day)
                        .CreateLogger();

            loggingConfig.AddConsole().AddSerilog(Log.Logger);
        });
        siloBuilder.AddActivityPropagation();

        // ADD SILO-WIDE GRAIN CALL FILTERS
        siloBuilder.AddIncomingGrainCallFilter<LoggingCallFilter>();

        // REGISTRAZIONE REMINDERS
        siloBuilder.UseInMemoryReminderService();
    }

    // Attivo il grano delle statistiche alla partenza dell'applicazione
    siloBuilder.AddStartupTask(
          (IServiceProvider services, CancellationToken cancellation) =>
          {
              var clusterClient = services.GetRequiredService<IClusterClient>();
              var grain = clusterClient.GetGrain<IUrlShortnerStatisticsGrain>("url_shortner_statistics");
              return grain.Initialize();
          });

    siloBuilder.UseDashboard(dashboardConfig =>
    {
        dashboardConfig.Port = 7070;
        dashboardConfig.Username = "admin";
        dashboardConfig.Password = "admin";
    });
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        // Set a service name
        tracing.SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName: "UrlShortner", serviceVersion: "1.0"));

        tracing.AddSource("Microsoft.Orleans.Runtime");
        tracing.AddSource("Microsoft.Orleans.Application");

        tracing.AddZipkinExporter(zipkin =>
        {
            zipkin.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
        });
    });

// Registrazione dei servizi del supporto Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aggiunta del supporto Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/shorten",
    async ([FromBody] PostCreateUrlShortnerModel.Request data, IClusterClient client, HttpRequest request) =>
    {
        var host = $"{request.Scheme}://{request.Host.Value}";

        // validazione del campo Url
        if (string.IsNullOrWhiteSpace(data.Url) && Uri.IsWellFormedUriString(data.Url, UriKind.Absolute) is false)
            return Results.BadRequest($"Valore del campo URL non valido.");

        // Attivazione di un grain legato all'host
        var uri = new Uri(data.Url);
        var domainGrain = client.GetGrain<IDomainStatisticsGrain>(uri.Host);
        await domainGrain.Initialize();

        // Creazione di un ID univoco
        var shortenerRouteSegmentWorker = client.GetGrain<IShortenedRouteSegmentStatelessWorker>(0);
        var shortenedRouteSegment = await shortenerRouteSegmentWorker.Create(data.Url);

        // Creazione del grano 
        var shortenerGrain = client.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);
        await shortenerGrain.CreateShortUrl(data.Url, data.IsOneShoot, data.DurationInSeconds);

        // Creazione della risposta
        var resultBuilder = new UriBuilder(host)
        {
            Path = $"/go/{shortenedRouteSegment}"
        };

        return Results.Ok(new PostCreateUrlShortnerModel.Response { Url = resultBuilder.Uri });
    })
    .WithName("Shorten")
    .WithDescription("Endpoint per l'abbreviazione degli url")
    .Produces<PostCreateUrlShortnerModel.Response>()
    .WithOpenApi();


app.MapGet("/go/{shortenedRouteSegment:required}",
    async (IClusterClient client, string shortenedRouteSegment) =>
    {
        // Recupero della reference al grano identificato dall'ID univoco
        var shortenedGrain = client.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);
        
        try
        {
            // Recupero dell'url dal grano e redirect
            var url = await shortenedGrain.GetUrl();
            return Results.Redirect(url);
        }
        catch (ExpiredShortenedRouteSegmentException) { return Results.BadRequest(); }
        catch (InvocationExcedeedException) { return Results.StatusCode(429); }
        catch (ShortenedRouteSegmentNotFound) { return Results.NotFound(); }
    })
    .WithName("Go")
    .WithDescription("Endpoint per il recupero dell'url abbreviato")
    .WithOpenApi();

var statisticsGroup = app.MapGroup("/statistics");

statisticsGroup.MapGet("/",
    async (IClusterClient client) =>
    {
        // Recupero della reference al grano identificato dall'ID univoco
        var shortenedGrain = client.GetGrain<IUrlShortnerStatisticsGrain>("url_shortner_statistics");

        // Recupero della statistiche tramite metodo GetTotal del grano
        var totalActivations = await shortenedGrain.GetTotal();
        var totalActiveShortenedRouteSegment = await shortenedGrain.GetNumberOfActiveShortenedRouteSegment();

        return Results.Ok(new GetStatisticsModel.Response { 
            TotalActivations = totalActivations,
            TotalActiveShortenedRouteSegment = totalActiveShortenedRouteSegment
        });
    })
    .WithName("Statistics")
    .WithDescription("Endpoint per il recupero delle statistiche")
    .WithOpenApi();

statisticsGroup.MapGet("/{domain}",
    async (IClusterClient client, [FromRoute] string domain) =>
    {
        // Recupero della reference al grano identificato dall'ID univoco
        var domainGrain = client.GetGrain<IDomainStatisticsGrain>(domain);

        // Recupero della statistiche tramite metodo GetTotal del grano
        var totalActivations = await domainGrain.GetTotal();
        var totalActiveShortenedRouteSegment = await domainGrain.GetNumberOfActiveShortenedRouteSegment();

        return Results.Ok(new GetStatisticsModel.Response
        {
            TotalActivations = totalActivations,
            TotalActiveShortenedRouteSegment = totalActiveShortenedRouteSegment
        });
    })
    .WithName("Domain statistics")
    .WithDescription("Endpoint per il recupero delle statistiche per dominio")
    .WithOpenApi();

app.Run();
