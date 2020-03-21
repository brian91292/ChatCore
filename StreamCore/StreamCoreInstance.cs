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
                        .AddSingleton<Random>()
                        .AddSingleton<TwitchService>()
                        .AddSingleton<TwitchServiceManager>()
                        .AddSingleton<MixerService>()
                        .AddSingleton<MixerServiceManager>()
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
                        .AddSingleton<IStreamingServiceManager>(x =>
                            new StreamServiceProvider(
                                x.GetService<ILogger<StreamServiceProvider>>(),
                                x.GetService<IStreamingService>(),
                                new List<IStreamingServiceManager>
                                {
                                    x.GetService<TwitchServiceManager>(),
                                    x.GetService<MixerServiceManager>()
                                }
                            )
                        )
                        .AddTransient<IWebSocketService, WebSocket4NetServiceProvider>();
                    _serviceProvider = serviceCollection.BuildServiceProvider();
                    _serviceProvider.GetService<IStreamingServiceManager>();
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
                var services = _serviceProvider.GetService<IStreamingServiceManager>();
                services.Start();
                return services.GetService();
            }
        }

        public TwitchServiceManager RunTwitchServices()
        {
            lock (_runLock)
            {
                if (_serviceProvider == null)
                {
                    throw new StreamCoreNotInitializedException("Make sure to call StreamCoreInstance.Create() to initialize StreamCore!");
                }
                var twitch = _serviceProvider.GetService<TwitchServiceManager>();
                twitch.Start();
                return twitch;
            }
        }

        public MixerServiceManager RunMixerServices()
        {
            lock (_runLock)
            {
                if (_serviceProvider == null)
                {
                    throw new StreamCoreNotInitializedException("Make sure to call StreamCoreInstance.Create() to initialize StreamCore!");
                }
                var mixer = _serviceProvider.GetService<MixerServiceManager>();
                mixer.Start();
                return mixer;
            }
        }
    }
}
