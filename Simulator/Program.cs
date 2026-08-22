using Simulator.Simulation;
using Simulator.Mqtt;
using Transportation_System.Data;
using Transportation_System.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not set.");

builder.Services.AddDbContext<BusDbContext>(options => options.UseNpgsql(connectionString));

var valhallaBaseUrl = builder.Configuration["ValhallaSettings:BaseUrl"] ?? "http://localhost:8002";
builder.Services.AddHttpClient<RoutingService>(client =>
{
    client.BaseAddress = new Uri(valhallaBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.Configure<SimulationSettings>(builder.Configuration.GetSection("SimulationSettings"));
builder.Services.AddSingleton<MqttPublisher>();
builder.Services.AddSingleton<BusSimulator>();
builder.Services.AddHostedService<BusSimulationWorker>();

var host = builder.Build();
await host.RunAsync();

