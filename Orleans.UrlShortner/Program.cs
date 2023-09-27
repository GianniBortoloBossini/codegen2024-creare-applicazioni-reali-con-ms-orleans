using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Orleans.Configuration;
using Orleans.UrlShortner.Abstractions;
using Orleans.UrlShortner.Filters;
using Orleans.UrlShortner.Observers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// *** REGISTRAZIONE E CONFIGURAZIONE DI ORLEANS ***
builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.ConfigureLogging(loggingConfig =>
    {
        Log.Logger = new LoggerConfiguration()
                    .WriteTo.File("logs/log-.txt",
                                  outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                                  rollingInterval: RollingInterval.Day)
                    .CreateLogger();

        loggingConfig.AddConsole().AddSerilog(Log.Logger);
    });

    siloBuilder.AddActivityPropagation();

    if (builder.Environment.IsDevelopment())
    {
        // CLUSTER LOCALE
        siloBuilder.UseLocalhostClustering();

        // ADD SILO-WIDE GRAIN CALL FILTERS
        siloBuilder.AddIncomingGrainCallFilter<LoggingCallFilter>();

        // REGISTRAZIONE REMINDERS
        siloBuilder.UseInMemoryReminderService();

        // REGISTRAZIONE STORAGE IN MEMORIA
        siloBuilder.AddMemoryGrainStorage("domainstatisticsstorage");
        siloBuilder.AddMemoryGrainStorage("applicationstatisticsstorage");
        siloBuilder.AddMemoryGrainStorage("urlshortnerstorage");
    }
    else
    {
        // CREAZIONE DEL CLUSTER PER AMBIENTI DI STAGING / PRODUZIONE
        siloBuilder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "CodicePlasticoCluster";
            options.ServiceId = "OrleansUrlShortener";
        })
        .UseAdoNetClustering(options =>
        {
            options.ConnectionString = builder.Configuration.GetConnectionString("SqlOrleans");
            options.Invariant = "System.Data.SqlClient";
        })
        .ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000);

        // REGISTRAZIONE REMINDERS PER AMBIENTI DI STAGING / PRODUZIONE
        siloBuilder.UseAdoNetReminderService(reminderOptions => {
            reminderOptions.ConnectionString = builder.Configuration.GetConnectionString("SqlOrleans");
            reminderOptions.Invariant = "System.Data.SqlClient";
        });

        // REGISTRAZIONE STORAGE PER AMBIENTI DI STAGING / PRODUZIONE
        siloBuilder.AddAdoNetGrainStorage("domainstatisticsstorage", storageOptions =>
        {
            storageOptions.ConnectionString = builder.Configuration.GetConnectionString("SqlOrleans");
            storageOptions.Invariant = "System.Data.SqlClient";
        }).AddAdoNetGrainStorage("applicationstatisticsstorage", storageOptions =>
        {
            storageOptions.ConnectionString = builder.Configuration.GetConnectionString("SqlOrleans");
            storageOptions.Invariant = "System.Data.SqlClient";
        }).AddAdoNetGrainStorage("urlshortnerstorage", storageOptions =>
        {
            storageOptions.ConnectionString = builder.Configuration.GetConnectionString("SqlOrleans");
            storageOptions.Invariant = "System.Data.SqlClient";
        });
    }

    // Attivo il grano delle statistiche alla partenza dell'applicazione
    siloBuilder.AddStartupTask(
          async (IServiceProvider services, CancellationToken cancellation) =>
          {
              var grainFactory = services.GetRequiredService<IGrainFactory>();

              var grainStats = grainFactory.GetGrain<IUrlShortnerStatisticsGrain>("url_shortner_statistics");
              var grainObservers = grainFactory.GetGrain<IRegistrationObserversManager>(0);

              await Task.WhenAll(
                  grainObservers.Activate(),
                  grainStats.Activate());
          }, ServiceLifecycleStage.Last);

    siloBuilder.UseDashboard(dashboardConfig =>
    {
        dashboardConfig.Port = 7070;
        dashboardConfig.Username = "admin";
        dashboardConfig.Password = "admin";
    });
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        // Set a service name
        tracing.SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName: "UrlShortner", serviceVersion: "1.0"));

        tracing.AddSource("Microsoft.Orleans.Runtime");
        tracing.AddSource("Microsoft.Orleans.Application");

        tracing.AddZipkinExporter(zipkin =>
        {
            zipkin.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
        });
    });

var app = builder.Build();

app.Run();
