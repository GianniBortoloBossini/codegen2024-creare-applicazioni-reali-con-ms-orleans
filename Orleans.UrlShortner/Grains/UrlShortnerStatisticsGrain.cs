using Orleans.Concurrency;

namespace Orleans.UrlShortner.Grains;

public interface IUrlShortnerStatisticsGrain : IGrainWithStringKey
{
    [OneWay]
    Task RegisterNew();
    [OneWay]
    Task RegisterExpiration();
    [ReadOnly]
    Task<int> GetTotal();
    [ReadOnly]
    Task<int> GetNumberOfActiveShortenedRouteSegment();
}

public class UrlShortnerStatisticsGrain : Grain, IUrlShortnerStatisticsGrain
{
    private readonly ILogger<UrlShortnerStatisticsGrain> logger;

    public UrlShortnerStatisticsGrain(ILogger<UrlShortnerStatisticsGrain> logger)
    {
        this.logger = logger;
    }

    public int TotalActivations { get; set; }
    public int NumberOfActiveShortenedRouteSegment { get; set; }

    public Task<int> GetTotal()
    {
        logger.LogInformation("Total activations: {TotalActivations}.", TotalActivations);

        return Task.FromResult(TotalActivations);
    }

    public Task<int> GetNumberOfActiveShortenedRouteSegment()
    {
        logger.LogInformation("Total number of active shortened route segment: {NumberOfActiveShortenedRouteSegment}.", NumberOfActiveShortenedRouteSegment);

        return Task.FromResult(NumberOfActiveShortenedRouteSegment);
    }

    public Task RegisterNew()
    {
        logger.LogInformation($"New activation registered!");

        this.TotalActivations++;
        this.NumberOfActiveShortenedRouteSegment++;

        return Task.CompletedTask;
    }

    public Task RegisterExpiration()
    {
        logger.LogInformation($"Activation expired!");

        this.NumberOfActiveShortenedRouteSegment--;

        return Task.CompletedTask;
    }
}
