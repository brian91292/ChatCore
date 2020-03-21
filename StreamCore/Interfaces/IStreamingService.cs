using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IStreamingService
    {
        Type ServiceType { get; }

        event Action<IChatMessage> OnMessageReceived;
    }
}
