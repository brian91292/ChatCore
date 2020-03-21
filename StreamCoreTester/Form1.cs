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

namespace StreamCoreTester
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();


            var streamCore = StreamCoreInstance.Create();
            var streamServiceProvider = streamCore.RunAllServices();
            streamServiceProvider.OnMessageReceived += StreamServiceProvider_OnMessageReceived;

            //Console.WriteLine($"StreamService is of type {streamServiceProvider.ServiceType.Name}");
        }

        private void StreamServiceProvider_OnMessageReceived(StreamCore.Interfaces.IChatMessage msg)
        {
            Console.WriteLine($"Message: {msg.Message}");
            Console.WriteLine($"Metadata: ");
            foreach(var meta in msg.Metadata)
            {
                Console.WriteLine($"{meta.Key}: {meta.Value}");
            }
        }
    }
}
