using Orleans.Concurrency;

namespace Orleans.UrlShortner.Grains.Stateless;

public interface IShortenedRouteSegmentStatelessWorker : IGrainWithIntegerKey
{
    Task<string> Create();
}

[StatelessWorker]
public class ShortenedRouteSegmentStatelessWorker : Grain, IShortenedRouteSegmentStatelessWorker
{
    public Task<string> Create()
        => Task.FromResult(Guid.NewGuid().GetHashCode().ToString("X"));
}
