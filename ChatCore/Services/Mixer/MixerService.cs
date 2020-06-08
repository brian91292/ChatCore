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
using ChatCore.SimpleJSON;
using ChatCore.Models.Mixer;

namespace ChatCore.Services.Mixer
{
    public class MixerMessageParser : IChatMessageParser
    {
        public bool ParseRawMessage(string rawMessage, ConcurrentDictionary<string, IChatChannel> channelInfo, IChatUser loggedInUser, out IChatMessage[] parsedMessage)
        {
            throw new NotImplementedException();
        }
    }

    public class MixerService : ChatServiceBase, IChatService
    {
        public string DisplayName { get; } = "Mixer"; 
        public MixerService(ILogger<MixerService> logger, MixerMessageParser messageParser, MixerDataProvider mixerDataProvider, IUserAuthProvider authManager, Random rand)
        {
            _logger = logger;
            _dataProvider = mixerDataProvider;
            _messageParser = messageParser;
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
        private MixerMessageParser _messageParser;
        private MixerDataProvider _dataProvider;
        private IUserAuthProvider _authManager;
        private Random _rand;
        private bool _isStarted = false;
        private string _anonUsername;
        private object _messageReceivedLock = new object(), _initLock = new object();
        private string _loggedInUsername;
        private Dictionary<string, IChatChannel> _channels = new Dictionary<string, IChatChannel>();
        private CancellationTokenSource _processMessageQueueCancellation = null;

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
                Task.Run(JoinMixerChannels);
            }
        }

        internal void Stop()
        {
            lock (_initLock)
            {
                if (_isStarted)
                {
                    _processMessageQueueCancellation?.Cancel();
                    _isStarted = false;
                    foreach(var channel in _channels.Values)
                    {
                        try
                        {
                            if (channel is MixerChannel mixer)
                            {
                                mixer.Socket.Disconnect();
                            }
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError(ex, "An unknown exception occurred while trying to shutdown Mixer channel socket.");
                        }
                    }
                    _channels.Clear();
                    // TODO: reimplement this
                    //_channels.Clear();
                    //LoggedInUser = null;
                    _loggedInUsername = null;
                }
            }
        }

        private async Task JoinMixerChannels()
        {
            foreach (var channel in _authManager.Credentials.Mixer_Channels)
            {
                await TryJoinMixerChannel(channel);
            }
        }

        private async Task TryJoinMixerChannel(string channel)
        {
            var channelId = await _dataProvider.GetChannelIdFromUsername(channel);
            if (string.IsNullOrEmpty(channelId))
            {
                return;
            }

            var userId = await _dataProvider.GetLoggedInUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var channelDetails = await _dataProvider.GetChannelDetails(channelId);
            if (channelDetails != null)
            {
                if (!_isStarted)
                {
                    return;
                }

                lock (_initLock)
                {
                    var socket = ChatCoreInstance._serviceProvider.GetService<IWebSocketService>();
                    Action OnOpenMethod = () =>
                    {
                        // Send the mixer auth command after the connection to the server is opened successfully
                        SendMixerMessageOfType("auth", channel, $"[{channelId}, {userId}, \"{channelDetails.authkey}\"]");
                    };
                    socket.OnOpen -= OnOpenMethod;
                    socket.OnOpen += OnOpenMethod;
                    socket.OnMessageReceived -= Socket_OnMessageReceived;
                    socket.OnMessageReceived += Socket_OnMessageReceived;
                    foreach (var server in channelDetails.endpoints)
                    {
                        //_logger.LogInformation($"Connecting to {server}");
                        try
                        {
                            socket.Connect(server);
                            _channels.Add(channel, new MixerChannel()
                            {
                                Id = channelId,
                                Socket = socket
                            });
                            return;
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError(ex, $"Unknown exception while trying to connect to mixer channel {channel}");
                        }
                    }
                    socket.Dispose();
                }
            }

        }

        private void Socket_OnMessageReceived(Assembly source, string message)
        {
            _logger.LogInformation($"Mixer Message: {message}");
            lock (_messageReceivedLock)
            {
                // TODO: finish implementing the message parser
                //if (_messageParser.ParseRawMessage(message, _channels, LoggedInUser, out var parsedMessages))
                //{ 
                //    foreach(MixerMessage mixerMessage in parsedMessages)
                //    {
                //        switch(mixerMessage.Type)
                //        {
                //            case "reply":

                //                break;
                //        }
                //    }
                //}
            }
        }

        private object _forwardLock = new object();
        private HashSet<long> _forwardOnReply = new HashSet<long>();
        private long currentMsgId = 0;
        private void SendMixerMessageOfType(Assembly assembly, string method, IChatChannel channel, string rawArguments, bool forwardToSharedClients = false)
        {
            if (channel is MixerChannel mixerChannel)
            {
                if (mixerChannel.Socket.IsConnected)
                {
                    var msgId = currentMsgId++;
                    mixerChannel.Socket.SendMessage($"{{\"type\": \"method\", \"method\": \"{method}\", \"arguments\": {rawArguments}, \"id\": {msgId}}}");
                    if (forwardToSharedClients)
                    {
                        lock (_forwardLock)
                        {
                            _forwardOnReply.Add(msgId);
                        }
                    }
                }
            }
        }

        public void SendMixerMessageOfType(string method, string channelName, string rawArguments)
        {
            if (_channels.TryGetValue(channelName, out var channel))
            {
                SendMixerMessageOfType(Assembly.GetCallingAssembly(), method, channel, rawArguments);
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
