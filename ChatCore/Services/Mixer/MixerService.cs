using Microsoft.Extensions.Logging;
using ChatCore.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChatCore.Services.Mixer
{
    public class MixerService : ChatServiceBase, IChatService
    {
        public string DisplayName { get; } = "Mixer"; 
        public MixerService(ILogger<MixerService> logger, /*MixerMessageParser messageParser,*/ MixerDataProvider mixerDataProvider, IWebSocketService websocketService, IUserAuthProvider authManager, Random rand)
        {
            _logger = logger;
            _dataProvider = mixerDataProvider;
            _websocketService = websocketService;
            _authManager = authManager;
            _rand = rand;

            _authManager.OnCredentialsUpdated += _authManager_OnCredentialsUpdated;
        }

        private void _authManager_OnCredentialsUpdated(Models.LoginCredentials obj)
        {
            if (_isStarted)
            {
                Start(true);
            }
        }

        private ILogger _logger;
        //private MixerMessageParser _messageParser;
        private MixerDataProvider _dataProvider;
        private IWebSocketService _websocketService;
        private IUserAuthProvider _authManager;
        private Random _rand;
        private bool _isStarted = false;
        private string _anonUsername;
        private object _messageReceivedLock = new object(), _initLock = new object();
        private string _loggedInUsername;

        private string _userName { get => string.IsNullOrEmpty(_authManager.Credentials.Twitch_OAuthToken) ? _anonUsername : "@"; }
        private string _oAuthToken { get => string.IsNullOrEmpty(_authManager.Credentials.Twitch_OAuthToken) ? "" : _authManager.Credentials.Twitch_OAuthToken; }
        internal async void Start(bool forceReconnect = false)
        {
            if (forceReconnect)
            {
                Stop();
            }

            _logger.LogInformation("Trying to get ninja channel details...");
            var details = await _dataProvider.GetChannelDetails(await _dataProvider.GetChannelIdFromUsername("test"));
            _logger.LogInformation($"Mixer channel details for ninja: {details?.ToJson() ?? "NULL"}");

            lock (_initLock)
            {
                if (!_isStarted)
                {
                    _isStarted = true;


                    //_websocketService.Connect("wss://chat.mixer.com:443", forceReconnect);
                    //Task.Run(ProcessQueuedMessages);
                }
            }
        }

        internal void Stop()
        {
            lock (_initLock)
            {
                if (_isStarted)
                {
                    _isStarted = false;
                    // TODO: reimplement this
                    //_channels.Clear();
                    //LoggedInUser = null;
                    _loggedInUsername = null;
                    _websocketService.Disconnect();
                }
            }
        }

        private int _currentMessageCount = 0;
        private DateTime _lastResetTime = DateTime.UtcNow;
        private ConcurrentQueue<string> _textMessageQueue = new ConcurrentQueue<string>();

        private async Task ProcessQueuedMessages()
        {
            while (_isStarted)
            {
                if (_currentMessageCount >= 20)
                {
                    float remainingMilliseconds = (float)(30000 - (DateTime.UtcNow - _lastResetTime).TotalMilliseconds);
                    if (remainingMilliseconds > 0)
                    {
                        await Task.Delay((int)remainingMilliseconds);
                    }
                }
                if ((DateTime.UtcNow - _lastResetTime).TotalSeconds >= 30)
                {
                    _currentMessageCount = 0;
                    _lastResetTime = DateTime.UtcNow;
                }

                if (_textMessageQueue.TryDequeue(out var msg))
                {
                    // TODO: reimplement this
                    //SendRawMessage(msg, true);
                    _currentMessageCount++;
                }
            }
        }


        public void SendTextMessage(string message, IChatChannel channel)
        {
            //if(channel is MixerChannel)
            //{
            //    SendTextMessage(message, channel.Id);
            //}
        }
    }
}
