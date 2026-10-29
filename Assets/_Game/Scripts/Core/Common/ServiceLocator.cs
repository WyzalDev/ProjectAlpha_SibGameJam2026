using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

namespace _Game.Scripts.Core.Common
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, Object> Services = new Dictionary<Type, Object>();

        public static void Register<T>(T service)
        {
            var type = typeof(T);

            if (Services.ContainsKey(type))
            {
                Debug.LogWarning("Service already registered and will be overwritten");
                Services[type] = service;
                return;
            }

            Services.Add(type, service);
        }

        public static void Unregister<T>()
        {
            var type = typeof(T);

            if (!Services.ContainsKey(type))
            {
                Debug.LogWarning("Service not registered");
                return;
            }

            Services.Remove(type);
        }

        public static T GetService<T>()
        {
            var type = typeof(T);

            if (!Services.TryGetValue(type, out var service))
                Debug.LogError("This service is not registered");

            return (T)service;
        }

        public static void Clear()
        {
            Services.Clear();
        }
    }
}