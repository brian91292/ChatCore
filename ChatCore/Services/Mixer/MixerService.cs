using Microsoft.Extensions.Logging;
using ChatCore.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace ChatCore.Services.Mixer
{
    public class MixerService : ChatServiceBase, IChatService
    {
        public string DisplayName { get; } = "Mixer"; 
        public MixerService(ILogger<MixerService> logger, /*MixerMessageParser messageParser,*/ MixerDataProvider mixerDataProvider, IUserAuthProvider authManager, Random rand)
        {
            _logger = logger;
            _dataProvider = mixerDataProvider;
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
        private Dictionary<string, IWebSocketService> _channelSockets = new Dictionary<string, IWebSocketService>();
        private CancellationTokenSource _processMessageQueueCancellation = null;

        private string _userName { get => string.IsNullOrEmpty(_authManager.Credentials.Twitch_OAuthToken) ? _anonUsername : "@"; }
        private string _oAuthToken { get => string.IsNullOrEmpty(_authManager.Credentials.Twitch_OAuthToken) ? "" : _authManager.Credentials.Twitch_OAuthToken; }
        internal async void Start(bool forceReconnect = false)
        {
            if (forceReconnect)
            {
                Stop();
            }

            bool shouldStart = false;
            lock (_initLock)
            {
                if (!_isStarted)
                {
                    _isStarted = true;
                    shouldStart = true;
                    //_websocketService.Connect("wss://chat.mixer.com:443", forceReconnect);
                    _processMessageQueueCancellation = new CancellationTokenSource();
                    Task.Run(ProcessQueuedMessages, _processMessageQueueCancellation.Token);
                }
            }

            if (shouldStart)
            {
                foreach (var channel in _authManager.Credentials.Mixer_Channels)
                {
                    await TryJoinMixerChannel(channel);
                }
            }
        }


        private async Task TryJoinMixerChannel(string channel)
        {
            var channelId = await _dataProvider.GetChannelIdFromUsername(channel);
            if(string.IsNullOrEmpty(channelId))
            {
                return;
            }

            var channelDetails = await _dataProvider.GetChannelDetails(channelId);

            var socket = ChatCoreInstance._serviceProvider.GetService<IWebSocketService>();

        }

        internal void Stop()
        {
            lock (_initLock)
            {
                if (_isStarted)
                {
                    _processMessageQueueCancellation?.Cancel();
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
            try
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
            catch(TaskCanceledException) { }
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
