using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StreamCore
{
    public static class DictionaryUtils
    {
        public static void AddAction(this ConcurrentDictionary<Assembly, Action> dict, Assembly assembly, Action value)
        {
            if (!dict.TryGetValue(assembly, out var action))
            {
                action = new Action(value);
                dict[assembly] = action;
            }
            action += value;
        }
        public static void AddAction<A>(this ConcurrentDictionary<Assembly, Action<A>> dict, Assembly assembly, Action<A> value)
        {
            if (!dict.TryGetValue(assembly, out var action))
            {
                action = new Action<A>(value);
                dict[assembly] = action;
            }
            action += value;
        }
        public static void AddAction<A,B>(this ConcurrentDictionary<Assembly, Action<A,B>> dict, Assembly assembly, Action<A,B> value)
        {
            if (!dict.TryGetValue(assembly, out var action))
            {
                action = new Action<A,B>(value);
                dict[assembly] = action;
            }
            action += value;
        }

        public static void RemoveAction(this ConcurrentDictionary<Assembly, Action> dict, Assembly assembly, Action value)
        {
            if (!dict.TryGetValue(assembly, out var action))
            {
                return;
            }
            action -= value;
        }
        public static void RemoveAction<A>(this ConcurrentDictionary<Assembly, Action<A>> dict, Assembly assembly, Action<A> value)
        {
            if (!dict.TryGetValue(assembly, out var action))
            {
                return;
            }
            action -= value;
        }
        public static void RemoveAction<A,B>(this ConcurrentDictionary<Assembly, Action<A,B>> dict, Assembly assembly, Action<A,B> value)
        {
            if (!dict.TryGetValue(assembly, out var action))
            {
                return;
            }
            action -= value;
        }

        public static void InvokeAll(this ConcurrentDictionary<Assembly, Action> dict, Assembly assembly, ILogger logger = null)
        {
            foreach (var kvp in dict)
            {
                if (kvp.Key == assembly)
                {
                    continue;
                }
                try
                {
                    kvp.Value?.Invoke();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, $"An exception occurred while invoking action no params.");
                }
            }
        }
        public static void InvokeAll<A>(this ConcurrentDictionary<Assembly, Action<A>> dict, Assembly assembly, A a, ILogger logger = null)
        {
            foreach (var kvp in dict)
            {
                if (kvp.Key == assembly)
                {
                    continue;
                }
                try
                {
                    kvp.Value?.Invoke(a);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, $"An exception occurred while invoking action with param type {typeof(A).Name}");
                }
            }
        }
        public static void InvokeAll<A,B>(this ConcurrentDictionary<Assembly, Action<A,B>> dict, Assembly assembly, A a, B b, ILogger logger = null)
        {
            foreach (var kvp in dict)
            {
                if (kvp.Key == assembly)
                {
                    continue;
                }
                try
                {
                    kvp.Value?.Invoke(a, b);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, $"An exception occurred while invoking action with param type {typeof(A).Name}");
                }
            }
        }
    }
}
