using Microsoft.EntityFrameworkCore;
using Transportation_System.DataBase;
using Transportation_System.Hubs;
using Transportation_System.MQTT;
using Transportation_System.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<BusDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSignalR();
builder.Services.AddScoped<BusService>();
builder.Services.AddScoped<TelemetryProcessor>();
builder.Services.AddHostedService<MqttService>();

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
    dbContext.Database.EnsureCreated();
}

app.Run();