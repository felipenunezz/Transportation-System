using MQTTnet;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Transportation_System.Models.Domain;
using Transportation_System.Services;

namespace Transportation_System.MQTT;

public class MqttService : BackgroundService
{
    private readonly ILogger<MqttService> _logger;
    private readonly IMqttClient _mqttClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MqttClientFactory _mqttClientFactory;
    private BackgroundService _backgroundServiceImplementation;
    
    private const string BrokerHost = "localhost";
    private const int BrokerPort = 1883;
    private const string TelemetryTopic = "buses/+/telemetry";

    public MqttService(IServiceScopeFactory scopeFactory, ILogger<MqttService> logger)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _mqttClientFactory = new MqttClientFactory();
        _mqttClient = _mqttClientFactory.CreateMqttClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _mqttClient.ConnectedAsync += OnConnectedAsync;
        _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        
        await ConnectedAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ConnectedAsync(CancellationToken cancellationToken)
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(BrokerHost, BrokerPort)
            .WithClientId("TransportationSystem-" + Environment.MachineName)
            .WithCleanSession()
            .Build();

        try { await _mqttClient.ConnectAsync(options, cancellationToken); }
    catch (Exception e) { _logger.LogError(e, "Could not connect to Mqtt broker at {Host}:{Port}.",  BrokerHost, BrokerPort); }
    }    
    private async Task OnConnectedAsync(MqttClientConnectedEventArgs arg)
    {
        _logger.LogInformation("Connected to MQTT Broker, subscribing to {Topic}", TelemetryTopic);
        
        var subscribeOptions = _mqttClientFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(TelemetryTopic)
            .Build();
        
        await _mqttClient.SubscribeAsync(subscribeOptions, CancellationToken.None);
    }
    
    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        _logger.LogInformation("Disconnected from MQTT Broker {Reason}, reconnecting in 5s", arg.Reason);
        await Task.Delay(TimeSpan.FromSeconds(5));
        await ConnectedAsync(CancellationToken.None);
    }
    
    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        try
        {
            var payload = arg.ApplicationMessage.ConvertPayloadToString();
            var telemetry = JsonSerializer.Deserialize<Bus>(payload);

            if (telemetry is null)
            {
                _logger.LogWarning("Could not parse payload on {Topic}:{Payload}", arg.ApplicationMessage.Topic,
                    payload);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<TelemetryProcessor>();
            await processor.ProcessAsync(telemetry);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error handling MQTT Message on {Topic}", arg.ApplicationMessage.Topic);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptions(){
                Reason = MqttClientDisconnectOptionsReason.NormalDisconnection
            });
        }
        await base.StopAsync(cancellationToken);
    }
}