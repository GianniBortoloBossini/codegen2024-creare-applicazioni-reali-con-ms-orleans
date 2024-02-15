using Orleans.Concurrency;

namespace Orleans.UrlShortner.StatelessWorkers;

public interface IShortenedRouteSegmentStatelessWorker : IGrainWithIntegerKey, IIncomingGrainCallFilter
{
    Task<string> Create(string url);
}

[StatelessWorker]
public class ShortenedRouteSegmentStatelessWorker : Grain, IShortenedRouteSegmentStatelessWorker
{
    public Task<string> Create(string url)
        => Task.FromResult(Guid.NewGuid().GetHashCode().ToString("X"));

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (context.InterfaceMethod.Name == "Create" && context.Request.GetArgument(0) is not null)
            context.Request.SetArgument(0, context.Request.GetArgument(0).ToString().ToLower());

        await context.Invoke();

        if (context.InterfaceMethod.Name == "Create" &&
            context.Request.GetArgument(0) is not null &&
            context.Request.GetArgument(0).ToString().ToLower().StartsWith("http://"))
            context.Result = $"UNSAFE_{context.Result}";
    }
}
