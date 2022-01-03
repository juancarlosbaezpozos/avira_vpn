using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Avira.VPN.Core
{
    public static class DiContainer
    {
        [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable",
            Justification = "Serialization is not used")]
        [ComVisible(false)]
        public class ExportedTypes : Dictionary<Type, Type>
        {
        }

        public class ExportAttribute : Attribute
        {
            public Type ImplementedType { get; set; }

            public ExportAttribute(Type t)
            {
                ImplementedType = t;
            }
        }

        public static IDiContainer Instance { get; set; } = new DiContainerImpl();


        public static void SetInstance<T>(object value) where T : class
        {
            Instance.SetInstance<T>(value);
        }

        public static T Resolve<T>() where T : class
        {
            return Instance.Resolve<T>();
        }

        public static void Register<T>(Type instanceType, CreationMode creationMode = CreationMode.Singleton)
        {
            Instance.Register<T>(instanceType, creationMode);
        }

        public static void SetActivator<T>(Func<object> func, CreationMode creationMode = CreationMode.Singleton)
        {
            Instance.SetActivator<T>(func, creationMode);
        }

        public static void Register<T>(ExportedTypes exportedTypes, CreationMode creationMode = CreationMode.Singleton)
        {
            Instance.Register<T>(exportedTypes, creationMode);
        }

        public static void SetGetter(string key, Func<object> getter)
        {
            Instance.SetGetter(key, getter);
        }

        public static T GetValue<T>(string key)
        {
            return Instance.GetValue<T>(key);
        }

        public static void Clear()
        {
            Instance.Clear();
        }

        public static ExportedTypes GetExportedTypes(Assembly assembly)
        {
            ExportedTypes exportedTypes = new ExportedTypes();
            foreach (TypeInfo definedType in assembly.DefinedTypes)
            {
                ExportAttribute exportAttribute =
                    CustomAttributeExtensions.GetCustomAttributes(definedType, typeof(ExportAttribute), inherit: false)
                        .FirstOrDefault() as ExportAttribute;
                if (exportAttribute != null)
                {
                    exportedTypes[exportAttribute.ImplementedType] = definedType.AsType();
                }
            }

            return exportedTypes;
        }
    }
}