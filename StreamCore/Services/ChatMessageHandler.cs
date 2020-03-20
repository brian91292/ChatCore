using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Services
{
    public class ChatMessageHandler : IChatMessageHandler, IDisposable
    {
        private ILogger _logger;
        public ChatMessageHandler(ILogger<ChatMessageHandler> logger)
        {
            _logger = logger;
        }

        public void OnMessageReceived(IChatMessage message)
        {
            _logger.LogInformation($"Received new chat message from {message.Author}. Message: {message.Message}");
        }

        public void Dispose()
        {

        }
    }
}
