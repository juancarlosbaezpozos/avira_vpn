using System;
using System.Linq;
using System.Runtime.Serialization;
using ServiceStack.Text;

namespace Avira.Acp.Extensions
{
    public static class TypeExtensions
    {
        public static string GetAcpTypeName(this Type type)
        {
            DataContractAttribute obj = type.AttributesOfType<DataContractAttribute>().FirstOrDefault() ??
                                        throw new Exception("DataContract not defined for type " + type);
            if (obj.Name == null)
            {
                throw new Exception("Name not explicitly defined in DataContract for type " + type);
            }

            return obj.Name;
        }
    }
}