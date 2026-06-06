using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using MQTTnet;
using MQTTnet.Client;
using Transportation_System.DataBase;
using Transportation_System.Hubs;

namespace Transportation_System.Services;

public class MqttService : IHostedService
    {
        private readonly IMqttClient _mqttClient;
        private readonly IServiceProvider _serviceProvider;
        
        public MqttService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _mqttClient = new MqttFactory().CreateMqttClient();
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("mosquitto", 1883)
                .Build();
                
            // Handle incoming messages
            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                
                // Only handle telemetry - this is all you need
                if (topic.Contains("telemetry"))
                {
                    var busId = int.Parse(topic.Split('/')[2]); // bus/system/{id}/telemetry
                    var data = JsonSerializer.Deserialize<JsonElement>(payload);
                    
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<BusDbContext>();
                    var hub = scope.ServiceProvider.GetRequiredService<IHubContext<BusTrackingHub>>();
                    
                    // Update database
                    var bus = await db.Buses.FindAsync(busId);
                    if (bus != null)
                    {
                        bus.CurrentLatitude = data.GetProperty("Latitude").GetDouble();
                        bus.CurrentLongitude = data.GetProperty("Longitude").GetDouble();
                        bus.Speed = data.GetProperty("Speed").GetDouble();
                        bus.PassengerCount = data.GetProperty("PassengerCount").GetInt32();
                        bus.LastUpdate = DateTime.UtcNow;
                        await db.SaveChangesAsync();
                    }
                    
                    // Send to browser
                    await hub.Clients.All.SendAsync("BusLocationUpdated", new
                    {
                        BusId = busId,
                        Latitude = data.GetProperty("Latitude").GetDouble(),
                        Longitude = data.GetProperty("Longitude").GetDouble(),
                        Speed = data.GetProperty("Speed").GetDouble()
                    });
                }
            };
            
            try
            {
                await _mqttClient.ConnectAsync(options, cancellationToken);
                await _mqttClient.SubscribeAsync("bus/system/+/telemetry");
            }
            catch
            {
                // MQTT broker not available - that's OK for now
            }
        }
        
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
