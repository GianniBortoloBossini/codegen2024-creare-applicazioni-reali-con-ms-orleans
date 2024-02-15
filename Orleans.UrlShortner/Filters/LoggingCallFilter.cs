namespace Orleans.UrlShortner.Filters;

public class LoggingCallFilter : IIncomingGrainCallFilter
{
    private readonly ILogger logger;

    public LoggingCallFilter(ILogger<LoggingCallFilter> logger)
    {
        this.logger = logger;
    }

public async Task Invoke(IIncomingGrainCallContext context)
    {
        try
        {
            if (context.InterfaceType.ToString().StartsWith("Orleans.UrlShortner"))
                logger.LogInformation("""
                    *** >>> INCOMING FILTERS!!! >>> ***
                    *** >>> Per il grain con id={0} di tipo {1} è stato invocato il metodo {2}.
                    *** >>> INCOMING FILTERS!!! >>> ***
                    """,
                                context.TargetContext.GrainId.Key,
                                context.InterfaceType,
                                context.InterfaceMethod.Name,
                                context.Result);

            await context.Invoke();

            if (context.InterfaceType.ToString().StartsWith("Orleans.UrlShortner"))
                logger.LogInformation("""
                    *** <<< INCOMING FILTERS!!! <<< ***
                    *** <<< Per il grain con id={0} di tipo {1} è stato invocato il metodo {2} ed ha restituito {3}.
                    *** <<< INCOMING FILTERS!!! <<< ***
                    """,
                context.TargetContext.GrainId.Key,
                context.InterfaceType,
                context.InterfaceMethod.Name,
                context.Result);
        }
        catch (Exception exception)
        {
            logger.LogError("""
                    *** <<< INCOMING FILTERS!!! <<< ***
                    *** <<< Per il grain con id={0} di tipo {1} è stato invocato il metodo {2} ed ha dato l'eccezione {3}.
                    *** <<< INCOMING FILTERS!!! <<< ***
                    """,
                context.TargetContext.GrainId,
                context.InterfaceType,
                context.InterfaceMethod.Name,
                exception);

            throw;
        }
    }
}
