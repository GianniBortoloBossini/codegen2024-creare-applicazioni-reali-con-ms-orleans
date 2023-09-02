using Orleans.Concurrency;

namespace Orleans.UrlShortner.Grains;

public interface IUrlShortnerStatisticsGrain : IGrainWithStringKey
{
    //[OneWay]
    Task RegisterNew();
    //[ReadOnly]
    Task<int> GetTotal();
}

public class UrlShortnerStatisticsGrain : Grain, IUrlShortnerStatisticsGrain
{
    public int TotalActivations { get; set; }

    public Task<int> GetTotal()
    {
        return Task.FromResult(TotalActivations);
    }

    public Task RegisterNew()
    {
        this.TotalActivations++;
        return Task.CompletedTask;
    }
}
