using Orleans.Runtime;
using Orleans.UrlShortner.Infrastructure.Exceptions;
using Orleans.UrlShortner.Observers;
using System.Threading.Tasks;

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
    [Id(5)]
    public string Domain { get; set; }
    [Id(6)]
    public bool IsReminderActive { get; set; }
    [Id(7)]
    public string ShortenedRouteSegmentExpiredReminderName { get; set; }
}

public class UrlShortenerGrain : Grain, IUrlShortenerGrain, IRemindable
{
    private IDisposable _timer = null;
    private IRegistrationObserversManager registrationManagerGrainReference;
    private IGrainReminder shortenedRouteSegmentExpiredReminder;
    private readonly IPersistentState<UrlShortenerState> state;
    private readonly ILogger<UrlShortenerGrain> logger;

    public UrlShortenerGrain(
        [PersistentState(stateName: "url-shortner", storageName: "urlshortnerstorage")] IPersistentState<UrlShortenerState> state,
        ILogger<UrlShortenerGrain> logger)
    {
        this.state = state;
        this.logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (this.state.State.Domain is not null)
        {
            var domainGrain = GrainFactory.GetGrain<IDomainStatisticsGrain>(this.state.State.Domain);
            await domainGrain.Activate();
        }

        this.registrationManagerGrainReference = GrainFactory.GetGrain<IRegistrationObserversManager>(0);

        if (this.state.State.ShortenedRouteSegmentExpiredReminderName is not null)
        {
            this.shortenedRouteSegmentExpiredReminder = await this.GetReminder(this.state.State.ShortenedRouteSegmentExpiredReminderName);
        }

        await base.OnActivateAsync(cancellationToken);
    }

    public async Task CreateShortUrl(string fullUrl, bool? isOneShoot, int? validFor)
    {
        var uri = new Uri(fullUrl);

        this.state.State.FullUrl = fullUrl;
        this.state.State.Domain = uri.Host;
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
            this.state.State.ShortenedRouteSegmentExpiredReminderName = $"shortenedRouteSegmentExpired{this.GetPrimaryKeyString()}";
            var reminder = await this.RegisterOrUpdateReminder(this.state.State.ShortenedRouteSegmentExpiredReminderName,
               TimeSpan.Zero,
               TimeSpan.FromSeconds(this.state.State.ValidFor));
            this.state.State.IsReminderActive = reminder != null;
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

        if (string.IsNullOrWhiteSpace(this.state.State.FullUrl)) { throw new InvocationExcedeedException(); }
        if (this.state.State.IsOneShoot && this.state.State.Invocations > 1) { throw new InvocationExcedeedException(); }
        if (DateTime.UtcNow > this.state.State.Expiration) { throw new ExpiredShortenedRouteSegmentException(); }

        if (this.state.State.IsOneShoot && this.state.State.Invocations == 1 && this.state.State.IsReminderActive)
            await ShortenedRouteSegmentExpired();

        return this.state.State.FullUrl;
    }

    public Task ReceiveReminder(string reminderName, TickStatus status)
    {
        return reminderName switch
        {
            var r when r == this.state.State.ShortenedRouteSegmentExpiredReminderName => ShortenedRouteSegmentExpired(),
            _ => Task.CompletedTask
        };
    }

    private async Task ShortenedRouteSegmentExpired()
    {
        logger.LogInformation("ShortenedRouteSegmentExpired invoked in grain with ID {0}", this.GetPrimaryKeyString());

        if (this.state.State.IsReminderActive)
        {
            this.shortenedRouteSegmentExpiredReminder = await this.GetReminder(this.state.State.ShortenedRouteSegmentExpiredReminderName);
            await this.UnregisterReminder(this.shortenedRouteSegmentExpiredReminder);
            this.state.State.IsReminderActive = false;
            logger.LogInformation("ShortenedRouteSegmentExpired unregister reminder in grain with ID {0}", this.GetPrimaryKeyString());
        }

        await registrationManagerGrainReference.RegisterExpiration(this.state.State.FullUrl);

        logger.LogInformation("ShortenedRouteSegmentExpired expiration registered in grain with ID {0}", this.GetPrimaryKeyString());
    }

    private Task ReceiveTimer(object _)
    {
        if (_timer is null)
            return Task.CompletedTask;

        logger.LogInformation("ReceiveTimer invoked in grain with ID {0}", this.GetPrimaryKeyString());

        _timer?.Dispose();
        logger.LogInformation("Timer disposed from grain with ID {0}", this.GetPrimaryKeyString());

        return registrationManagerGrainReference.RegisterExpiration(this.state.State.FullUrl);
    }
}
