using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.UrlShortner.Observers;

namespace Orleans.UrlShortner.Grains;

public interface IDomainStatisticsGrain : IGrainWithStringKey, IGrainObserver
{
    Task Initialize();
    Task RegisterNew();
    Task RegisterExpiration();
    [ReadOnly]
    Task<int> GetTotal();
    [ReadOnly]
    Task<int> GetNumberOfActiveShortenedRouteSegment();
}

public class DomainStatisticsGrain : Grain, IDomainStatisticsGrain
[GenerateSerializer]
public class DomainStatisticsState
{
    [Id(0)]
    public int TotalActivations { get; set; }
    [Id(1)]
    public int NumberOfActiveShortenedRouteSegment { get; set; }
}

public class DomainStatisticsGrain : Grain, IDomainStatisticsGrain
{
    private readonly IPersistentState<DomainStatisticsState> state;
    private readonly ILogger<IDomainGrain> logger;

    public DomainStatisticsGrain(ILogger<IDomainStatisticsGrain> logger)
    {
        this.state = state;
        this.logger = logger;
    }

    public Task Initialize()
    {
        var friend = GrainFactory.GetGrain<IRegistrationObserversManager>(0);
        var obj = this.AsReference<IDomainStatisticsGrain>();
        return friend.Subscribe(obj);
    }

    public Task<int> GetTotal()
    {
        logger.LogInformation("Total activations: {TotalActivations}.", this.state.State.TotalActivations);

        return Task.FromResult(this.state.State.TotalActivations);
    }

    public Task<int> GetNumberOfActiveShortenedRouteSegment()
    {
        logger.LogInformation("Total number of active shortened route segment: {NumberOfActiveShortenedRouteSegment}.", this.state.State.NumberOfActiveShortenedRouteSegment);

        return Task.FromResult(this.state.State.NumberOfActiveShortenedRouteSegment);
    }

    public Task RegisterNew()
    {
        logger.LogInformation($"New activation registered!");

        this.state.State.TotalActivations++;
        this.state.State.NumberOfActiveShortenedRouteSegment++;

        return state.WriteStateAsync();
    }

    public Task RegisterExpiration()
    {
        logger.LogInformation($"Activation expired!");

        this.state.State.NumberOfActiveShortenedRouteSegment--;

        return state.WriteStateAsync();
    }
}
