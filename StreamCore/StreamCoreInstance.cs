using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreamCore.Exceptions;
using StreamCore.Interfaces;
using StreamCore.Models;
using StreamCore.Services;
using StreamCore.Services.Mixer;
using StreamCore.Services.Twitch;
using StreamCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
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
                        .AddSingleton<TwitchMessageParser>()
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
                            new StreamingServiceManager(
                                x.GetService<ILogger<StreamingServiceManager>>(),
                                x.GetService<IStreamingService>(),
                                new List<IStreamingServiceManager>
                                {
                                    x.GetService<TwitchServiceManager>(),
                                    x.GetService<MixerServiceManager>()
                                }
                            )
                        )
                        .AddSingleton<IPathProvider, PathProvider>()
                        .AddSingleton<ISettingsProvider, MainSettingsProvider>()
                        .AddSingleton<IUserAuthManager, UserAuthManager>()
                        .AddSingleton<IWebLoginProvider, WebLoginProvider>()
                        .AddTransient<IWebSocketService, WebSocket4NetServiceProvider>();
                    _serviceProvider = serviceCollection.BuildServiceProvider();
                    if (_serviceProvider.GetService<ISettingsProvider>().RunWebApp)
                    {
                        _serviceProvider.GetService<IWebLoginProvider>().Start();
                    }
                }
                return _instance;
            }
        }

        private static void _webLoginProvider_OnLoginDataUpdated(Models.LoginCredentials obj)
        {
            Console.WriteLine($"Twitch_OAuthToken: {obj.Twitch_OAuthToken}");
        }

        private object _runLock = new object();
        public StreamingService RunAllServices()
        {
            lock (_runLock)
            {
                if (_serviceProvider == null)
                {
                    throw new StreamCoreNotInitializedException("Make sure to call StreamCoreInstance.Create() to initialize StreamCore!");
                }
                var services = _serviceProvider.GetService<IStreamingServiceManager>();
                services.Start();
                return services.GetService() as StreamingService;
            }
        }

        public TwitchService RunTwitchServices()
        {
            lock (_runLock)
            {
                if (_serviceProvider == null)
                {
                    throw new StreamCoreNotInitializedException("Make sure to call StreamCoreInstance.Create() to initialize StreamCore!");
                }
                var twitch = _serviceProvider.GetService<TwitchServiceManager>();
                twitch.Start();
                return twitch.GetService() as TwitchService;
            }
        }

        public MixerService RunMixerServices()
        {
            lock (_runLock)
            {
                if (_serviceProvider == null)
                {
                    throw new StreamCoreNotInitializedException("Make sure to call StreamCoreInstance.Create() to initialize StreamCore!");
                }
                var mixer = _serviceProvider.GetService<MixerServiceManager>();
                mixer.Start();
                return mixer.GetService() as MixerService;
            }
        }
    }
}
