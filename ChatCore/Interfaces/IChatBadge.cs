using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Interfaces
{
    public interface IChatBadge
    {
        string Id { get; }
        string Name { get; }
        string Uri { get; }
    }
}
