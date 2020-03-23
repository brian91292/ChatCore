using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StreamCore.Utilities
{
    public static class DictionaryUtils
    {
        public static void AddAction<A>(this ConcurrentDictionary<Assembly, Action<A>> dict , Assembly assembly, Action<A> value)
        {
            if (!dict.TryGetValue(assembly, out var action))
            {
                action = new Action<A>(value);
                dict[assembly] = action;
            }
            action += value;
        }

        public static void RemoveAction<A>(this ConcurrentDictionary<Assembly, Action<A>> dict, Assembly assembly, Action<A> value)
        {
            if (!dict.TryGetValue(assembly, out var action))
            {
                return;
            }
            action -= value;
        }

        public static void InvokeAll<A>(this ConcurrentDictionary<Assembly, Action<A>> dict, Assembly assembly, A data, ILogger logger = null)
        {
            foreach (var kvp in dict)
            {
                if (kvp.Key == assembly)
                {
                    continue;
                }
                try
                {
                    kvp.Value?.Invoke(data);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, $"An exception occurred while invoking action with param type {typeof(A).Name}");
                }
            }
        }
    }
}
