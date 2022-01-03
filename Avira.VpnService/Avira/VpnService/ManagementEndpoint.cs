using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Serilog;

namespace Avira.VpnService
{
    public sealed class ManagementEndpoint : IManagementEndpoint, IDisposable
    {
        private delegate void ResponseDelegate(ManagementEndpoint endpoint);

        private enum MessageType
        {
            RequestSuccess,
            RequestError,
            Realtime
        }

        private sealed class RequestMessage
        {
            public bool Active { get; set; }

            public string Body { get; set; }
        }

        private readonly TcpClient tcpClient;

        private readonly Queue<RequestMessage> requestQueue = new Queue<RequestMessage>();

        private readonly ResponseDelegate response = delegate(ManagementEndpoint endpoint) { endpoint.Response(); };

        public event EventHandler<ManagementMessage> MessageReceived;

        public event EventHandler<ManagementMessage> ErrorReceived;

        public event EventHandler<EventArgs> StreamClosed;

        public ManagementEndpoint(string ip, int port)
        {
            tcpClient = new TcpClient(ip, port);
        }

        public void Start()
        {
            response.BeginInvoke(this, null, null);
        }

        private static string GetNextResponse(TextReader reader)
        {
            string result = null;
            try
            {
                result = reader.ReadLine();
                return result;
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "Failed to get response.");
                return result;
            }
        }

        private static MessageType GetType(string body)
        {
            if (body.StartsWith("SUCCESS:"))
            {
                return MessageType.RequestSuccess;
            }

            if (body.StartsWith("ERROR:"))
            {
                return MessageType.RequestError;
            }

            return MessageType.Realtime;
        }

        private void Response()
        {
            using (StreamReader reader = new StreamReader(tcpClient.GetStream()))
            {
                for (string nextResponse = GetNextResponse(reader);
                     nextResponse != null;
                     nextResponse = GetNextResponse(reader))
                {
                    ProcessMessage(nextResponse);
                }
            }

            this.StreamClosed?.Invoke(this, EventArgs.Empty);
        }

        private void ProcessMessage(string message)
        {
            if (!message.Contains("BYTECOUNT"))
            {
                Log.Debug("MI send: " + message);
            }

            switch (GetType(message))
            {
                case MessageType.Realtime:
                    this.MessageReceived?.Invoke(this, new ManagementMessage
                    {
                        Data = message
                    });
                    return;
                case MessageType.RequestError:
                    this.ErrorReceived?.Invoke(this, new ManagementMessage
                    {
                        Data = message
                    });
                    break;
            }

            lock (requestQueue)
            {
                requestQueue.Dequeue();
                ProcessNextRequest();
            }
        }

        private void SendMessage(string command)
        {
            Log.Debug("MI received: " + command);
            command += "\r\n";
            NetworkStream stream = tcpClient.GetStream();
            byte[] bytes = Encoding.ASCII.GetBytes(command);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }

        private void ProcessNextRequest()
        {
            if (requestQueue.Count != 0)
            {
                RequestMessage requestMessage = requestQueue.First();
                if (requestMessage != null && !requestMessage.Active)
                {
                    requestMessage.Active = true;
                    SendMessage(requestMessage.Body);
                }
            }
        }

        public void Request(string command)
        {
            try
            {
                lock (requestQueue)
                {
                    requestQueue.Enqueue(new RequestMessage
                    {
                        Active = false,
                        Body = command
                    });
                    ProcessNextRequest();
                }
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "Failed to process request.");
            }
        }

        public void Dispose()
        {
            if (tcpClient == null)
            {
                return;
            }

            try
            {
                if (tcpClient.Connected)
                {
                    tcpClient.GetStream().Close();
                    tcpClient.Close();
                }
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "ManagementEndPoint Dispose failed.");
            }
        }
    }
}