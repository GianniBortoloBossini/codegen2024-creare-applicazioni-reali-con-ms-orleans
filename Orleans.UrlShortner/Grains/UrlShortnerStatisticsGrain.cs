using Orleans.Concurrency;

namespace Orleans.UrlShortner.Grains;

public interface IUrlShortnerStatisticsGrain : IGrainWithStringKey
{
    //[OneWay]
    Task RegisterNew();
    //[ReadOnly]
    Task<int> GetTotal();
}

public class UrlShortnerStatisticsGrain : Grain, IUrlShortnerStatisticsGrain
{
    private readonly ILogger<UrlShortnerStatisticsGrain> logger;

    public UrlShortnerStatisticsGrain(ILogger<UrlShortnerStatisticsGrain> logger)
    {
        this.logger = logger;
    }

    public int TotalActivations { get; set; }

    public Task<int> GetTotal()
    {
        logger.LogInformation("Total activations: {TotalActivations}.", TotalActivations);

        return Task.FromResult(TotalActivations);
    }

    public Task RegisterNew()
    {
        logger.LogInformation($"New activation registered!.");

        this.TotalActivations++;
        return Task.CompletedTask;
    }
}
