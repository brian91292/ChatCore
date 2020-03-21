using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreamCore.Exceptions;
using StreamCore.Interfaces;
using StreamCore.Services;
using StreamCore.Services.Mixer;
using StreamCore.Services.Twitch;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace StreamCore
{
    
    public class StreamCoreInstance
    {
        private static object _createLock = new object();
        private static StreamCoreInstance _instance = null;
        private static ServiceProvider _serviceProvider;

        StreamCoreInstance() { }

        public static StreamCoreInstance Create()
        {
            lock (_createLock)
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
                        .AddSingleton<TwitchService>()
                        .AddSingleton<TwitchServiceProvider>()
                        .AddSingleton<MixerService>()
                        .AddSingleton<MixerServiceProvider>()
                        .AddSingleton<IStreamingService>(x =>
                            new StreamingService(
                                x.GetService<ILogger<StreamingService>>(),
                                new List<IStreamingService>()
                                {
                                    x.GetService<TwitchService>(),
                                    x.GetService<MixerService>()
                                }
                            )
                        )
                        .AddSingleton<IStreamingServiceProvider>(x =>
                            new StreamServiceProvider(
                                x.GetService<ILogger<StreamServiceProvider>>(),
                                x.GetService<IStreamingService>(),
                                new List<IStreamingServiceProvider>
                                {
                                    x.GetService<TwitchServiceProvider>(),
                                    x.GetService<MixerServiceProvider>()
                                }
                            )
                        )
                        .AddTransient<IWebSocketService, WebSocket4NetServiceProvider>();
                        //.AddSingleton<IChatMessageHandler, ChatMessageHandler>();
                    _serviceProvider = serviceCollection.BuildServiceProvider();
                    _serviceProvider.GetService<IStreamingServiceProvider>();
                }
                return _instance;
            }
        }

        private object _runLock = new object();
        public IStreamingService RunAllServices()
        {
            lock (_runLock)
            {
                if (_serviceProvider == null)
                {
                    throw new StreamCoreNotInitializedException("Make sure to call StreamCoreInstance.Create() to initialize StreamCore!");
                }
                var services = _serviceProvider.GetService<IStreamingServiceProvider>();
                services.Start();
                return services.GetService();
            }
        }

        public TwitchServiceProvider RunTwitchServices()
        {
            lock (_runLock)
            {
                if (_serviceProvider == null)
                {
                    throw new StreamCoreNotInitializedException("Make sure to call StreamCoreInstance.Create() to initialize StreamCore!");
                }
                var twitch = _serviceProvider.GetService<TwitchServiceProvider>();
                twitch.Start();
                return twitch;
            }
        }

        public MixerServiceProvider RunMixerServices()
        {
            lock (_runLock)
            {
                if (_serviceProvider == null)
                {
                    throw new StreamCoreNotInitializedException("Make sure to call StreamCoreInstance.Create() to initialize StreamCore!");
                }
                var mixer = _serviceProvider.GetService<MixerServiceProvider>();
                mixer.Start();
                return mixer;
            }
        }
    }
}
