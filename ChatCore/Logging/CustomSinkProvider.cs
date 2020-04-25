using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Logging
{
    internal class CustomSinkProvider : ILoggerProvider
    {
        private ChatCoreInstance _sc;
        public CustomSinkProvider(ChatCoreInstance sc)
        {
            _sc = sc;
        }

        internal void OnLogReceived(CustomLogLevel level, string categoryName, string message)
        {
            _sc.OnLogReceivedInternal(level, categoryName, message);
        }

        private readonly ConcurrentDictionary<string, CustomLoggerSink> _loggers = new ConcurrentDictionary<string, CustomLoggerSink>();
        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new CustomLoggerSink(this, categoryName));
        }
        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
