using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using StreamCore.Models.Twitch;
using StreamCore.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StreamCore.Services.Twitch
{
    public class TwitchService : StreamingServiceBase, IStreamingService
    {
        private ConcurrentDictionary<string, TwitchChannel> _channels = new ConcurrentDictionary<string, TwitchChannel>();

        public TwitchService(ILogger<TwitchService> logger, TwitchMessageParser messageParser, IWebSocketService websocketService, Random rand)
        {
            _logger = logger;
            _messageParser = messageParser;
            _websocketService = websocketService;
            _rand = rand;
        }

        private ILogger _logger;
        private IChatMessageParser _messageParser;
        private IWebSocketService _websocketService;
        private Random _rand;

        internal void Start()
        {
            _websocketService.OnOpen += _websocketService_OnOpen;
            _websocketService.OnClose += _websocketService_OnClose;
            _websocketService.OnMessageReceived += _websocketService_OnMessageReceived;
            _websocketService.Connect("wss://irc-ws.chat.twitch.tv:443");
        }

        internal void Stop()
        {
            _websocketService.Disconnect();
        }

        private void _websocketService_OnMessageReceived(Assembly assembly, string message)
        {
           
            if (_messageParser.ParseRawMessage(message, out var parsedMessages))
            {
                foreach (TwitchMessage twitchMessage in parsedMessages)
                {
                    var twitchChannel = (twitchMessage.Channel as TwitchChannel);
                    if (twitchChannel.Roomstate == null)
                    {
                        twitchChannel.Roomstate = _channels.TryGetValue(twitchMessage.Channel.Id, out var channel) ? (channel as TwitchChannel).Roomstate : new TwitchRoomstate();
                    }
                    switch (twitchMessage.Type)
                    {
                        case "MODE":
                        case "NAMES":
                        case "CLEARCHAT":
                        case "CLEARMSG":
                        case "HOSTTARGET":
                        case "RECONNECT":
                        case "USERNOTICE":
                        case "USERSTATE":
                        case "GLOBALUSERSTATE":
                            break;
                        case "NOTICE":
                            _onMessageReceivedCallbacks.InvokeAll(assembly, twitchMessage, _logger);
                            continue;
                        case "PING":
                            SendRawMessage("PONG :tmi.twitch.tv");
                            continue;
                        case "001":  // successful login
                            JoinChannel("brian91292"); // TODO: allow user to set channel somehow
                            continue;
                        case "PRIVMSG":
                            _onMessageReceivedCallbacks.InvokeAll(assembly, twitchMessage, _logger);
                            continue;
                        case "JOIN":
                            if(!_channels.ContainsKey(twitchMessage.Channel.Id))
                            {
                                _channels[twitchMessage.Channel.Id] = (TwitchChannel)twitchMessage.Channel;
                                _logger.LogInformation($"Added channel {twitchMessage.Channel.Id} to the channel list.");
                            }
                            _onJoinChannelCallbacks.InvokeAll(assembly, twitchMessage.Channel, _logger);
                            continue;
                        case "PART":
                            if(_channels.TryRemove(twitchMessage.Channel.Id, out var channel))
                            {
                                _logger.LogInformation($"Removed channel {channel.Id} from the channel list.");
                            }
                            continue;
                        case "ROOMSTATE":
                            _channels[twitchMessage.Channel.Id] = (TwitchChannel)twitchMessage.Channel;
                            _onChannelStateUpdatedCallbacks.InvokeAll(assembly, twitchMessage.Channel, _logger);
                            continue;
                    }
                }
            }
        }

        private void _websocketService_OnClose()
        {
            _logger.LogInformation("Twitch connection closed");
        }

        private void _websocketService_OnOpen()
        {
            _logger.LogInformation("Twitch connection opened");
            _websocketService.SendMessage("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership");
            _websocketService.SendMessage($"NICK justinfan{_rand.Next(10000, 1000000)}"); // TODO: implement way to enter credentials
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
            SendRawMessage(Assembly.GetCallingAssembly(), rawMessage, forwardToSharedClients);
        }

        public void SendTextMessage(string message, string channel)
        {
            SendRawMessage(Assembly.GetCallingAssembly(), $"PRIVMSG #{channel} :{message}", true);
        }

        public void SendCommand(string command, string channel)
        {
            SendRawMessage(Assembly.GetCallingAssembly(), $"PRIVMSG #{channel} :/{command}");
        }

        public void JoinChannel(string channel)
        {
            SendRawMessage(Assembly.GetCallingAssembly(), $"JOIN #{channel.ToLower()}");
        }
    }
}
