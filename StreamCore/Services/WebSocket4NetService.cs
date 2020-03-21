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
    public class WebSocket4NetService : IWebSocketService
    {
        public bool IsConnected => !(_client is null) && DateTime.UtcNow.Subtract(_client.LastActiveTime.ToUniversalTime()).TotalMinutes > 2;
        public bool AutoReconnect { get; set; } = true;
        public event Action OnOpen;
        public event Action OnClose;
        public event Action OnError;
        public event Action<Assembly, string> OnMessageReceived;

        public WebSocket4NetService(ILogger<WebSocket4NetService> logger)
        {
            _logger = logger;
        }

        private ILogger _logger;
        private object _lock = new object();
        private CancellationTokenSource _cancellationToken;
        private WebSocket _client;
        private string _uri = "";

        public void Connect(string uri)
        {
            lock (_lock)
            {
                if (_client is null)
                {
                    _logger.LogDebug($"Connecting to {uri}");
                    _uri = uri;
                    _cancellationToken = new CancellationTokenSource();
                    Task.Run(async () => 
                    {
                        try
                        {
                            _client = new WebSocket(uri);
                            _client.Opened += _client_Opened; ;
                            _client.Closed += _client_Closed;
                            _client.Error += _client_Error;
                            _client.MessageReceived += _client_MessageReceived; ;
                            _client.EnableAutoSendPing = true;
                            await _client.OpenAsync();
                            _reconnectDelay = 1000;
                        }
                        catch (TaskCanceledException)
                        {
                            _logger.LogInformation("WebSocket client task was cancelled");
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError(ex, $"An exception occurred in WebSocket while connecting to {_uri}");
                            OnError?.Invoke();
                            TryHandleReconnect();
                        }
                    }, _cancellationToken.Token);
                }
            }
        }

        private void _client_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            _logger.LogDebug($"Message received from {_uri}: {e.Message}");
            OnMessageReceived?.Invoke(null, e.Message);
        }

        private void _client_Opened(object sender, EventArgs e)
        {
            _logger.LogDebug($"Connection to {_uri} opened successfully!");
            OnOpen?.Invoke();
        }

        private void _client_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            _logger.LogError(e.Exception, $"An error occurred in WebSocket while connected to {_uri}");
            OnError?.Invoke();
            TryHandleReconnect();
        }


        private void _client_Closed(object sender, EventArgs e)
        {
            _logger.LogDebug($"WebSocket connection to {_uri} was closed");
            OnClose?.Invoke();
            TryHandleReconnect();
        }

        private int _reconnectDelay = 1000;
        private SemaphoreSlim _reconnectLock = new SemaphoreSlim(1, 1);
        private async void TryHandleReconnect()
        {
            if(!_reconnectLock.Wait(0))
            {
                //_logger.LogInformation("Not trying to reconnect, connectLock already locked.");
                return;
            }
            if (AutoReconnect && !_cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Trying to reconnect to {_uri} in {(int)TimeSpan.FromMilliseconds(_reconnectDelay).TotalSeconds} sec");
                _client = null;
                try
                {
                    await Task.Delay(_reconnectDelay, _cancellationToken.Token);
                    Connect(_uri);
                    _reconnectDelay *= 2;
                    if (_reconnectDelay > 120000)
                    {
                        _reconnectDelay = 120000;
                    }
                }
                catch (TaskCanceledException) { }
            }
            _reconnectLock.Release();
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
