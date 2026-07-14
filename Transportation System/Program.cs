using Microsoft.EntityFrameworkCore;
using Transportation_System.DataBase;
using Transportation_System.Hubs;
using Transportation_System.Services;

var builder = WebApplication.CreateBuilder(args);

// Add MVC services
builder.Services.AddControllersWithViews();

// Add SQLite database
builder.Services.AddDbContext<BusDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add SignalR for real-time web updates
builder.Services.AddSignalR();

// Add MQTT service as background service
//builder.Services.AddHostedService<MqttService>();
//not working right now, working on a better implementation

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