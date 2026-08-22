using Microsoft.Extensions.Configuration;
using MQTTnet;

namespace Simulator.Mqtt;

public class MqttPublisher(IConfiguration configuration) : IAsyncDisposable
{
    private readonly IMqttClient _client = new MqttClientFactory().CreateMqttClient();
    private bool _connected;

    public async Task ConnectAsync(CancellationToken ct)
    {
        var host = configuration["MqttSettings:BrokerAddress"] ?? "localhost";
        var port = int.TryParse(configuration["MqttSettings:BrokerPort"], out var p) ? p : 1883;

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId("device-simulator-cs-" + Guid.NewGuid().ToString("N")[..8])
            .WithCleanSession()
            .Build();

        await _client.ConnectAsync(options, ct);
        _connected = true;
    }

    public async Task PublishAsync(string topic, string payload, CancellationToken ct)
    {
        if (!_connected) return;

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build();

        await _client.PublishAsync(message, ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connected) await _client.DisconnectAsync();
    }
}