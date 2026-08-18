using Microsoft.EntityFrameworkCore;
using Transportation_System.Data;
using Transportation_System.Hubs;
using Transportation_System.MQTT;
using Transportation_System.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddDbContext<BusDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount:5,
                maxRetryDelay:TimeSpan.FromSeconds(10),
                errorCodesToAdd: null)));
builder.Services.AddSignalR();
builder.Services.AddScoped<BusService>();
builder.Services.AddScoped<StopService>();
builder.Services.AddScoped<TelemetryProcessor>();
builder.Services.AddHostedService<MqttService>();

builder.Services.AddHttpClient<RoutingService>(client =>
{
    var baseUrl = builder.Configuration["ValhallaSettings:BaseUrl"] ?? "http://localhost:8002";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Dashboard}/{id?}");

app.MapHub<BusTrackingHub>("/busHub");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BusDbContext>();
    var dataSeed = new DataSeed();
    await dbContext.Database.MigrateAsync();
    await dataSeed.SeedAsync(dbContext);
}

app.Run();