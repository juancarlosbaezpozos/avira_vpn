using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    public static class AcpFilterHelper
    {
        private static readonly Type DataMemberAttributeType = typeof(DataMemberAttribute);

        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> PropertyInfoCache =
            new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        public static Func<T, bool> ConvertToFilterExpression<T>(IEnumerable<AcpRequestParameter> acpRequestParameters)
        {
            Type typeFromHandle = typeof(T);
            ParameterExpression parameterExpression = Expression.Parameter(typeFromHandle, typeFromHandle.FullName);
            Expression expression = ((Expression<Func<T, bool>>)((T x) => true)).Body;
            foreach (AcpRequestParameter item in acpRequestParameters.Where((AcpRequestParameter parameter) =>
                         parameter.Type == AcpRequestParameter.ParameterType.Filter))
            {
                PropertyInfo property = FindProperty(typeFromHandle, item.Property);
                Expression right = CreateOperatorExpression(parameterExpression, item.Operator, property, item.Value);
                expression = Expression.And(expression, right);
            }

            return Expression.Lambda<Func<T, bool>>(expression, new ParameterExpression[1] { parameterExpression })
                .Compile();
        }

        public static IEnumerable<Resource<T>> ApplyAcpRequestFilter<T>(this IEnumerable<Resource<T>> source,
            IEnumerable<AcpRequestParameter> acpRequestParameters)
        {
            Func<T, bool> filterExpression = ConvertToFilterExpression<T>(acpRequestParameters);
            return source.Where((Resource<T> e) => filterExpression(e.Attributes));
        }

        private static PropertyInfo FindProperty(Type objectType, string propertyName)
        {
            PropertyInfo cachedPropertyInfo = GetCachedPropertyInfo(objectType, propertyName);
            if (cachedPropertyInfo != null)
            {
                return cachedPropertyInfo;
            }

            PropertyInfo[] properties = objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo propertyInfo in properties)
            {
                List<Attribute> list = (from m in Attribute.GetCustomAttributes(propertyInfo, DataMemberAttributeType)
                    where ((DataMemberAttribute)m).Name == propertyName
                    select m).ToList();
                if (list.Count > 1)
                {
                    throw new ApplicationException(
                        $"Found more than one dataMember attribute with name = {propertyName}");
                }

                if (list.FirstOrDefault() != null || propertyInfo.Name == propertyName)
                {
                    CachePropertyInfo(objectType, propertyName, propertyInfo);
                    return propertyInfo;
                }
            }

            throw new ArgumentException("Invalid parameter name", propertyName);
        }

        private static void CachePropertyInfo(Type objectType, string propertyName, PropertyInfo propertyInfo)
        {
            if (!PropertyInfoCache.TryGetValue(objectType, out var value))
            {
                value = new Dictionary<string, PropertyInfo>();
                PropertyInfoCache[objectType] = value;
            }

            value[propertyName] = propertyInfo;
        }

        private static PropertyInfo GetCachedPropertyInfo(Type objectType, string propertyName)
        {
            if (PropertyInfoCache.TryGetValue(objectType, out var value) && value.ContainsKey(propertyName))
            {
                return value[propertyName];
            }

            return null;
        }

        private static Expression CreateOperatorExpression(ParameterExpression parameter,
            AcpRequestParameter.OperatorType @operator, PropertyInfo property, string value)
        {
            Expression expression = Expression.Property(parameter, property);
            Expression right = Expression.Constant(Convert.ChangeType(value, expression.Type));
            return @operator switch
            {
                AcpRequestParameter.OperatorType.Equals => Expression.Equal(expression, right),
                AcpRequestParameter.OperatorType.NotEquals => Expression.NotEqual(expression, right),
                AcpRequestParameter.OperatorType.GreaterThan => Expression.GreaterThan(expression, right),
                AcpRequestParameter.OperatorType.LessThan => Expression.LessThan(expression, right),
                AcpRequestParameter.OperatorType.GreaterEquals => Expression.GreaterThanOrEqual(expression, right),
                AcpRequestParameter.OperatorType.LessEquals => Expression.LessThanOrEqual(expression, right),
                AcpRequestParameter.OperatorType.Unknown => null,
                _ => throw new ArgumentOutOfRangeException(@operator.ToString(), @operator, null),
            };
        }
    }
}