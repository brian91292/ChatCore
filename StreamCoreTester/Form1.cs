using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using StreamCore;
using StreamCore.Interfaces;
using StreamCore.Models.Twitch;
using StreamCore.Services;
using StreamCore.Services.Twitch;

namespace StreamCoreTester
{
    public partial class Form1 : Form
    {
        private StreamingService streamingService;
        private TwitchService twitchService;
        public Form1()
        {
            InitializeComponent();

            var streamCore = StreamCoreInstance.Create();
            streamingService = streamCore.RunAllServices();
            twitchService = streamingService.GetTwitchService();
            streamingService.OnLogin += StreamingService_OnLogin; 
            streamingService.OnTextMessageReceived += StreamServiceProvider_OnMessageReceived;
            streamingService.OnJoinChannel += StreamServiceProvider_OnChannelJoined;
            streamingService.OnLeaveChannel += StreamServiceProvider_OnLeaveChannel;
            streamingService.OnRoomStateUpdated += StreamServiceProvider_OnChannelStateUpdated;
            //Console.WriteLine($"StreamService is of type {streamServiceProvider.ServiceType.Name}");
        }

        private void StreamingService_OnLogin(IStreamingService svc)
        {
            if(svc is TwitchService twitchService)
            {
                twitchService.JoinChannel("brian91292");
            }
        }

        private void StreamServiceProvider_OnChannelStateUpdated(IStreamingService svc, IChatChannel channel)
        {
            Console.WriteLine($"Channel state updated for {channel.GetType().Name} {channel.Id}");
            if (channel is TwitchChannel twitchChannel)
            {
                Console.WriteLine($"RoomId: {twitchChannel.Roomstate.RoomId}");
            }
        }

        private void StreamServiceProvider_OnLeaveChannel(IStreamingService svc, IChatChannel channel)
        {
            Console.WriteLine($"Left channel {channel.Id}");
        }

        private void StreamServiceProvider_OnChannelJoined(IStreamingService svc, IChatChannel channel)
        {
            Console.WriteLine($"Joined channel {channel.Id}");
        }

        private void StreamServiceProvider_OnMessageReceived(IStreamingService svc, IChatMessage msg)
        {
            Console.WriteLine($"{msg.Sender.Name}: {msg.Message}");
            //Console.WriteLine($"Bytes: {BitConverter.ToString(Encoding.UTF32.GetBytes(msg.Message))}");
            //Console.WriteLine("Badges: ");
            //foreach (var badge in msg.Sender.Badges)
            //{
            //    Console.WriteLine($"Badge: {badge.Name}, URI: {badge.Uri}");
            //}
            //Console.WriteLine($"Metadata: ");
            //foreach (var meta in msg.Metadata)
            //{
            //    Console.WriteLine($"{meta.Key}: {meta.Value}");
            //}
        }

        private void button1_Click(object sender, EventArgs e)
        {
            streamingService.GetTwitchService().PartChannel("xqcow");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            streamingService.GetTwitchService().JoinChannel("xqcow");
        }
    }
}
