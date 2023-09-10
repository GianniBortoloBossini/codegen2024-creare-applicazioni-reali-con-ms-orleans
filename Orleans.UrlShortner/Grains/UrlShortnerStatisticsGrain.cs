using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.UrlShortner.Observers;

namespace Orleans.UrlShortner.Grains;

public interface IUrlShortnerStatisticsGrain : IGrainWithStringKey, IGrainObserver
{
    Task Initialize();
    Task RegisterNew();
    Task RegisterExpiration();
    [ReadOnly]
    Task<int> GetTotal();
    [ReadOnly]
    Task<int> GetNumberOfActiveShortenedRouteSegment();
}

[GenerateSerializer]
public class ApplicationStatisticsState
{
    [Id(0)]
    public int TotalActivations { get; set; }
    [Id(1)]
    public int NumberOfActiveShortenedRouteSegment { get; set; }
}

public class UrlShortnerStatisticsGrain : Grain, IUrlShortnerStatisticsGrain
{
    private readonly IPersistentState<ApplicationStatisticsState> state;
    private readonly ILogger<UrlShortnerStatisticsGrain> logger;

    public UrlShortnerStatisticsGrain(
        [PersistentState(stateName: "application-statistics", storageName: "applicationstatisticsstorage")] IPersistentState<ApplicationStatisticsState> state,
        ILogger<UrlShortnerStatisticsGrain> logger)
    {
        this.state = state;
        this.logger = logger;
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        await base.OnDeactivateAsync(reason, token);

        var friend = GrainFactory.GetGrain<IRegistrationObserversManager>(0);
        var obj = this.AsReference<UrlShortnerStatisticsGrain>();
        await friend.Unsubscribe(obj);
    }

    public Task Initialize()
    {
        var friend = GrainFactory.GetGrain<IRegistrationObserversManager>(0);
        var obj = this.AsReference<IUrlShortnerStatisticsGrain>();
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
