namespace Orleans.UrlShortner.Filters;

public class OutgoingLoggingCallFilter : IOutgoingGrainCallFilter
{
    private const string CALL_INFO = "OUTGOING FILTERS!!! >>> [sourceGrainId={0}][targetGrainId={1}][targetGraintType={2}][targetGrainMethodName={3}]";
    private readonly ILogger logger;

    public OutgoingLoggingCallFilter(ILogger<OutgoingLoggingCallFilter> logger)
    {
        this.logger = logger;
    }

    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        try
        {
            if (context.InterfaceType.ToString().StartsWith("Orleans.UrlShortner"))
                logger.LogInformation(CALL_INFO,
                GetSourceId(context),
                context.TargetId.Key,
                context.InterfaceType,
                context.InterfaceMethod.Name);

            await context.Invoke();

            if (context.InterfaceType.ToString().StartsWith("Orleans.UrlShortner"))
                logger.LogInformation($"{CALL_INFO}[result={{4}}]",
                GetSourceId(context),
                context.TargetId.Key,
                context.InterfaceType,
                context.InterfaceMethod.Name,
                context.Result);
        }
        catch (Exception exception)
        {
            logger.LogError($"{CALL_INFO}[exc={{4}}]",
                GetSourceId(context),
                context.TargetId.Key,
                context.InterfaceType,
                context.InterfaceMethod.Name,
                exception);

            throw;
        }
    }

    private static object GetSourceId(IOutgoingGrainCallContext context)
        => context.SourceId.HasValue ? context.SourceId.Value.Key : "<none>";
}
