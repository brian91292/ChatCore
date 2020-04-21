using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IStreamingServiceManager
    {
        void Start(Assembly assembly);
        void Stop(Assembly assembly);
        IStreamingService GetService();
        bool IsRunning { get; }
        HashSet<Assembly> RegisteredAssemblies { get; }
    }
}
