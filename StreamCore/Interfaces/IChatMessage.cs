using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IChatMessage
    {
        string Message { get; }
        string Author { get; }
    }
}
