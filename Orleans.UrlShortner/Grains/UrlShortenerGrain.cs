using Orleans.Runtime;
using Orleans.UrlShortner.Infrastructure.Exceptions;

namespace Orleans.UrlShortner.Grains;

public interface IUrlShortenerGrain : IGrainWithStringKey
{
    Task CreateShortUrl(string fullUrl, bool? isOneShoot, int? validFor);
    Task<string> GetUrl();
}

public class UrlShortenerGrain : Grain, IUrlShortenerGrain, IRemindable
{
    private string FullUrl { get; set; }
    private bool IsOneShoot { get; set; }
    private int ValidFor { get; set; }
    private DateTime Expiration { get; set; }
    private int Invocations { get; set; }

    private IGrainReminder _reminder = null;
    private IDisposable _timer = null;
    private readonly ILogger<UrlShortenerGrain> logger;

    public UrlShortenerGrain(ILogger<UrlShortenerGrain> logger)
    {
        this.logger = logger;
    }

    public async Task CreateShortUrl(string fullUrl, bool? isOneShoot, int? validFor)
    {
        this.FullUrl = fullUrl;
        this.IsOneShoot = isOneShoot ?? false;
        this.ValidFor = validFor ?? 60;
        this.Expiration = DateTime.UtcNow.AddSeconds(ValidFor);

        var statsGrain = GrainFactory.GetGrain<IUrlShortnerStatisticsGrain>("url_shortner_statistics");
        await statsGrain.RegisterNew();

        if (ValidFor >= 60)
        {
            _reminder = await this.RegisterOrUpdateReminder("shortenedRouteSegmentExpired",
               TimeSpan.Zero,
               TimeSpan.FromSeconds(ValidFor));
        }
        else
        {
            _timer = this.RegisterTimer(ReceiveTimer, null,
                TimeSpan.FromSeconds(ValidFor),
                TimeSpan.FromSeconds(ValidFor));
        }
    }

    public async Task<string> GetUrl()
    {
        this.Invocations += 1;

        if (string.IsNullOrWhiteSpace(this.FullUrl)) { throw new ShortenedRouteSegmentNotFound(); }
        if (IsOneShoot && this.Invocations > 1) { throw new InvocationExcedeedException(); }
        if (DateTime.UtcNow > this.Expiration) { throw new ExpiredShortenedRouteSegmentException(); }

        if(IsOneShoot && this.Invocations == 1 && _reminder is not null)
            await ShortenedRouteSegmentExpired();

        return this.FullUrl;
    }

    public Task ReceiveReminder(string reminderName, TickStatus status)
    {
        return reminderName switch
        {
            "shortenedRouteSegmentExpired" => ShortenedRouteSegmentExpired(),
            _ => Task.CompletedTask
        };
    }

    private async Task ShortenedRouteSegmentExpired()
    {
        if (_reminder is not null)
        {
            await this.UnregisterReminder(_reminder);
            _reminder = null;
        }

        var statsGrain = GrainFactory.GetGrain<IUrlShortnerStatisticsGrain>("url_shortner_statistics");
        await statsGrain.RegisterExpiration();
    }

    private Task ReceiveTimer(object _)
    {
        if(_timer is null)
            return Task.CompletedTask;

        logger.LogInformation("ReceiveTimer invoked in grain with ID {0}", this.GetPrimaryKeyString());

        _timer?.Dispose();
        logger.LogInformation("Timer disposed from grain with ID {0}", this.GetPrimaryKeyString());

        var statsGrain = GrainFactory.GetGrain<IUrlShortnerStatisticsGrain>("url_shortner_statistics");
        return statsGrain.RegisterExpiration();
    }
}
