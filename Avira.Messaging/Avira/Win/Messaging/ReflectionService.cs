using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.Win.Messaging
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
            return Delegate.CreateDelegate(targetDelegateType, originalDelegate.Target, originalDelegate.Method);
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
                Log.Warning(ex, "Failed to handle request.");
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
            MemberInfo[] events = rootObject.GetType().GetEvents();
            AddMemberRoutes(dictionary, events);
            events = rootObject.GetType().GetMethods();
            AddMemberRoutes(dictionary, events);
            return dictionary.Keys.ToList();
        }

        public void Subscribe(string messageCommand, Action<Message> onMessage)
        {
            EventInfo eventInfo = FindEventInfo(messageCommand);
            if (eventInfo == null)
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
                value.Add(CreateSubbsSubscriptionInfo(messageCommand, onMessage, eventInfo));
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
            if (methodInfo == null)
            {
                return false;
            }

            object[] parameters = GetParameters(message, methodInfo);
            object result = methodInfo.Invoke(rootObject, parameters);
            Message obj = ((methodInfo.ReturnType == typeof(void))
                ? Message.CreateResponse(message, "OK")
                : Message.CreateResponse(message, result));
            onResponse(obj);
            return true;
        }

        private object[] GetParameters(Message message, MethodInfo methodInfo)
        {
            object[] result = null;
            ParameterInfo[] parameters = methodInfo.GetParameters();
            if (parameters.Length == 1)
            {
                result = new object[1] { Message.ToType(parameters[0].ParameterType, message.Params) };
            }

            return result;
        }

        private MethodInfo FindMethodInfo(string method)
        {
            return FindMemberInfo(method, rootObject.GetType().GetMethods());
        }

        private PropertyInfo FindProperty(string method)
        {
            if (method.EndsWith("/set") || method.EndsWith("/get"))
            {
                method = method.Substring(0, method.Length - "/set".Length);
            }

            return FindMemberInfo(method, rootObject.GetType().GetProperties());
        }

        private bool TryHandleProperty(Message message, Action<Message> onResponse)
        {
            PropertyInfo propertyInfo = FindProperty(message.Method);
            if (propertyInfo == null)
            {
                return false;
            }

            Message obj;
            if (message.Params == null || message.Method.EndsWith("/get"))
            {
                object value = propertyInfo.GetValue(rootObject);
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

        private void TrySendInitialNotification(string messageCommand, Action<Message> onMessage, EventInfo eventInfo)
        {
            MethodInfo methodInfo = FindMethodInfo(messageCommand);
            RoutingAttribute routingAttribute =
                eventInfo.GetCustomAttributes(typeof(RoutingAttribute)).FirstOrDefault() as RoutingAttribute;
            if (methodInfo != null && methodInfo.ReturnType != typeof(void) && routingAttribute != null &&
                routingAttribute.AutoPublishOnSubscribe)
            {
                object data = methodInfo.Invoke(rootObject, null);
                Message message = Message.CreateNotification(messageCommand);
                message.Params = Message.ToJObject(data);
                onMessage(message);
            }
        }

        private T FindMemberInfo<T>(string messageCommand, T[] infoList) where T : MemberInfo
        {
            foreach (T val in infoList)
            {
                RoutingAttribute routingAttribute =
                    val.GetCustomAttributes(typeof(RoutingAttribute)).FirstOrDefault() as RoutingAttribute;
                if (routingAttribute != null && routingAttribute.Route == messageCommand)
                {
                    return val;
                }
            }

            return null;
        }

        private EventInfo FindEventInfo(string messageCommand)
        {
            return FindMemberInfo(messageCommand, rootObject.GetType().GetEvents());
        }

        private SubscriptionInfo CreateSubbsSubscriptionInfo(string messageCommand, Action<Message> onMessage,
            EventInfo eventInfo)
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
            eventInfo.AddEventHandler(rootObject, handler);
            return result;
        }

        private void RemoveEventHandler(string messageCommand, SubscriptionInfo subscriptionInfo)
        {
            FindEventInfo(messageCommand)?.RemoveEventHandler(rootObject, subscriptionInfo.Handler);
        }

        public void HandleEvent(object sender, object eventArgs, Action<Message> publish, string method)
        {
            Message message = null;
            if (eventArgs.GetType() == typeof(EventArgs))
            {
                message = Message.CreateNotification(method);
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
            PropertyInfo property = eventArgs.GetType().GetProperty("Value");
            if (!(property != null))
            {
                return eventArgs;
            }

            return property.GetValue(eventArgs);
        }

        private void AddMemberRoutes(Dictionary<string, bool> routes, MemberInfo[] memberInfos)
        {
            for (int i = 0; i < memberInfos.Length; i++)
            {
                RoutingAttribute routingAttribute =
                    memberInfos[i].GetCustomAttributes(typeof(RoutingAttribute)).FirstOrDefault() as RoutingAttribute;
                if (routingAttribute != null)
                {
                    routes[routingAttribute.Route] = true;
                }
            }
        }

        private void AddPropertyRoutes(Dictionary<string, bool> routes)
        {
            PropertyInfo[] properties = rootObject.GetType().GetProperties();
            foreach (PropertyInfo propertyInfo in properties)
            {
                RoutingAttribute routingAttribute =
                    propertyInfo.GetCustomAttributes(typeof(RoutingAttribute)).FirstOrDefault() as RoutingAttribute;
                if (routingAttribute != null)
                {
                    if (propertyInfo.CanRead)
                    {
                        routes[routingAttribute.Route + "/get"] = true;
                    }

                    if (propertyInfo.CanWrite)
                    {
                        routes[routingAttribute.Route + "/set"] = true;
                    }
                }
            }
        }
    }
}