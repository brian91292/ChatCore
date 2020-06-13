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
using System.Collections.ObjectModel;

namespace ChatCore.Services.Mixer
{
    public class MixerService : ChatServiceBase, IChatService
    {
        private ConcurrentDictionary<string, IChatChannel> _channels = new ConcurrentDictionary<string, IChatChannel>();
        public ReadOnlyDictionary<string, IChatChannel> Channels;

        public string DisplayName { get; } = "Mixer";

        public MixerService(ILogger<MixerService> logger, MixerMessageParser messageParser, MixerDataProvider mixerDataProvider, IUserAuthProvider authManager, Random rand)
        {
            _logger = logger;
            _dataProvider = mixerDataProvider;
            _messageParser = messageParser;
            _authManager = authManager;
            _rand = rand;

            Channels = new ReadOnlyDictionary<string, IChatChannel>(_channels);

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
        private object _messageReceivedLock = new object(), _initLock = new object();
        private CancellationTokenSource _processMessageQueueCancellation = null;
        private ConcurrentDictionary<string, MixerSentMessageInfo> _sentMessageInfo = new ConcurrentDictionary<string, MixerSentMessageInfo>();

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
                }
            }
        }

        private async Task JoinMixerChannels()
        {
            foreach (var channel in _authManager.Credentials.Mixer_Channels)
            {
                _logger.LogInformation($"Trying to join {channel}");
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
                        SendMixerMessageOfType("auth", channelId, $"[{channelId}, {userId}, \"{channelDetails.authkey}\"]");
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
                            _channels.TryAdd(channelId, new MixerChannel()
                            {
                                Id = channelId,
                                Name = channel,
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
            //_logger.LogInformation($"Mixer Message: {message}");
            lock (_messageReceivedLock)
            {
                // TODO: finish implementing the message parser
                if (_messageParser.ParseRawMessage(message, _channels, null, out var parsedMessages))
                {
                    foreach (MixerMessage mixerMessage in parsedMessages)
                    {
                        switch (mixerMessage.Type)
                        {
                            case "reply":
                                if(_sentMessageInfo.TryGetValue(mixerMessage.Id, out var sentMessageInfo))
                                {
                                    _logger.LogInformation($"Re-broadcasting message {mixerMessage.Id} to local clients.");
                                    Socket_OnMessageReceived(sentMessageInfo.Assembly, mixerMessage.Message.Replace("\"type\":\"reply\"", $"\"type\":\"{sentMessageInfo.MessageType}\""));
                                    _sentMessageInfo.TryRemove(mixerMessage.Id, out var removedMessageInfo);
                                }
                                break;
                            case "ChatMessage":
                                _onTextMessageReceivedCallbacks.InvokeAll(source, this, mixerMessage, _logger);
                                break;
                        }
                    }
                }
            }
        }

        private object _messageIdIncrementLock = new object();
        private long _currentMsgId = 0;
        private long GetCurrentMessageId()
        {
            lock (_messageIdIncrementLock)
            {
                return _currentMsgId++;
            }
        }
        private void SendMixerMessageOfType(Assembly assembly, string method, IChatChannel channel, string rawArguments, bool forwardToSharedClients = false)
        {
            if (channel is MixerChannel mixerChannel)
            {
                if (mixerChannel.Socket.IsConnected)
                {
                    var msgId = GetCurrentMessageId();
                    mixerChannel.Socket.SendMessage($"{{\"type\": \"method\", \"method\": \"{method}\", \"arguments\": {rawArguments}, \"id\": {msgId}}}");
                    if (forwardToSharedClients)
                    {
                        _sentMessageInfo.TryAdd(msgId.ToString(), new MixerSentMessageInfo()
                        {
                            Assembly = assembly,
                            MessageType = method
                        });
                    }
                }
            }
        }

        public void SendMixerMessageOfType(string method, string channelId, string rawArguments)
        {
            if (_channels.TryGetValue(channelId, out var channel))
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
                    Thread.Sleep(10);
                }
            }
            catch(TaskCanceledException) { }
        }

        public void SendTextMessage(string message, IChatChannel channel)
        {
            if (channel is MixerChannel)
            {
                SendMixerMessageOfType(Assembly.GetCallingAssembly(), "msg", channel, $"[\"{message}\"]");
            }
        }
    }
}
