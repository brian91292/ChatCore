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
using System.Net.Http;
using StreamCore.Config;
using System.Collections.Concurrent;

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
                        .AddTransient<HttpClient>()
                        .AddSingleton<ObjectSerializer>()
                        .AddSingleton<MainSettingsProvider>()
                        .AddSingleton<TwitchService>()
                        .AddSingleton<TwitchServiceManager>()
                        .AddSingleton<TwitchMessageParser>()
                        .AddSingleton<TwitchDataProvider>()
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
                        .AddSingleton<IUserAuthProvider, UserAuthProvider>()
                        .AddSingleton<IWebLoginProvider, WebLoginProvider>()
                        .AddSingleton<IEmojiParser, FrwTwemojiParser>()
                        .AddTransient<IWebSocketService, WebSocket4NetServiceProvider>();
                    _serviceProvider = serviceCollection.BuildServiceProvider();
                    if (_serviceProvider.GetService<MainSettingsProvider>().RunWebApp)
                    {
                        _serviceProvider.GetService<IWebLoginProvider>().Start();
                        //System.Diagnostics.Process.Start($"http://localhost:{_serviceProvider.GetService<MainSettingsProvider>().WebAppPort}");
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
        /// <summary>
        /// Starts all services if they haven't been already.
        /// </summary>
        /// <returns>A reference to the generic service multiplexer</returns>
        public StreamingService RunAllServices()
        {
            lock (_runLock)
            {
                if (_serviceProvider == null)
                {
                    throw new StreamCoreNotInitializedException("Make sure to call StreamCoreInstance.Create() to initialize StreamCore!");
                }
                var services = _serviceProvider.GetService<IStreamingServiceManager>();
                services.Start(Assembly.GetCallingAssembly());
                return services.GetService() as StreamingService;
            }
        }
        /// <summary>
        /// Stops all services as long as no references remain. Make sure to unregister any callbacks first!
        /// </summary>
        public void StopAllServices()
        {
            lock (_runLock)
            {
                _serviceProvider.GetService<IStreamingServiceManager>().Stop(Assembly.GetCallingAssembly());
            }
        }
        /// <summary>
        /// Starts the Twitch services if they haven't been already.
        /// </summary>
        /// <returns>A reference to the Twitch service</returns>
        public TwitchService RunTwitchServices()
        {
            lock (_runLock)
            {
                if (_serviceProvider == null)
                {
                    throw new StreamCoreNotInitializedException("Make sure to call StreamCoreInstance.Create() to initialize StreamCore!");
                }
                var twitch = _serviceProvider.GetService<TwitchServiceManager>();
                twitch.Start(Assembly.GetCallingAssembly());
                return twitch.GetService() as TwitchService;
            }
        }
        /// <summary>
        /// Stops the Twitch services as long as no references remain. Make sure to unregister any callbacks first!
        /// </summary>
        public void StopTwitchServices()
        {
            lock (_runLock)
            {
                _serviceProvider.GetService<TwitchServiceManager>().Stop(Assembly.GetCallingAssembly());
            }
        }
        /// <summary>
        /// Starts the Mixer services if they haven't been already.
        /// </summary>
        /// <returns>A reference to the Mixer service</returns>
        public MixerService RunMixerServices()
        {
            lock (_runLock)
            {
                if (_serviceProvider == null)
                {
                    throw new StreamCoreNotInitializedException("Make sure to call StreamCoreInstance.Create() to initialize StreamCore!");
                }
                var mixer = _serviceProvider.GetService<MixerServiceManager>();
                mixer.Start(Assembly.GetCallingAssembly());
                return mixer.GetService() as MixerService;
            }
        }
        /// <summary>
        /// Stops the Mixer services as long as no references remain. Make sure to unregister any callbacks first!
        /// </summary>
        public void StopMixerServices()
        {
            lock (_runLock)
            {
                _serviceProvider.GetService<MixerServiceManager>().Stop(Assembly.GetCallingAssembly());
            }
        }

        private void TryShutdownService(IStreamingServiceManager service)
        {
            lock (_runLock)
            {
                service.Stop(Assembly.GetCallingAssembly());
            }
        }
    }
}
