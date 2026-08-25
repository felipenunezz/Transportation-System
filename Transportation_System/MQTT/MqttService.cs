using System.Reflection.Metadata;
using MQTTnet;
using System.Text.Json;
using Transportation_System.Models.Dto;
using Transportation_System.Services;

namespace Transportation_System.MQTT;

public class MqttService : BackgroundService
{
    private readonly ILogger<MqttService> _logger;
    private readonly IMqttClient _mqttClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MqttClientFactory _mqttClientFactory;

    private readonly string _brokerHost;
    private readonly int _brokerPort;
    private const string BusTopic = "buses/+/+";
    private const string StopTopic = "stops/+/+";

    public MqttService(IServiceScopeFactory scopeFactory, ILogger<MqttService> logger, IConfiguration config)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;

        _brokerHost = config["MqttSettings:BrokerAddress"] ?? "localhost";
        _brokerPort = int.TryParse(config["MqttSettings:BrokerPort"], out var port) ? port : 1883;

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
            .WithTcpServer(_brokerHost, _brokerPort)
            .WithClientId("TransportationSystem-" + Environment.MachineName)
            .WithCleanSession()
            .Build();

        try
        {
            await _mqttClient.ConnectAsync(options, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not connect to Mqtt broker at {Host}:{Port}.", _brokerHost, _brokerPort);
        }
    }

    private async Task OnConnectedAsync(MqttClientConnectedEventArgs arg)
    {
        _logger.LogInformation("Connected to MQTT Broker, subscribing to {Topic}", BusTopic);

        var subscribeOptions = _mqttClientFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(BusTopic)
            .WithTopicFilter(StopTopic)
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
        var topic = arg.ApplicationMessage.Topic;

        try
        {
            var parts = topic.Split('/');
            if (parts.Length < 3)
            {
                _logger.LogWarning("Unrecognized topic shape {Topic}", topic);
                return;
            }

            var entityType = parts[0];
            var messageType = parts[2];

            if (!int.TryParse(parts[1], out var entityId))
            {
                _logger.LogWarning("Could not parse entity id from topic {Topic}", topic);
                return;
            }

            var payload = arg.ApplicationMessage.ConvertPayloadToString();
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<TelemetryProcessor>();

            switch (entityType, messageType)
            {
                case ("buses", "Start"):
                    await  HandleBusStart(processor, entityId, payload, topic);
                    break;
                
                case ("buses", "Stop"):
                    await HandleBusFinish(processor, entityId, payload, topic);
                    break;
                
                case ("buses", "Movement"):
                    await HandleBusMovment(processor, entityId, payload, topic);
                    break;
                
                case ("buses", "passengers"):
                    await HandleBusPassangers(processor, entityId, payload, topic);
                    break;
                
                case ("stops", "passengers"):
                    await HandleStopTelemetry(processor, entityId, payload, topic);
                    break;

                default:
                    _logger.LogWarning("Unhandled topic {Topic} (entityType={EntityType}, messageType={MessageType})",
                        topic, entityType, messageType);
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error handling MQTT Message on {Topic}", topic);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptions()
            {
                Reason = MqttClientDisconnectOptionsReason.NormalDisconnection
            }, cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task HandleBusStart(TelemetryProcessor processor, int busId, string payload, string topic)
    {
        var dto = JsonSerializer.Deserialize<BusStartDto>(payload);
        if (dto is null)
        {
            _logger.LogWarning("Could not parse payload on {Topic}: {Payload}", topic, payload);
            return;
        }
        await processor.ProcessBusAsync(busId, dto);
    }

    private async Task HandleBusFinish(TelemetryProcessor processor, int busId, string payload, string topic)
    {
    }

    private async Task HandleBusMovment(TelemetryProcessor processor, int busId, string payload, string topic)
    {
        var dto = JsonSerializer.Deserialize<BusMovementDto>(payload);
        if (dto is null)
        {
            _logger.LogWarning("Could not parse payload on {Topic}: {Payload}", topic, payload);
            return;
        }

        await processor.ProcessBusAsync(busId, dto);
    }

    private async Task HandleBusPassangers(TelemetryProcessor processor, int busId, string payload, string topic)
    {
        var dto = JsonSerializer.Deserialize<BusPassengerDto>(payload);
        if (dto is null)
        {
            _logger.LogWarning("Could not parse payload on {Topic}: {Payload}", topic, payload);
            return;
        }
        await processor.ProcessBusAsync(busId, dto);
    }

    private async Task HandleStopTelemetry(TelemetryProcessor processor, int stopId, string payload, string topic)
    {
        var dto = JsonSerializer.Deserialize<StopDto>(payload);
        if (dto is null)
        {
            _logger.LogWarning("Could not parse payload on {Topic}: {Payload}", topic, payload);
            return;
        }

        await processor.ProcessStopAsync(stopId, dto);
    }
}