using Orleans.Concurrency;
using Orleans.UrlShortner.Observers;

namespace Orleans.UrlShortner.Grains;

public interface IDomainGrain : IGrainWithStringKey, IGrainObserver
{
    Task Initialize();
    Task RegisterNew();
    Task RegisterExpiration();
    [ReadOnly]
    Task<int> GetTotal();
    [ReadOnly]
    Task<int> GetNumberOfActiveShortenedRouteSegment();
}

public class DomainGrain : Grain, IDomainGrain
{
    private readonly ILogger<IDomainGrain> logger;

    public DomainGrain(ILogger<IDomainGrain> logger)
    {
        this.logger = logger;
    }

    public int TotalActivations { get; set; }
    public int NumberOfActiveShortenedRouteSegment { get; set; }

    public Task Initialize()
    {
        var friend = GrainFactory.GetGrain<IRegistrationObserversManager>(0);
        var obj = this.AsReference<IDomainGrain>();
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
