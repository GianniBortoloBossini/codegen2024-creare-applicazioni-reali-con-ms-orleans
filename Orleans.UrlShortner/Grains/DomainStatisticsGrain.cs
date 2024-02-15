using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.UrlShortner.Abstractions;
using Orleans.UrlShortner.Observers;

namespace Orleans.UrlShortner.Grains;

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
    private readonly ILogger<IDomainStatisticsGrain> logger;

    public DomainStatisticsGrain(
        [PersistentState(stateName: "domain-statistics", storageName: "domainstatisticsstorage")] IPersistentState<DomainStatisticsState> state,
        ILogger<IDomainStatisticsGrain> logger)
    {
        this.state = state;
        this.logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var friend = GrainFactory.GetGrain<IRegistrationObserversManager>(0);
        var obj = this.AsReference<IDomainStatisticsGrain>();
        await friend.Subscribe(obj);

        await base.OnActivateAsync(cancellationToken);
    }

    public Task Activate()
        => Task.CompletedTask;

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

        this.state.State.TotalActivations += 1;
        this.state.State.NumberOfActiveShortenedRouteSegment += 1;

        return state.WriteStateAsync();
    }

    public Task RegisterExpiration()
    {
        logger.LogInformation($"Activation expired!");

        this.state.State.NumberOfActiveShortenedRouteSegment -= 1;

        return state.WriteStateAsync();
    }
}
