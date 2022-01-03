using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Avira.VPN.Notifier;
using Avira.Win.Messaging;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.VPN.NotifierClient
{
    public class NotifierClient : IMessengerFactory, INotifierClient, IDisposable
    {
        private IService notifier;

        private readonly ISettings settings;

        private IMessenger messenger;

        private List<Notification> activeNotifications;

        private readonly object activeNotificationsAccess = new object();

        public Action<string, string, string> CustomActionHandler { get; set; }

        public NotifierClient()
            : this(null, null, DiContainer.Resolve<ISettings>())
        {
        }

        public NotifierClient(IMessenger messenger, IService notifier, ISettings settings)
        {
            activeNotifications = new List<Notification>();
            this.messenger = messenger;
            this.notifier = notifier;
            this.settings = settings;
            if (this.notifier != null)
            {
                SubscribeToNotifier();
            }
        }

        internal Notification GetNotification(int uniqueId)
        {
            lock (activeNotificationsAccess)
            {
                return activeNotifications.Find((Notification n) => n.UniqueId == uniqueId);
            }
        }

        public IMessenger GetMessenger(string url)
        {
            if (url != "pipe://" + ProductSettings.NotifierPipeName)
            {
                return null;
            }

            return messenger ?? (messenger = CreateMessenger());
        }

        protected IMessenger CreateMessenger(int timeout = 5000)
        {
            try
            {
                PipeSecurity pipeSecurity = new PipeSecurity();
                pipeSecurity.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User,
                    PipeAccessRights.FullControl, AccessControlType.Allow));
                pipeSecurity.AddAccessRule(new PipeAccessRule(
                    new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), PipeAccessRights.ReadWrite,
                    AccessControlType.Allow));
                pipeSecurity.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.ServiceSid, null),
                    PipeAccessRights.ReadWrite, AccessControlType.Allow));
                pipeSecurity.AddAccessRule(new PipeAccessRule(
                    new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), PipeAccessRights.ReadWrite,
                    AccessControlType.Allow));
                return new PipeMessenger(ProductSettings.NotifierPipeName, timeout, pipeSecurity);
            }
            catch (TimeoutException)
            {
                Log.Information("Notifier not started yet.");
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Failed to create messenger.");
            }

            return null;
        }

        protected void StartNotifier()
        {
            if (!ProductSettings.SpotlightIsActive())
            {
                DesktopShell.ShellExecute(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    ProductSettings.NotifierExe));
            }
        }

        public void Notify(Notification notification)
        {
            if (!TryNotify(notification))
            {
                TryNotify(notification);
            }
        }

        private void ConnectToNotifier()
        {
            if (messenger == null)
            {
                messenger = CreateMessenger(100);
                if (messenger == null)
                {
                    StartNotifier();
                    messenger = CreateMessenger(10000);
                }
            }

            if (messenger == null)
            {
                throw new Exception("Failed to connect to notifier.");
            }
        }

        private void SubscribeForActionNotifications()
        {
            if (notifier == null)
            {
                ServiceInterfaceFactory serviceInterfaceFactory =
                    new ServiceInterfaceFactory(new ServiceLocator(), this);
                notifier = serviceInterfaceFactory.CreateServiceInterface("Notifier");
                SubscribeToNotifier();
            }
        }

        private void ResetNotifierConnectionState()
        {
            messenger?.Dispose();
            messenger = null;
            notifier = null;
            lock (activeNotificationsAccess)
            {
                activeNotifications.Clear();
            }
        }

        private void SendShowNotificationRequest(Notification notification)
        {
            Message message = Message.CreateRequest("notification/show");
            notification.UniqueId = message.Id;
            message.Params = Message.ToJObject(notification);
            lock (activeNotificationsAccess)
            {
                activeNotifications.Add(notification);
            }

            Log.Debug($"Sending notification with id : {notification.UniqueId}");
            notifier.Request(message, null, null);
        }

        private bool TryNotify(Notification notification)
        {
            try
            {
                ConnectToNotifier();
                SubscribeForActionNotifications();
                SendShowNotificationRequest(notification);
                return true;
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Notify failed.");
                ResetNotifierConnectionState();
                return false;
            }
        }

        private void ActionHandler(Message notificationMessage)
        {
            JObject jObject = notificationMessage.Params.ToObject<JObject>();
            int num = jObject["RequestId"]!.ToObject<int>();
            Notification notification = GetNotification(num);
            if (notification == null)
            {
                Log.Debug($"Could not apply notification action. Could not find notification with id : {num}");
                return;
            }

            string text = jObject["ActionId"]!.ToString();
            Notification.Command command = ((notification.Action1?.Id == text)
                ? notification.Action1
                : ((notification.Action2?.Id == text) ? notification.Action2 : null));
            if (command != null && command.Run != null)
            {
                Log.Debug(
                    $"Handling action {command} for {notificationMessage.Method}. Params : {notificationMessage.Params}");
                RunActionWithArguments(notificationMessage.Method, command, notificationMessage.Params);
            }
            else
            {
                string arg = jObject["ActionParam"]!.ToString();
                CustomActionHandler?.Invoke(notification.Id, text, arg);
            }

            OnClosed(num);
        }

        private void RunActionWithArguments(string method, Notification.Command action, JToken arguments)
        {
            string arg = arguments.ToObject<JObject>()!["ActionId"]!.ToString();
            action.Run(method, arg, null);
        }

        private void RunActionClose(Notification.Command closeAction)
        {
            closeAction.Run(string.Empty, closeAction.Id, string.Empty);
        }

        private void OnClosed(int notificationUniqueId)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                Notification notification = GetNotification(notificationUniqueId);
                if (notification != null)
                {
                    lock (activeNotificationsAccess)
                    {
                        activeNotifications.Remove(notification);
                        if (activeNotifications.Count == 0)
                        {
                            UnsubscribeFromNotifier();
                            notifier = null;
                            messenger?.Dispose();
                            messenger = null;
                        }
                    }
                }
            });
        }

        private void SubscribeToNotifier()
        {
            notifier?.Subscribe("action", ActionHandler);
        }

        private void UnsubscribeFromNotifier()
        {
            notifier.Unsubscribe("action", ActionHandler);
        }

        public void Dispose()
        {
            if (messenger != null)
            {
                messenger.Dispose();
            }
        }
    }
}