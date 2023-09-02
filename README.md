# Orleans.UrlShortner

Progetto per la realizzazione di un applicativo UrlShortener.

## Setup del progetto

Per inizializzare il progetto è necessario
- Creazione del file di soluzione
  `dotnet new sln -n Orleans.UrlShortner -o .\Orleans.UrlShortner\`
- Mi sposto nella cartella creata 
  `cd Orleans.UrlShortner`
- Creazione del progetto Minimal API
  `dotnet new web`
- All'interno del file `Program.cs` elimino l'endpoint `app.MapGet("/", () => "Hello World!");`
- Aggiungo il pacchetto Nuget `dotnet add package Microsoft.Orleans.Server`
- Per aggiungere un silo alla nostra app ci servirà il seguente codice
  ```
  builder.Host.UseOrleans(siloBuilder =>
  {
    if (builder.Environment.IsDevelopment())
    {
        siloBuilder.UseLocalhostClustering();
    }
  });
  ```
- Sempre nel file Program.cs aggiungo le seguenti rotte
  ```
  app.MapPost("/shorten",
    async (IGrainFactory grains, HttpRequest request, [FromQuery] string url) =>
    {
        var host = $"{request.Scheme}://{request.Host.Value}";

        // Validate the URL query string.
        if (string.IsNullOrWhiteSpace(url) && Uri.IsWellFormedUriString(url, UriKind.Absolute) is false)
            return Results.BadRequest($"""
                The URL query string is required and needs to be well formed.
                Consider, ${host}/shorten?https://www.microsoft.com
                """);

        // Create a unique, short ID
        var shortenedRouteSegment = Guid.NewGuid().GetHashCode().ToString("X");

        // Create and persist a grain with the shotened ID and full URL
        var shortenerGrain = grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);
        await shortenerGrain.SetUrl(url);

        // Return the shortened URL for later use
        var resultBuilder = new UriBuilder(host)
        {
            Path = $"/go/{shortenedRouteSegment}"
        };

        return Results.Ok(resultBuilder.Uri);
    });
  ```
  ```
  app.MapGet("/go/{shortenedRouteSegment:required}",
    async (IGrainFactory grains, string shortenedRouteSegment) =>
    {
        // Retrieve the grain using the shortened ID and url to the original URL
        var shortenedGrain = grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);
        var url = await shortenedGrain.GetUrl();

        if (url is null) return Results.BadRequest();

        return Results.Redirect(url);
    });
  ```