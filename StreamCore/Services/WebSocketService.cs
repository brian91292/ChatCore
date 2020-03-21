using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using StreamCore.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace StreamCore.Services
{
    public class WebSocketService : IWebSocketService
    {
        public bool IsConnected => !(_client is null) && DateTime.UtcNow.Subtract(_client.LastActiveTime.ToUniversalTime()).TotalMinutes > 2;
        public event Action OnOpen;
        public event Action OnClose;
        public event Action OnError;
        public event Action<Assembly, string> OnMessageReceived;

        public WebSocketService(ILogger<WebSocketService> logger)
        {
            _logger = logger;
        }

        private ILogger _logger;
        private object _lock = new object();
        private CancellationTokenSource _cancellationToken;
        private WebSocket _client;

        public void Connect(string uri, int port)
        {
            lock (_lock)
            {
                _logger.LogDebug($"Connecting to {uri}");
                if (_client is null)
                {
                    _cancellationToken = new CancellationTokenSource();
                    Task.Run(() => 
                    {
                        try
                        {
                            _client = new WebSocket(uri);
                            _client.Opened += HandleOnOpen;
                            _client.Closed += (s, e) => OnClose?.Invoke();
                            _client.Error += (s, e) => OnError?.Invoke();
                            _client.MessageReceived += HandleMessageReceived;
                            _client.EnableAutoSendPing = true;
                            _client.Open();
                        }
                        catch (TaskCanceledException)
                        {
                            _logger.LogInformation("WebSocket client task was cancelled");
                        }
                    }, _cancellationToken.Token);
                }
            }
        }

        private void HandleOnOpen(object sender, EventArgs e)
        {
            _logger.LogInformation("Connection opened successfully!");
            OnOpen?.Invoke();
        }

        private void HandleMessageReceived(object sender, MessageReceivedEventArgs message)
        {
            _logger.LogInformation($"Message received from websocket: {message.Message}");
            OnMessageReceived?.Invoke(null, message.Message);
        }

        public void Disconnect()
        {
            lock (_lock)
            {
                _logger.LogInformation("Disconnecting");
                if (IsConnected)
                {
                    _cancellationToken.Cancel();
                }
            }
        }

        public void SendMessage(string message)
        {
            if (IsConnected)
            {
                _logger.LogInformation($"Sending {message}");
                _client.Send(message);
            }
            else
            {
                _logger.LogInformation("WebSocket is not connected!");
            }
        }
    }
}
