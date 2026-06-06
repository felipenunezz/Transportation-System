using Microsoft.EntityFrameworkCore;
using Transportation_System.DataBase;
using Transportation_System.Models;
using Transportation_System.Services;
using Transportation_System.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "Data Source=Database.db";

builder.Services.AddDbContextFactory<BusDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<BusDbContext>(provider => provider.GetRequiredService<IDbContextFactory<BusDbContext>>().CreateDbContext());
builder.Services.AddSignalR();
builder.Services.AddControllersWithViews();
builder.Services.AddHostedService<MqttService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Dashboard}/{id?}");

// SignalR hub endpoint
app.MapHub<BusTrackingHub>("/busHub");

// Create database and apply migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BusDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();