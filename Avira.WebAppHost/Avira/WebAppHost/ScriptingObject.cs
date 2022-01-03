using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Avira.Win.Messaging;
using Serilog;

namespace Avira.WebAppHost
{
    [ComVisible(true)]
    public class ScriptingObject
    {
        private object javaScriptCallback;

        private object onCloseCallback;

        private IMessenger messenger;

        public IMessenger Messenger
        {
            get { return messenger; }
            set
            {
                messenger = value;
                if (messenger != null)
                {
                    messenger.MessageReceived += delegate(object sender, MessageReceivedEvent @event)
                    {
                        OnMessage(@event.Message);
                    };
                    messenger.ConnectionReestablished += delegate
                    {
                        OnMessage(
                            Envelope.Pack(Message.CreateNotification("connectionReestablished"), "WebAppHost"));
                    };
                }
            }
        }

        public void Close()
        {
            if (onCloseCallback != null)
            {
                onCloseCallback.GetType().InvokeMember("[DispID=0]", BindingFlags.Instance | BindingFlags.InvokeMethod,
                    null, onCloseCallback, new object[0]);
            }
        }

        public void Trace(string message)
        {
            Log.Debug(message);
        }

        public void SendMessage(string message)
        {
            Messenger.Send(message);
        }

        public void RegisterMessageCallback(object callback)
        {
            javaScriptCallback = callback;
        }

        public void OnMessage(string message)
        {
            try
            {
                javaScriptCallback?.GetType().InvokeMember("[DispID=0]",
                    BindingFlags.Instance | BindingFlags.InvokeMethod, null, javaScriptCallback,
                    new object[1] { message });
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Javascript 'onMessage' callback failed.");
            }
        }

        public void RegisterOnClose(object closeCallback)
        {
            onCloseCallback = closeCallback;
        }
    }
}