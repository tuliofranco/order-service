using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Order.Api.Features.Orders.DTOs;

namespace Order.Worker.Notification;

public sealed class WorkerOrderNotification : IWorkerOrderNotification, IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly ILogger<WorkerOrderNotification> _logger;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private readonly string _hubUrl; 
    private bool _started;

    public WorkerOrderNotification(
        IOptions<WorkerNotificationOptions> options,
        ILogger<WorkerOrderNotification> logger)
    {
        _logger = logger;

        if (string.IsNullOrWhiteSpace(options.Value.HubUrl))
            throw new InvalidOperationException("WorkerNotificationOptions.HubUrl não foi configurado.");
        
        _hubUrl = options.Value.HubUrl;    

        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect()
            .Build();
    }

    private async Task EnsureStartedAsync(CancellationToken ct)
    {
        if (_started)
            return;

        await _startLock.WaitAsync(ct);
        try
        {
            if (_started)
                return;

            _logger.LogInformation("Iniciando conexão SignalR do Worker para {Url}", _hubUrl);

            await _connection.StartAsync(ct);
            _started = true;

            _logger.LogInformation("Conexão SignalR do Worker iniciada.");
        }
        finally
        {
            _startLock.Release();
        }
    }

    public async Task NotifyOrderStatusChangedAsync(Guid orderId, CancellationToken ct = default)
    {
        await EnsureStartedAsync(ct);

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["orderId"] = orderId
        }))
        {
            _logger.LogInformation("Enviando notificação de mudança de status via SignalR a partir do Worker");

            await _connection.InvokeAsync("BroadcastOrderStatusChanged", orderId, ct);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_started)
                await _connection.StopAsync();
        }
        finally
        {
            await _connection.DisposeAsync();
            _startLock.Dispose();
        }
    }
}
