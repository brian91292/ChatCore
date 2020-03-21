using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IStreamingServiceProvider
    {
        void Start();
        void Stop();
        IStreamingService GetService();
        bool IsRunning { get; }
    }
}
