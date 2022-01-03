using System;
using System.Collections.Generic;

namespace Avira.VPN.Core
{
    public class DiContainerImpl : IDiContainer
    {
        private Dictionary<Type, object> instances = new Dictionary<Type, object>();

        private Dictionary<Type, Func<object>> activators = new Dictionary<Type, Func<object>>();

        private Dictionary<Type, Func<object>> nonSharedActivators = new Dictionary<Type, Func<object>>();

        private Dictionary<string, Func<object>> values = new Dictionary<string, Func<object>>();

        public void SetInstance<T>(object value) where T : class
        {
            instances[typeof(T)] = value;
        }

        public T Resolve<T>() where T : class
        {
            if (instances.TryGetValue(typeof(T), out var value) && value != null)
            {
                return (T)value;
            }

            if (activators.TryGetValue(typeof(T), out var value2))
            {
                object obj = value2();
                instances[typeof(T)] = obj;
                return (T)obj;
            }

            if (nonSharedActivators.TryGetValue(typeof(T), out value2))
            {
                return (T)value2();
            }

            return null;
        }

        public void SetActivator<T>(Func<object> func, CreationMode creationMode = CreationMode.Singleton)
        {
            if (creationMode == CreationMode.Multiple)
            {
                nonSharedActivators[typeof(T)] = func;
            }
            else
            {
                activators[typeof(T)] = func;
            }
        }

        public void Register<T>(Type instanceType, CreationMode creationMode = CreationMode.Singleton)
        {
            if (creationMode == CreationMode.Multiple)
            {
                nonSharedActivators[typeof(T)] = () => Activator.CreateInstance(instanceType);
            }
            else
            {
                activators[typeof(T)] = () => Activator.CreateInstance(instanceType);
            }
        }

        public void Register<T>(DiContainer.ExportedTypes exportedTypes,
            CreationMode creationMode = CreationMode.Singleton)
        {
            exportedTypes.TryGetValue(typeof(T), out var value);
            if ((object)value == null)
            {
                throw new InvalidCastException($"Type {typeof(T)} could not be resolved");
            }

            Register<T>(value, creationMode);
        }

        public void SetGetter(string key, Func<object> getter)
        {
            values[key] = getter;
        }

        public T GetValue<T>(string key)
        {
            if (values.TryGetValue(key, out var value))
            {
                return (T)value();
            }

            return default(T);
        }

        public void Clear()
        {
            instances.Clear();
        }
    }
}