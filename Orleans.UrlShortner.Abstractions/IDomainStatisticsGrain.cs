using Orleans.Concurrency;

namespace Orleans.UrlShortner.Abstractions;

public interface IDomainStatisticsGrain : IGrainWithStringKey, IGrainObserver
{
    [OneWay]
    Task Activate();
    Task RegisterNew();
    Task RegisterExpiration();
    [ReadOnly]
    Task<int> GetTotal();
    [ReadOnly]
    Task<int> GetNumberOfActiveShortenedRouteSegment();
}
