using Orleans.Runtime;
using Orleans.UrlShortner.Infrastructure.Exceptions;
using Orleans.UrlShortner.Observers;

namespace Orleans.UrlShortner.Grains;

public interface IUrlShortenerGrain : IGrainWithStringKey
{
    Task CreateShortUrl(string fullUrl, bool? isOneShoot, int? validFor);
    Task<string> GetUrl();
}

[GenerateSerializer]
public class UrlShortenerState
{
    [Id(0)]
    public string FullUrl { get; set; }
    [Id(1)]
    public bool IsOneShoot { get; set; }
    [Id(2)]
    public int ValidFor { get; set; }
    [Id(3)]
    public DateTime Expiration { get; set; }
    [Id(4)]
    public int Invocations { get; set; }
}

public class UrlShortenerGrain : Grain, IUrlShortenerGrain, IRemindable
{
    private IGrainReminder _reminder = null;
    private IDisposable _timer = null;
    private readonly IPersistentState<UrlShortenerState> state;
    private readonly ILogger<UrlShortenerGrain> logger;

    public UrlShortenerGrain(
        [PersistentState(stateName: "url-shortner", storageName: "urlshortnerstorage")] IPersistentState<UrlShortenerState> state,
        ILogger<UrlShortenerGrain> logger)
    {
        this.state = state;
        this.logger = logger;
    }

    public async Task CreateShortUrl(string fullUrl, bool? isOneShoot, int? validFor)
    {
        this.state.State.FullUrl = fullUrl;
        this.state.State.IsOneShoot = isOneShoot ?? false;
        this.state.State.ValidFor = validFor switch
        {
            var x when x is null || x == 0 => 60,
            _ => validFor.Value
        };
        this.state.State.Expiration = DateTime.UtcNow.AddSeconds(this.state.State.ValidFor);

        var registrationManagerGrain = GrainFactory.GetGrain<IRegistrationObserversManager>(0);
        await registrationManagerGrain.RegisterNew(this.state.State.FullUrl);

        if (this.state.State.ValidFor >= 60)
        {
            _reminder = await this.RegisterOrUpdateReminder("shortenedRouteSegmentExpired",
               TimeSpan.Zero,
               TimeSpan.FromSeconds(this.state.State.ValidFor));
        }
        else
        {
            _timer = this.RegisterTimer(ReceiveTimer, null,
                TimeSpan.FromSeconds(this.state.State.ValidFor),
                TimeSpan.FromSeconds(this.state.State.ValidFor));
        }

        await this.state.WriteStateAsync();
    }

    public async Task<string> GetUrl()
    {
        this.state.State.Invocations += 1;

        if (string.IsNullOrWhiteSpace(this.state.State.FullUrl)) { throw new ShortenedRouteSegmentNotFound(); }
        if (this.state.State.IsOneShoot && this.state.State.Invocations > 1) { throw new InvocationExcedeedException(); }
        if (DateTime.UtcNow > this.state.State.Expiration) { throw new ExpiredShortenedRouteSegmentException(); }

        if(this.state.State.IsOneShoot && this.state.State.Invocations == 1 && _reminder is not null)
            await ShortenedRouteSegmentExpired();

        return this.state.State.FullUrl;
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

        var registrationManagerGrain = GrainFactory.GetGrain<IRegistrationObserversManager>(0);
        await registrationManagerGrain.RegisterExpiration(this.state.State.FullUrl);
    }

    private Task ReceiveTimer(object _)
    {
        if(_timer is null)
            return Task.CompletedTask;

        logger.LogInformation("ReceiveTimer invoked in grain with ID {0}", this.GetPrimaryKeyString());

        _timer?.Dispose();
        logger.LogInformation("Timer disposed from grain with ID {0}", this.GetPrimaryKeyString());

        var registrationManagerGrain = GrainFactory.GetGrain<IRegistrationObserversManager>(0);
        return registrationManagerGrain.RegisterExpiration(this.state.State.FullUrl);
    }
}
