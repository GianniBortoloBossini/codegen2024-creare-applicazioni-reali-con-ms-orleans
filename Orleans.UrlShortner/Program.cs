using Microsoft.AspNetCore.Mvc;
using Orleans.UrlShortner.Grains;
using Orleans.UrlShortner.Infrastructure.Exceptions;
using Orleans.UrlShortner.Models;

var builder = WebApplication.CreateBuilder(args);

// *** REGISTRAZIONE E CONFIGURAZIONE DI ORLEANS ***
builder.Host.UseOrleans(siloBuilder =>
{
    if (builder.Environment.IsDevelopment())
    {
        siloBuilder.UseLocalhostClustering();
    }
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
    async ([FromBody] PostCreateUrlShortnerModel.Request data, IGrainFactory grains, HttpRequest request) =>
    {
        var host = $"{request.Scheme}://{request.Host.Value}";

        // validazione del campo Url
        if (string.IsNullOrWhiteSpace(data.Url) && Uri.IsWellFormedUriString(data.Url, UriKind.Absolute) is false)
            return Results.BadRequest($"Valore del campo URL non valido.");

        // Creazione di un ID univoco
        var shortenedRouteSegment = Guid.NewGuid().GetHashCode().ToString("X");

        // Creazione del grano 
        var shortenerGrain = grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);
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
    async (IGrainFactory grains, string shortenedRouteSegment) =>
    {
        // Recupero della reference al grano identificato dall'ID univoco
        var shortenedGrain = grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);
        
        try
        {
            // Recupero dell'url dal grano e redirect
            var url = await shortenedGrain.GetUrl();
            return Results.Redirect(url);
        }
        catch (ExpiredShortenedRouteSegmentException) { return Results.NotFound(); }
        catch (InvocationExcedeedException) { return Results.NotFound(); }
    })
    .WithName("Go")
    .WithDescription("Endpoint per il recupero dell'url abbreviato")
    .WithOpenApi();

app.MapGet("/statistics",
    async (IGrainFactory grains) =>
    {
        // Recupero della reference al grano identificato dall'ID univoco
        var shortenedGrain = grains.GetGrain<IUrlShortnerStatisticsGrain>("url_shortner_statistics");

        // Recupero della statistiche tramite metodo GetTotal del grano
        var total = await shortenedGrain.GetTotal();

        return Results.Ok(new GetStatisticsModel.Response { TotalActivations = total });
    })
    .WithName("Statistics")
    .WithDescription("Endpoint per il recupero delle statistiche")
    .WithOpenApi(); ;

app.Run();
