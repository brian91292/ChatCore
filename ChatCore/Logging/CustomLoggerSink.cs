using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Logging
{
    internal class CustomLoggerSink : ILogger
    {
        CustomSinkProvider _provider;
        string _categoryName;
        public CustomLoggerSink(CustomSinkProvider provider, string categoryName)
        {
            _provider = provider;
            _categoryName = categoryName;
        }
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
#if !DEBUG
            // Debug logs only belong in debug builds
            if (logLevel == LogLevel.Debug)
            {
                return false;
            }
#endif
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            _provider.OnLogReceived((CustomLogLevel)logLevel, _categoryName, formatter(state, exception));
        }
    }
}
