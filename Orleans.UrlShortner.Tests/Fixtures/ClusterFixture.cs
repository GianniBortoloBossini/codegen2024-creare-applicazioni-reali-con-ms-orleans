using Orleans.TestingHost;

namespace Orleans.UrlShortner.Tests.Fixtures;
public class ClusterFixture : IDisposable
{
    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();

        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose() => Cluster.StopAllSilos();

    public TestCluster Cluster { get; }
}

public class TestSiloConfigurations : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.UseInMemoryReminderService();
        siloBuilder.AddMemoryGrainStorage("domainstatisticsstorage");
        siloBuilder.AddMemoryGrainStorage("applicationstatisticsstorage");
        siloBuilder.AddMemoryGrainStorage("urlshortnerstorage");
    }
}
