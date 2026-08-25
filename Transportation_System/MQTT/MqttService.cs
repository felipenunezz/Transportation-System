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

    private readonly Dictionary<string, Func<string, object>> _busTelemetryHandlers;

    public MqttService(IServiceScopeFactory scopeFactory, ILogger<MqttService> logger, IConfiguration config)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;

        _brokerHost = config["MqttSettings:BrokerAddress"] ?? "localhost";
        _brokerPort = int.TryParse(config["MqttSettings:BrokerPort"], out var port) ? port : 1883;

        _mqttClientFactory = new MqttClientFactory();
        _mqttClient = _mqttClientFactory.CreateMqttClient();

        // Initialize handlers for different bus telemetry types
        _busTelemetryHandlers = new Dictionary<string, Func<string, object>>
        {
            ["gps"] = payload => JsonSerializer.Deserialize<Gps>(payload),
            ["speed"] = payload => JsonSerializer.Deserialize<Speedometer>(payload),
            ["passengers"] = payload => JsonSerializer.Deserialize<BusPassenger>(payload),
            ["terminal"] = payload => JsonSerializer.Deserialize<Terminal>(payload)
        };
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

            if (!int.TryParse(parts[1], out var entityId))
            {
                _logger.LogWarning("Could not parse entity id from topic {Topic}", topic);
                return;
            }

            var payload = arg.ApplicationMessage.ConvertPayloadToString();
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<TelemetryProcessor>();

            switch (entityType)
            {
                case "buses":
                    await HandleBusTelemetry(processor, entityId, payload, topic);
                    break;
                
                case "stops":
                    await HandleStopTelemetry(processor, entityId, payload, topic);
                    break;

                default:
                    _logger.LogWarning("Unhandled entity type {EntityType} for topic {Topic}", entityType, topic);
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

    private async Task HandleBusTelemetry(TelemetryProcessor processor, int busId, string payload, string topic)
    {
        var parts = topic.Split('/');
        if (parts.Length < 3)
        {
            _logger.LogWarning("Invalid topic format for bus telemetry: {Topic}", topic);
            return;
        }

        var messageType = parts[2];

        if (!_busTelemetryHandlers.TryGetValue(messageType, out var handler))
        {
            _logger.LogWarning("Unhandled bus telemetry type: {MessageType} for topic {Topic}", messageType, topic);
            return;
        }

        try
        {
            var dto = handler(payload);
            if (dto is null)
            {
                _logger.LogWarning("Could not parse payload on {Topic}: {Payload}", topic, payload);
                return;
            }

            await processor.ProcessBusAsync(busId, messageType, dto);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for topic {Topic} with payload: {Payload}", topic, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bus telemetry for topic {Topic}", topic);
        }
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