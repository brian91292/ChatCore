using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using StreamCore;
using StreamCore.Models.Twitch;

namespace StreamCoreTester
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();


            var streamCore = StreamCoreInstance.Create();
            var streamServiceProvider = streamCore.RunTwitchServices();
            streamServiceProvider.OnMessageReceived += StreamServiceProvider_OnMessageReceived;
            streamServiceProvider.OnJoinChannel += StreamServiceProvider_OnChannelJoined;
            streamServiceProvider.OnLeaveChannel += StreamServiceProvider_OnLeaveChannel;
            streamServiceProvider.OnChannelStateUpdated += StreamServiceProvider_OnChannelStateUpdated;
            //Console.WriteLine($"StreamService is of type {streamServiceProvider.ServiceType.Name}");
        }

        private void StreamServiceProvider_OnChannelStateUpdated(StreamCore.Interfaces.IChatChannel channel)
        {
            Console.WriteLine($"Channel state updated for {channel.GetType().Name} {channel.Id}");
            var twitchChannel = (TwitchChannel)channel;
            Console.WriteLine($"RoomId: {twitchChannel.Roomstate.RoomId}");
        }

        private void StreamServiceProvider_OnLeaveChannel(StreamCore.Interfaces.IChatChannel channel)
        {
            Console.WriteLine($"Left channel {channel.Id}");
        }

        private void StreamServiceProvider_OnChannelJoined(StreamCore.Interfaces.IChatChannel channel)
        {
            Console.WriteLine($"Joined channel {channel.Id}");
        }

        private void StreamServiceProvider_OnMessageReceived(StreamCore.Interfaces.IChatMessage msg)
        {
            Console.WriteLine($"{msg.Sender.Name}: {msg.Message}");
            //Console.WriteLine("Badges: ");
            foreach (var badge in msg.Sender.Badges)
            {
                Console.WriteLine($"Badge: {badge.Name}, URI: {badge.Uri}");
            }
            //Console.WriteLine($"Metadata: ");
            //foreach (var meta in msg.Metadata)
            //{
            //    Console.WriteLine($"{meta.Key}: {meta.Value}");
            //}
        }
    }
}
