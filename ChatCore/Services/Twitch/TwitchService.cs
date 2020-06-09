using ChatCore.Interfaces;
using ChatCore.Models;
using ChatCore.Models.Twitch;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ChatCore.Services.Twitch
{
    public class TwitchService : ChatServiceBase, IChatService
    {
        private ConcurrentDictionary<string, IChatChannel> _channels = new ConcurrentDictionary<string, IChatChannel>();
        public ReadOnlyDictionary<string, IChatChannel> Channels;
        public TwitchUser LoggedInUser { get; internal set; } = null;

        public string DisplayName { get; } = "Twitch";

        protected ConcurrentDictionary<Assembly, Action<IChatService, string>> _onRawMessageReceivedCallbacks = new ConcurrentDictionary<Assembly, Action<IChatService, string>>();
        public event Action<IChatService, string> OnRawMessageReceived
        {
            add => _onRawMessageReceivedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onRawMessageReceivedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        public TwitchService(ILogger<TwitchService> logger, TwitchMessageParser messageParser, TwitchDataProvider twitchDataProvider, IWebSocketService websocketService, IUserAuthProvider authManager, Random rand)
        {
            _logger = logger;
            _messageParser = messageParser;
            _dataProvider = twitchDataProvider;
            _websocketService = websocketService;
            _authManager = authManager;
            _rand = rand;

            Channels = new ReadOnlyDictionary<string, IChatChannel>(_channels);

            _authManager.OnCredentialsUpdated += _authManager_OnCredentialsUpdated;
            _websocketService.OnOpen += _websocketService_OnOpen;
            _websocketService.OnClose += _websocketService_OnClose;
            _websocketService.OnError += _websocketService_OnError;
            _websocketService.OnMessageReceived += _websocketService_OnMessageReceived;
        }

        private void _authManager_OnCredentialsUpdated(LoginCredentials credentials)
        {
            if (_isStarted)
            {
                Start(true);
            }
        }

        private ILogger _logger;
        private TwitchMessageParser _messageParser;
        private TwitchDataProvider _dataProvider;
        private IWebSocketService _websocketService;
        private IUserAuthProvider _authManager;
        private Random _rand;
        private bool _isStarted = false;
        private string _anonUsername;
        private object _messageReceivedLock = new object(), _initLock = new object();
        private string _loggedInUsername;

        private string _userName { get => string.IsNullOrEmpty(_authManager.Credentials.Twitch_OAuthToken) ? _anonUsername : "@"; }
        private string _oAuthToken { get => string.IsNullOrEmpty(_authManager.Credentials.Twitch_OAuthToken) ? "" : _authManager.Credentials.Twitch_OAuthToken; }

        internal void Start(bool forceReconnect = false)
        {
            if(forceReconnect)
            {
                Stop();
            }
            lock (_initLock)
            {
                if (!_isStarted)
                {
                    _isStarted = true;
                    _websocketService.Connect("wss://irc-ws.chat.twitch.tv:443", forceReconnect);
                    Task.Run(ProcessQueuedMessages);
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
                    _channels.Clear();
                    LoggedInUser = null;
                    _loggedInUsername = null;
                    _websocketService.Disconnect();
                }
            }
        }

        private void _websocketService_OnMessageReceived(Assembly assembly, string rawMessage)
        {
            lock (_messageReceivedLock)
            {
                //_logger.LogInformation("RawMessage: " + rawMessage);
                _onRawMessageReceivedCallbacks?.InvokeAll(assembly, this, rawMessage);
                if (_messageParser.ParseRawMessage(rawMessage, _channels, LoggedInUser, out var parsedMessages))
                {
                    foreach (TwitchMessage twitchMessage in parsedMessages)
                    {
                        if(assembly != null)
                        {
                            twitchMessage.Sender = LoggedInUser;
                        }
                        var twitchChannel = (twitchMessage.Channel as TwitchChannel);
                        if (twitchChannel.Roomstate == null)
                        {
                            twitchChannel.Roomstate = _channels.TryGetValue(twitchMessage.Channel.Id, out var channel) ? (channel as TwitchChannel).Roomstate : new TwitchRoomstate();
                        }
                        switch (twitchMessage.Type)
                        {
                            case "PING":
                                SendRawMessage("PONG :tmi.twitch.tv");
                                continue;
                            case "376":  // successful login
                                _dataProvider.TryRequestGlobalResources();
                                _loggedInUsername = twitchMessage.Channel.Id;
                                // This isn't a typo, when you first sign in your username is in the channel id.
                                _logger.LogInformation($"Logged into Twitch as {_loggedInUsername}");
                                _websocketService.ReconnectDelay = 500;
                                _onLoginCallbacks?.InvokeAll(assembly, this, _logger);
                                foreach (var channel in _authManager.Credentials.Twitch_Channels)
                                {
                                    JoinChannel(channel);
                                }
                                continue;
                            case "NOTICE":
                                switch (twitchMessage.Message)
                                {
                                    case "Login authentication failed":
                                    case "Invalid NICK":
                                        _websocketService.Disconnect();
                                        break;
                                }
                                goto case "PRIVMSG";
                            case "USERNOTICE":
                            case "PRIVMSG":
                                _onTextMessageReceivedCallbacks?.InvokeAll(assembly, this, twitchMessage, _logger);
                                continue;
                            case "JOIN":
                                //_logger.LogInformation($"{twitchMessage.Sender.Name} JOINED {twitchMessage.Channel.Id}. LoggedInuser: {LoggedInUser.Name}");
                                if (twitchMessage.Sender.UserName == _loggedInUsername)
                                {
                                    if (!_channels.ContainsKey(twitchMessage.Channel.Id))
                                    {
                                        _channels[twitchMessage.Channel.Id] = twitchMessage.Channel.AsTwitchChannel();
                                        _logger.LogInformation($"Added channel {twitchMessage.Channel.Id} to the channel list.");
                                        _onJoinRoomCallbacks?.InvokeAll(assembly, this, twitchMessage.Channel, _logger);
                                    }
                                }
                                continue;
                            case "PART":
                                //_logger.LogInformation($"{twitchMessage.Sender.Name} PARTED {twitchMessage.Channel.Id}. LoggedInuser: {LoggedInUser.Name}");
                                if (twitchMessage.Sender.UserName == _loggedInUsername)
                                {
                                    if (_channels.TryRemove(twitchMessage.Channel.Id, out var channel))
                                    {
                                        _dataProvider.TryReleaseChannelResources(twitchMessage.Channel);
                                        _logger.LogInformation($"Removed channel {channel.Id} from the channel list.");
                                        _onLeaveRoomCallbacks?.InvokeAll(assembly, this, twitchMessage.Channel, _logger);
                                    }
                                }
                                continue;
                            case "ROOMSTATE":
                                _channels[twitchMessage.Channel.Id] = twitchMessage.Channel;
                                _dataProvider.TryRequestChannelResources(twitchMessage.Channel, (resources) =>
                                {
                                    _onChannelResourceDataCached?.InvokeAll(assembly, this, twitchMessage.Channel, resources);
                                });
                                _onRoomStateUpdatedCallbacks?.InvokeAll(assembly, this, twitchMessage.Channel, _logger);
                                continue;
                            case "USERSTATE":
                            case "GLOBALUSERSTATE":
                                LoggedInUser = twitchMessage.Sender.AsTwitchUser();
                                if(string.IsNullOrEmpty(LoggedInUser.DisplayName))
                                {
                                    LoggedInUser.DisplayName = _loggedInUsername;
                                }
                                continue;
                            case "CLEARCHAT":
                                twitchMessage.Metadata.TryGetValue("target-user-id", out var targetUser);
                                _onChatClearedCallbacks?.InvokeAll(assembly, this, targetUser, _logger);
                                continue;
                            case "CLEARMSG":
                                if (twitchMessage.Metadata.TryGetValue("target-msg-id", out var targetMessage))
                                {
                                    _onMessageClearedCallbacks?.InvokeAll(assembly, this, targetMessage, _logger);
                                }
                                continue;
                            //case "MODE":
                            //case "NAMES":
                            //case "HOSTTARGET":
                            //case "RECONNECT":
                            //    _logger.LogInformation($"No handler exists for type {twitchMessage.Type}. {rawMessage}");
                            //    continue;
                        }
                    }
                }
            }
        }

        private void _websocketService_OnClose()
        {
            _logger.LogInformation("Twitch connection closed");
        }

        private void _websocketService_OnError()
        {
            _logger.LogError("An error occurred in Twitch connection");
        }

        private void _websocketService_OnOpen()
        {
            _logger.LogInformation("Twitch connection opened");
            _websocketService.SendMessage("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership");
            _anonUsername = $"justinfan{_rand.Next(10000, 1000000)}".ToLower();
            TryLogin();
        }

        private void TryLogin()
        {
            _logger.LogInformation("Trying to login!");
            if (!string.IsNullOrEmpty(_oAuthToken))
            {
                _websocketService.SendMessage($"PASS {_oAuthToken}");
            }
            _websocketService.SendMessage($"NICK {_userName}");
        }

        private void SendRawMessage(Assembly assembly, string rawMessage, bool forwardToSharedClients = false)
        {
            if (_websocketService.IsConnected)
            {
                _websocketService.SendMessage(rawMessage);
                if (forwardToSharedClients)
                {
                    _websocketService_OnMessageReceived(assembly, rawMessage);
                }
            }
            else
            {
                _logger.LogWarning("WebSocket service is not connected!");
            }
        }

        private int _currentMessageCount = 0;
        private DateTime _lastResetTime = DateTime.UtcNow;
        private ConcurrentQueue<KeyValuePair<Assembly, string>> _textMessageQueue = new ConcurrentQueue<KeyValuePair<Assembly, string>>();

        private async Task ProcessQueuedMessages()
        {
            while(_isStarted)
            {
                if (_currentMessageCount >= 20)
                {
                    float remainingMilliseconds = (float)(30000 - (DateTime.UtcNow - _lastResetTime).TotalMilliseconds);
                    if (remainingMilliseconds > 0)
                    {
                        await Task.Delay((int)remainingMilliseconds);
                    }
                }
                if((DateTime.UtcNow - _lastResetTime).TotalSeconds >= 30)
                {
                    _currentMessageCount = 0;
                    _lastResetTime = DateTime.UtcNow;
                }

                if(_textMessageQueue.TryDequeue(out var msg))
                {
                    SendRawMessage(msg.Key, msg.Value, true);
                    _currentMessageCount++;
                }
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Sends a raw message to the Twitch server
        /// </summary>
        /// <param name="rawMessage">The raw message to send.</param>
        /// <param name="forwardToSharedClients">
        /// Whether or not the message should also be sent to other clients in the assembly that implement StreamCore, or only to the Twitch server.<br/>
        /// This should only be set to true if the Twitch server would rebroadcast this message to other external clients as a response to the message.
        /// </param>
        public void SendRawMessage(string rawMessage, bool forwardToSharedClients = false)
        {
            // TODO: rate limit sends to Twitch service
            SendRawMessage(Assembly.GetCallingAssembly(), rawMessage, forwardToSharedClients);
        }

        internal void SendTextMessage(Assembly assembly, string message, string channel)
        {
            _textMessageQueue.Enqueue(new KeyValuePair<Assembly, string>(assembly, $"PRIVMSG #{channel} :{message}"));
        }

        public void SendTextMessage(string message, string channel)
        {
           SendTextMessage(Assembly.GetCallingAssembly(), message, channel);
        }

        public void SendTextMessage(string message, IChatChannel channel)
        {
            if (channel is TwitchChannel)
            {
                SendTextMessage(Assembly.GetCallingAssembly(), message, channel.Id);
            }
        }

        public void SendCommand(string command, string channel)
        {
            SendRawMessage(Assembly.GetCallingAssembly(), $"PRIVMSG #{channel} :/{command}");
        }

        public void JoinChannel(string channel)
        {
            _logger.LogInformation($"Trying to join channel #{channel}");
            SendRawMessage(Assembly.GetCallingAssembly(), $"JOIN #{channel.ToLower()}");
        }

        public void PartChannel(string channel)
        {
            SendRawMessage(Assembly.GetCallingAssembly(), $"PART #{channel.ToLower()}");
        }
    }
}
