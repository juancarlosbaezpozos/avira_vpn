using System;

namespace Avira.VPN.Core
{
    public interface IDiContainer
    {
        void Clear();

        T GetValue<T>(string key);

        void Register<T>(DiContainer.ExportedTypes exportedTypes, CreationMode creationMode = CreationMode.Singleton);

        void Register<T>(Type instanceType, CreationMode creationMode = CreationMode.Singleton);

        T Resolve<T>() where T : class;

        void SetActivator<T>(Func<object> func, CreationMode creationMode = CreationMode.Singleton);

        void SetGetter(string key, Func<object> getter);

        void SetInstance<T>(object value) where T : class;
    }
}