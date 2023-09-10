using Orleans.Concurrency;
using Orleans.UrlShortner.Observers;

namespace Orleans.UrlShortner.Grains;

public interface IUrlShortnerStatisticsGrain : IGrainWithStringKey, IGrainObserver
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

public class UrlShortnerStatisticsGrain : Grain, IUrlShortnerStatisticsGrain
{
    private readonly ILogger<UrlShortnerStatisticsGrain> logger;

    public UrlShortnerStatisticsGrain(ILogger<UrlShortnerStatisticsGrain> logger)
    {
        this.logger = logger;
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        await base.OnDeactivateAsync(reason, token);

        var friend = GrainFactory.GetGrain<IRegistrationObserversManager>(0);
        var obj = this.AsReference<UrlShortnerStatisticsGrain>();
        await friend.Unsubscribe(obj);
    }

    public int TotalActivations { get; set; }
    public int NumberOfActiveShortenedRouteSegment { get; set; }

    public Task Initialize()
    {
        var friend = GrainFactory.GetGrain<IRegistrationObserversManager>(0);
        var obj = this.AsReference<IUrlShortnerStatisticsGrain>();
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
