namespace Orleans.UrlShortner.Abstractions.StatelessWorkers;

public interface IShortenedRouteSegmentStatelessWorker : IGrainWithIntegerKey, IIncomingGrainCallFilter
{
    Task<string> Create(string url);
}
