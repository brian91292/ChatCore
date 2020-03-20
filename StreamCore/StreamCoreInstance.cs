using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using StreamCore.Services;
using System;

namespace StreamCore
{
    
    public class StreamCoreInstance
    {
        private static object _lock = new object();
        private static StreamCoreInstance _instance = null;
        private static ServiceProvider _serviceProvider;

        StreamCoreInstance() { }

        public static StreamCoreInstance Create()
        {
            lock (_lock)
            {
                if (_instance is null)
                {
                    _instance = new StreamCoreInstance();
                    var serviceCollection = new ServiceCollection();
                    serviceCollection
                        .AddLogging(builder =>
                        {
                            builder.AddConsole();
                        })
                        .AddSingleton<CoreService>()
                        .AddSingleton<IChatMessageHandler, ChatMessageHandler>();
                    _serviceProvider = serviceCollection.BuildServiceProvider();
                    _serviceProvider.GetService<CoreService>();
                }
                return _instance;
            }
        }

        ~StreamCoreInstance()
        {
            _serviceProvider?.Dispose();
        }
    }
}
