using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ChatCore.Interfaces
{
    public interface IChatServiceManager
    {
        void Start(Assembly assembly);
        void Stop(Assembly assembly);
        IChatService GetService();
        bool IsRunning { get; }
        HashSet<Assembly> RegisteredAssemblies { get; }
    }
}
