using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.Messaging
{
    public class ReflectionService : IService
    {
        private class SubscriptionInfo
        {
            public Action<Message> NotificationChannel { get; set; }

            public int SubscriptionCount { get; set; }

            public Delegate Handler { get; set; }
        }

        private readonly object rootObject;

        private readonly ConcurrentDictionary<string, List<SubscriptionInfo>> subscriptionInfoMap =
            new ConcurrentDictionary<string, List<SubscriptionInfo>>();

        public ReflectionService(object rootObject)
        {
            this.rootObject = rootObject;
        }

        public static Delegate ConvertDelegate(Delegate originalDelegate, Type targetDelegateType)
        {
            return originalDelegate.GetMethodInfo().CreateDelegate(targetDelegateType, originalDelegate.Target);
        }

        public void Request(Message message, Action<Message> onResponse, Action<Message> onError)
        {
            try
            {
                if (TryHandleProperty(message, onResponse) || TryHandleMethod(message, onResponse))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Request failed.");
                Message message2 = Message.CreateFailedResponse(message, JsonRpcErrors.ServerException);
                message2.Error["message"] = (JToken)(ex.InnerException?.Message ?? ex.Message);
                message2.Error["data"] = (JToken)ex.ToString();
                onResponse(message2);
                return;
            }

            onResponse(Message.CreateFailedResponse(message, JsonRpcErrors.MethodNotFound));
        }

        public List<string> GetRoutes()
        {
            Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
            AddPropertyRoutes(dictionary);
            AddMemberRoutes(dictionary, rootObject.GetType().GetTypeInfo().DeclaredEvents);
            AddMemberRoutes(dictionary, rootObject.GetType().GetTypeInfo().DeclaredMethods);
            return dictionary.Keys.ToList();
        }

        public void Subscribe(string messageCommand, Action<Message> onMessage)
        {
            object obj = rootObject;
            EventInfo eventInfo = FindEventInfo(messageCommand, ref obj);
            if ((object)eventInfo == null && messageCommand.EndsWith("/get", StringComparison.CurrentCulture))
            {
                string messageCommand2 = messageCommand.Substring(0, messageCommand.Length - 4);
                eventInfo = FindEventInfo(messageCommand2, ref obj);
            }

            if ((object)eventInfo == null)
            {
                Log.Debug("No eventInfo found for subscription " + messageCommand);
                return;
            }

            if (!subscriptionInfoMap.TryGetValue(messageCommand, out var value))
            {
                value = new List<SubscriptionInfo>();
                if (!subscriptionInfoMap.TryAdd(messageCommand, value))
                {
                    Log.Error("Failed to subscribe to " + messageCommand + ". TryAdd failed.");
                }
            }

            SubscriptionInfo subscriptionInfo =
                value.FirstOrDefault((SubscriptionInfo s) => s.NotificationChannel == onMessage);
            if (subscriptionInfo != null)
            {
                subscriptionInfo.SubscriptionCount++;
            }
            else
            {
                value.Add(CreateSubbsSubscriptionInfo(messageCommand, onMessage, eventInfo, obj));
            }

            TrySendInitialNotification(messageCommand, onMessage, eventInfo);
        }

        public void Unsubscribe(string messageCommand, Action<Message> onMessage)
        {
            if (!subscriptionInfoMap.TryGetValue(messageCommand, out var value) || value == null)
            {
                return;
            }

            SubscriptionInfo subscriptionInfo =
                value.FirstOrDefault((SubscriptionInfo s) => s.NotificationChannel == onMessage);
            if (subscriptionInfo != null)
            {
                subscriptionInfo.SubscriptionCount--;
                if (subscriptionInfo.SubscriptionCount == 0)
                {
                    value.Remove(subscriptionInfo);
                    RemoveEventHandler(messageCommand, subscriptionInfo);
                }

                if (value != null && value.Count == 0)
                {
                    subscriptionInfoMap.TryRemove(messageCommand, out var _);
                }
            }
        }

        private bool TryHandleMethod(Message message, Action<Message> onResponse)
        {
            MethodInfo methodInfo = FindMethodInfo(message.Method);
            if ((object)methodInfo == null)
            {
                return false;
            }

            object[] parameters = GetParameters(message, methodInfo);
            object result = methodInfo.Invoke(rootObject, parameters);
            Message obj = (((object)methodInfo.ReturnType == typeof(void))
                ? Message.CreateResponse(message, "OK")
                : Message.CreateResponse(message, result));
            onResponse(obj);
            return true;
        }

        private object[] GetParameters(Message message, MethodInfo methodInfo)
        {
            object[] array = null;
            ParameterInfo[] parameters = methodInfo.GetParameters();
            if (parameters.Length == 1)
            {
                array = new object[1];
                if (message.Params != null)
                {
                    array[0] = Message.ToType(parameters[0].ParameterType, message.Params);
                }
            }

            return array;
        }

        private MethodInfo FindMethodInfo(string method)
        {
            return FindMemberInfo(method, rootObject.GetType().GetTypeInfo().DeclaredMethods);
        }

        private PropertyInfo FindProperty(string method)
        {
            if (method.EndsWith("/set") || method.EndsWith("/get"))
            {
                method = method.Substring(0, method.Length - "/set".Length);
            }

            return FindMemberInfo(method, rootObject.GetType().GetTypeInfo().DeclaredProperties);
        }

        private bool TryHandleProperty(Message message, Action<Message> onResponse)
        {
            PropertyInfo propertyInfo = FindProperty(message.Method);
            if ((object)propertyInfo == null)
            {
                return false;
            }

            Message obj;
            if (message.Params == null || message.Method.EndsWith("/get"))
            {
                object value = propertyInfo.GetValue(rootObject);
                PropertyInfo propertyInfo2 = FindDataProperty(value);
                if ((object)propertyInfo2 != null)
                {
                    value = propertyInfo2.GetValue(value);
                }

                obj = Message.CreateResponse(message, value);
            }
            else
            {
                object value2 = Message.ToType(propertyInfo.PropertyType, message.Params);
                propertyInfo.SetValue(rootObject, value2);
                obj = Message.CreateResponse(message, "OK");
            }

            onResponse(obj);
            return true;
        }

        private PropertyInfo FindDataProperty(object value)
        {
            PropertyInfo propertyInfo = value.GetType().GetTypeInfo().DeclaredProperties
                .FirstOrDefault((PropertyInfo p) => HasRouting(p, "Data"));
            if ((object)propertyInfo == null)
            {
                propertyInfo = value.GetType().GetTypeInfo().GetDeclaredProperty("Data");
            }

            return propertyInfo;
        }

        private bool HasRouting(MemberInfo methodInfo, string routing)
        {
            RoutingAttribute routingAttribute =
                methodInfo.GetCustomAttribute(typeof(RoutingAttribute)) as RoutingAttribute;
            if (routingAttribute != null)
            {
                return routingAttribute.Route == routing;
            }

            return false;
        }

        private void TrySendInitialNotification(string messageCommand, Action<Message> onMessage, EventInfo eventInfo)
        {
            MethodInfo methodInfo = FindMethodInfo(messageCommand);
            RoutingAttribute routingAttribute =
                eventInfo.GetCustomAttributes(typeof(RoutingAttribute)).FirstOrDefault() as RoutingAttribute;
            if ((object)methodInfo != null && (object)methodInfo.ReturnType != typeof(void) &&
                routingAttribute != null && routingAttribute.AutoPublishOnSubscribe)
            {
                object data = methodInfo.Invoke(rootObject, null);
                Message message = Message.CreateNotification(messageCommand);
                message.Params = Message.ToJObject(data);
                onMessage(message);
            }
        }

        private T FindMemberInfo<T>(string messageCommand, IEnumerable<T> infoList) where T : MemberInfo
        {
            foreach (T info in infoList)
            {
                RoutingAttribute routingAttribute =
                    info.GetCustomAttributes(typeof(RoutingAttribute)).FirstOrDefault() as RoutingAttribute;
                if (routingAttribute != null && routingAttribute.Route == messageCommand)
                {
                    return info;
                }
            }

            return null;
        }

        private EventInfo FindEventInfo(string messageCommand, ref object obj)
        {
            EventInfo eventInfo = FindMemberInfo(messageCommand, rootObject.GetType().GetTypeInfo().DeclaredEvents);
            if ((object)eventInfo == null)
            {
                PropertyInfo propertyInfo =
                    FindMemberInfo(messageCommand, rootObject.GetType().GetTypeInfo().DeclaredProperties);
                if ((object)propertyInfo != null)
                {
                    object value = propertyInfo.GetValue(rootObject);
                    if (value != null)
                    {
                        obj = value;
                        return FindDataChangedEvent(value);
                    }
                }
            }

            return eventInfo;
        }

        private EventInfo FindDataChangedEvent(object obj)
        {
            EventInfo eventInfo = obj.GetType().GetTypeInfo().DeclaredEvents
                .FirstOrDefault((EventInfo e) => HasRouting(e, "DataChanged"));
            if ((object)eventInfo == null)
            {
                eventInfo = obj.GetType().GetTypeInfo().GetDeclaredEvent("DataChanged");
            }

            return eventInfo;
        }

        private SubscriptionInfo CreateSubbsSubscriptionInfo(string messageCommand, Action<Message> onMessage,
            EventInfo eventInfo, object obj)
        {
            Delegate handler =
                ConvertDelegate(
                    (Action<object, object>)delegate(object s, object e)
                    {
                        HandleEvent(s, e, onMessage, messageCommand);
                    }, eventInfo.EventHandlerType);
            SubscriptionInfo result = new SubscriptionInfo
            {
                Handler = handler,
                NotificationChannel = onMessage,
                SubscriptionCount = 1
            };
            eventInfo.AddEventHandler(obj, handler);
            return result;
        }

        private void RemoveEventHandler(string messageCommand, SubscriptionInfo subscriptionInfo)
        {
            object obj = rootObject;
            FindEventInfo(messageCommand, ref obj)?.RemoveEventHandler(obj, subscriptionInfo.Handler);
        }

        public void HandleEvent(object sender, object eventArgs, Action<Message> publish, string method)
        {
            Message message = null;
            if ((object)eventArgs.GetType() == typeof(EventArgs))
            {
                message = Message.CreateNotification(method);
                PropertyInfo declaredProperty = sender.GetType().GetTypeInfo().GetDeclaredProperty("Data");
                if ((object)declaredProperty != null)
                {
                    message.Params = Message.ToJObject(declaredProperty.GetValue(sender));
                }
            }
            else
            {
                message = Message.CreateNotification(method);
                message.Params = Message.ToJObject(GetValue(eventArgs));
            }

            publish(message);
        }

        private object GetValue(object eventArgs)
        {
            PropertyInfo declaredProperty = eventArgs.GetType().GetTypeInfo().GetDeclaredProperty("Value");
            if ((object)declaredProperty == null)
            {
                return eventArgs;
            }

            return declaredProperty.GetValue(eventArgs);
        }

        private void AddMemberRoutes(Dictionary<string, bool> routes, IEnumerable<MemberInfo> memberInfos)
        {
            foreach (MemberInfo memberInfo in memberInfos)
            {
                RoutingAttribute routingAttribute =
                    memberInfo.GetCustomAttributes(typeof(RoutingAttribute)).FirstOrDefault() as RoutingAttribute;
                if (routingAttribute != null)
                {
                    routes[routingAttribute.Route] = true;
                }
            }
        }

        private void AddPropertyRoutes(Dictionary<string, bool> routes)
        {
            foreach (PropertyInfo declaredProperty in rootObject.GetType().GetTypeInfo().DeclaredProperties)
            {
                RoutingAttribute routingAttribute =
                    declaredProperty.GetCustomAttributes(typeof(RoutingAttribute)).FirstOrDefault() as RoutingAttribute;
                if (routingAttribute != null)
                {
                    if (declaredProperty.CanRead)
                    {
                        routes[routingAttribute.Route + "/get"] = true;
                    }

                    if (declaredProperty.CanWrite)
                    {
                        routes[routingAttribute.Route + "/set"] = true;
                    }

                    routes[routingAttribute.Route] = true;
                }
            }
        }
    }
}