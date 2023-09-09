using Orleans.UrlShortner.Grains;
using Orleans.Utilities;

namespace Orleans.UrlShortner.Observers;

public interface IRegistrationObserversManager : IGrainWithIntegerKey
{
    Task Subscribe(IDomainGrain observer);
    Task Subscribe(IUrlShortnerStatisticsGrain observer);
    Task Unsubscribe(IDomainGrain observer);
    Task Unsubscribe(IUrlShortnerStatisticsGrain observer);

    public Task RegisterNew(string url);
    Task RegisterExpiration(string url);
}


public class RegistrationObserversManager : Grain, IRegistrationObserversManager
{
    private readonly ObserverManager<IUrlShortnerStatisticsGrain> subsStatisticsManager;
    private readonly ObserverManager<IDomainGrain> subsDomainsManager;

    public RegistrationObserversManager(ILogger<IRegistrationObserversManager> logger)
    {
        this.subsStatisticsManager = new ObserverManager<IUrlShortnerStatisticsGrain>(TimeSpan.FromMinutes(5), logger);
        this.subsDomainsManager = new ObserverManager<IDomainGrain>(TimeSpan.FromMinutes(5), logger);
    }

    public Task RegisterNew(string url)
    {
        this.subsStatisticsManager.Notify(s => s.RegisterNew());
        this.subsDomainsManager.Notify(s => s.RegisterNew(), s => {
            var uri = new Uri(url);
            return s.GetPrimaryKeyString() == uri.Host;
            });

        return Task.CompletedTask;
    }

    public Task RegisterExpiration(string url)
    {
        this.subsStatisticsManager.Notify(s => s.RegisterExpiration());
        this.subsDomainsManager.Notify(s => s.RegisterExpiration(), s => {
            var uri = new Uri(url);
            return s.GetPrimaryKeyString() == uri.Host;
        });

        return Task.CompletedTask;
    }

    public Task Subscribe(IDomainGrain observer)
    {
        this.subsDomainsManager.Subscribe(observer, observer);
        return Task.CompletedTask;
    }

    public Task Subscribe(IUrlShortnerStatisticsGrain observer)
    {
        this.subsStatisticsManager.Subscribe(observer, observer);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IDomainGrain observer)
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
