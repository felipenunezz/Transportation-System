using Simulator.Simulation;
using Simulator.Mqtt;
using Transportation_System.Data;
using Transportation_System.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Simulator.Devices;

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

builder.Services.AddScoped<FleetTerminal>();
builder.Services.AddScoped<SpeedometerDevice>();
builder.Services.AddScoped<GpsDevice>();
builder.Services.AddScoped<PassangerCounterDevice>();
builder.Services.AddScoped<BusSimulator>();
builder.Services.AddScoped<StopSimulator>();

builder.Services.AddHostedService<BusSimulationWorker>();

var host = builder.Build();
await host.RunAsync();

