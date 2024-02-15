using Orleans.Concurrency;
using Orleans.UrlShortner.Observers;

namespace Orleans.UrlShortner.Grains;

public interface IDomainStatisticsGrain : IGrainWithStringKey, IGrainObserver
{
    Task Initialize();
    [OneWay]
    Task RegisterNew();
    [OneWay]
    Task RegisterExpiration();
    [ReadOnly]
    Task<int> GetTotal();
    [ReadOnly]
    Task<int> GetNumberOfActiveShortenedRouteSegment();
}

public class DomainStatisticsGrain : Grain, IDomainStatisticsGrain
{
    private readonly ILogger<IDomainStatisticsGrain> logger;

    public DomainStatisticsGrain(ILogger<IDomainStatisticsGrain> logger)
    {
        this.logger = logger;
    }

    public int TotalActivations { get; set; }
    public int NumberOfActiveShortenedRouteSegment { get; set; }

    public Task Initialize()
    {
        var friend = GrainFactory.GetGrain<IRegistrationObserversManager>(0);
        var obj = this.AsReference<IDomainStatisticsGrain>();
        return friend.Subscribe(obj);
    }

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
