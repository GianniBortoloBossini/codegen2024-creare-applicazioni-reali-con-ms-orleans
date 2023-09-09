namespace Orleans.UrlShortner.Filters;

public class IncomingLoggingCallFilter : IIncomingGrainCallFilter
{
    private const string CALL_INFO = ">>> INCOMING FILTERS!!! >>> [grainId={0}][grainType={1}][methodName={2}]";
    private readonly ILogger logger;

    public IncomingLoggingCallFilter(ILogger<IncomingLoggingCallFilter> logger)
    {
        this.logger = logger;
    }

public async Task Invoke(IIncomingGrainCallContext context)
    {
        try
        {
            if (IsApplicationInterface(context))
                logger.LogInformation(CALL_INFO,
                                context.TargetContext.GrainId.Key,
                                context.InterfaceType,
                                context.InterfaceMethod.Name);

            await context.Invoke();

            if (IsApplicationInterface(context))
                logger.LogInformation($"{CALL_INFO}[result={{3}}]",
                context.TargetContext.GrainId.Key,
                context.InterfaceType,
                context.InterfaceMethod.Name,
                context.Result);
        }
        catch (Exception exception)
        {
            logger.LogError($"{CALL_INFO}[exc={{3}}]",
                context.TargetContext.GrainId,
                context.InterfaceType,
                context.InterfaceMethod.Name,
                exception);

            throw;
        }
    }

    private static bool IsApplicationInterface(IIncomingGrainCallContext context) 
        => context.InterfaceType != null && context.InterfaceType.ToString().StartsWith("Orleans.UrlShortner");
}
