using Orleans.Concurrency;
using Orleans.UrlShortner.Abstractions;
using Orleans.Utilities;

namespace Orleans.UrlShortner.Observers;

public interface IRegistrationObserversManager : IGrainWithIntegerKey
{
    [OneWay]
    Task Activate();
    Task Subscribe(Abstractions.IDomainStatisticsGrain observer);
    Task Subscribe(IUrlShortnerStatisticsGrain observer);
    Task Unsubscribe(IDomainStatisticsGrain observer);
    Task Unsubscribe(IUrlShortnerStatisticsGrain observer);

    public Task RegisterNew(string url);
    Task RegisterExpiration(string url);
}


public class RegistrationObserversManager : Grain, IRegistrationObserversManager
{
    private readonly ObserverManager<IUrlShortnerStatisticsGrain> subsStatisticsManager;
    private readonly ObserverManager<IDomainStatisticsGrain> subsDomainsManager;
    private readonly ILogger<IRegistrationObserversManager> logger;

    public RegistrationObserversManager(ILogger<IRegistrationObserversManager> logger)
    {
        this.subsStatisticsManager = new ObserverManager<IUrlShortnerStatisticsGrain>(TimeSpan.FromDays(7), logger);
        this.subsDomainsManager = new ObserverManager<IDomainStatisticsGrain>(TimeSpan.FromDays(7), logger);
        this.logger = logger;
    }

    public Task Activate()
        => Task.CompletedTask;

    public Task RegisterNew(string url)
        => Task.WhenAll(
            this.subsStatisticsManager.Notify(s => s.RegisterNew()),
            this.subsDomainsManager.Notify(s => s.RegisterNew(), s => {
                var uri = new Uri(url);
                return s.GetPrimaryKeyString() == uri.Host;
            }));

    public Task RegisterExpiration(string url)
    => Task.WhenAll(
            this.subsStatisticsManager.Notify(s => s.RegisterExpiration()),
            this.subsDomainsManager.Notify(s => s.RegisterExpiration(), s =>
            {
                var uri = new Uri(url);
                return s.GetPrimaryKeyString() == uri.Host;
            }));


    public Task Subscribe(IDomainStatisticsGrain observer)
    {
        this.subsDomainsManager.Subscribe(observer, observer);
        return Task.CompletedTask;
    }

    public Task Subscribe(IUrlShortnerStatisticsGrain observer)
    {
        this.subsStatisticsManager.Subscribe(observer, observer);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IDomainStatisticsGrain observer)
    {
        this.subsDomainsManager.Unsubscribe(observer);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IUrlShortnerStatisticsGrain observer)
    {
        this.subsStatisticsManager.Unsubscribe(observer);
        return Task.CompletedTask;
    }
}
