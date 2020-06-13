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
using ChatCore;
using ChatCore.Interfaces;
using ChatCore.Models.Twitch;
using ChatCore.Services;
using ChatCore.Services.Twitch;

namespace StreamCoreTester
{
    public partial class Form1 : Form
    {
        private ChatServiceMultiplexer streamingService;
        private TwitchService twitchService;
        public Form1()
        {
            InitializeComponent();

            var streamCore = ChatCoreInstance.Create();
            streamingService = streamCore.RunAllServices();
            twitchService = streamingService.GetTwitchService();
            streamingService.OnLogin += StreamingService_OnLogin; 
            streamingService.OnTextMessageReceived += StreamServiceProvider_OnMessageReceived;
            streamingService.OnJoinChannel += StreamServiceProvider_OnChannelJoined;
            streamingService.OnLeaveChannel += StreamServiceProvider_OnLeaveChannel;
            streamingService.OnRoomStateUpdated += StreamServiceProvider_OnChannelStateUpdated;
            //Console.WriteLine($"StreamService is of type {streamServiceProvider.ServiceType.Name}");
        }

        private void StreamingService_OnLogin(IChatService svc)
        {
            if(svc is TwitchService twitchService)
            {
                twitchService.JoinChannel("brian91292");
            }
        }

        private void StreamServiceProvider_OnChannelStateUpdated(IChatService svc, IChatChannel channel)
        {
            Console.WriteLine($"Channel state updated for {channel.GetType().Name} {channel.Id}");
            if (channel is TwitchChannel twitchChannel)
            {
                Console.WriteLine($"RoomId: {twitchChannel.Roomstate.RoomId}");
            }
        }

        private void StreamServiceProvider_OnLeaveChannel(IChatService svc, IChatChannel channel)
        {
            Console.WriteLine($"Left channel {channel.Id}");
        }

        private void StreamServiceProvider_OnChannelJoined(IChatService svc, IChatChannel channel)
        {
            Console.WriteLine($"Joined channel {channel.Id}");
        }

        private void StreamServiceProvider_OnMessageReceived(IChatService svc, IChatMessage msg)
        {
            Console.WriteLine($"{msg.Sender.DisplayName}: {msg.Message}");
            //Console.WriteLine(msg.ToJson().ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            streamingService.GetTwitchService().PartChannel("xqcow");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            streamingService.GetTwitchService().JoinChannel("xqcow");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            streamingService.GetMixerService().SendTextMessage("This is a test message :)", streamingService.GetMixerService().Channels.Values.First());
        }
    }
}
