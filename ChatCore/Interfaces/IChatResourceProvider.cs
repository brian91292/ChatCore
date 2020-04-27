using ChatCore.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ChatCore.Interfaces
{
    internal interface IChatResourceProvider<T>
    {
        ConcurrentDictionary<string, T> Resources { get; }
        Task<bool> TryRequestResources(string category);
        bool TryGetResource(string identifier, string category, out T data);
    }
}
