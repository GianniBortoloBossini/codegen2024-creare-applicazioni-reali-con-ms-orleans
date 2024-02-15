using Orleans.UrlShortner.Grains;
using Orleans.UrlShortner.StatelessWorkers;
using Orleans.UrlShortner.Tests.Fixtures;

namespace Orleans.UrlShortner.Tests.Grains;
internal class UrlShortnerStatisticsGrainTests
{
    private ClusterFixture fixture;

    [SetUp]
    public void SetUp()
    {
        this.fixture = new ClusterFixture();
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(100)]
    public async Task GetTotal_Should_Count_New_Registrations(int registrationTimes)
    {
        // ARRANGE
        var statisticsGrain = fixture.Cluster.GrainFactory.GetGrain<IUrlShortnerStatisticsGrain>("url_shortner_statistics");
        
        List<Task> tasks = new();
        for (int i = 0; i < registrationTimes; i++)
            tasks.Add(statisticsGrain.RegisterNew());

        await Task.WhenAll(tasks);

        // ACT

        /* PERCHE' QUESTO DELAY? 
         * Provare a decommentare gli attributi OneWay e ReadOnly sull'interfaccia del grano e scoprirai che i test falliscono.
         * E' legato alle migliorie alle performance introdotte da Orleans nella gestione del multi-threading in presenza di quei due attributi:
         * await non attende che il Task sia effettivamente terminato! 
         * Per questo è necessario attendere 
         */
        await Task.Delay(100); 

        var result = await statisticsGrain.GetTotal();

        // ASSERT
        Assert.That(result, Is.EqualTo(registrationTimes));
    }

    [TestCase(3, 3100)]
    [TestCase(60, 61000)]
    public async Task GetNumberOfActiveShortenedRouteSegment_Should_Count_Active_Shortened_Route_Segment(int validFor, int checkAfter)
    {
        var url = "https://capitalecultura2023.it/";
        var shortenerRouteSegmentWorker = fixture.Cluster.GrainFactory.GetGrain<IShortenedRouteSegmentStatelessWorker>(0);
        var shortenerRouteSegment = await shortenerRouteSegmentWorker.Create(url);

        var urlshortenerGrain = fixture.Cluster.GrainFactory.GetGrain<IUrlShortenerGrain>(shortenerRouteSegment);
        await urlshortenerGrain.CreateShortUrl(url, false, 3);

        var statisticsGrain = fixture.Cluster.GrainFactory.GetGrain<IUrlShortnerStatisticsGrain>("url_shortner_statistics");

        await Task.Delay(100);

        var totalActivation = await statisticsGrain.GetTotal();
        var numberOfActiveShortenerRouteSegment = await statisticsGrain.GetNumberOfActiveShortenedRouteSegment();

        Assert.That(totalActivation, Is.EqualTo(1));
        Assert.That(numberOfActiveShortenerRouteSegment, Is.EqualTo(1));

        await Task.Delay(checkAfter);

        totalActivation = await statisticsGrain.GetTotal();
        numberOfActiveShortenerRouteSegment = await statisticsGrain.GetNumberOfActiveShortenedRouteSegment();

        Assert.That(totalActivation, Is.EqualTo(1));
        Assert.That(numberOfActiveShortenerRouteSegment, Is.EqualTo(0));
    }
}
