using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using Serilog;

namespace Avira.Win.Messaging
{
    public class PipeMessenger : IMessenger, IDisposable
    {
        private sealed class PipeMessengerState
        {
            public const int Disconnected = 0;

            public const int Connecting = 1;

            public const int Connected = 2;
        }

        private readonly string outPipeName;

        private readonly PipeSecurity pipeSecurity;

        private MessageStream outMessageStream;

        private int connectionState;

        private PipeStream InStream { get; set; }

        private PipeStream OutStream { get; set; }

        public event EventHandler<MessageReceivedEvent> MessageReceived;

        public event EventHandler ConnectionReestablished;

        public PipeMessenger(PipeStream inStream, PipeStream outStream)
        {
            InStream = inStream;
            OutStream = outStream;
            outMessageStream = new MessageStream(outStream);
        }

        public PipeMessenger(string outPipeName, int timeout = 500, PipeSecurity pipeSecurity = null)
        {
            this.pipeSecurity = pipeSecurity;
            this.outPipeName = outPipeName;
            Connect(timeout);
        }

        public void Send(string message)
        {
            if (outMessageStream == null && string.IsNullOrEmpty(outPipeName))
            {
                throw new MessengerClosedException();
            }

            try
            {
                EnsureConnected();
                outMessageStream?.WriteMessage(message);
            }
            catch (ObjectDisposedException)
            {
                EnsureConnected();
                outMessageStream.WriteMessage(message);
            }
        }

        public void RunReadMessageLoop()
        {
            try
            {
                MessageStream messageStream = new MessageStream(InStream);
                while (true)
                {
                    string message = messageStream.ReadMessage();
                    if (message != "E5661997-FE27-4181-93D5-8EE30FED087E")
                    {
                        ThreadPool.QueueUserWorkItem(delegate { HandleMessage(message); });
                        continue;
                    }

                    break;
                }
            }
            catch (IOException exception)
            {
                Log.Error(exception, "Reading failed.");
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public void Dispose()
        {
            Close();
        }

        private static void Connected(IAsyncResult ar)
        {
            PipeMessenger pipeMessenger = (PipeMessenger)ar.AsyncState;
            if (pipeMessenger?.InStream == null)
            {
                Log.Warning("PipeMessenger.Connected : PipeMessenger instance is not correct");
            }
            else if (2 != Interlocked.CompareExchange(ref pipeMessenger.connectionState, 2, 1))
            {
                try
                {
                    ((NamedPipeServerStream)pipeMessenger.InStream).EndWaitForConnection(ar);
                    pipeMessenger.RunReadMessageLoop();
                }
                catch (Exception exception)
                {
                    Log.Warning(exception, "Failed to connect to pipe.");
                }

                pipeMessenger.Close();
                pipeMessenger.connectionState = 0;
            }
        }

        private NamedPipeServerStream CreateRecievingPipeServer(Stream client)
        {
            string text = Guid.NewGuid().ToString();
            new MessageStream(client).WriteMessage(text);
            if (pipeSecurity == null)
            {
                return new NamedPipeServerStream(text, PipeDirection.In, -1, PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);
            }

            return new NamedPipeServerStream(text, PipeDirection.In, -1, PipeTransmissionMode.Message,
                PipeOptions.Asynchronous, 0, 0, pipeSecurity);
        }

        private void Connect(int timeout = 500)
        {
            if (Interlocked.CompareExchange(ref connectionState, 1, 0) == 0)
            {
                NamedPipeClientStream namedPipeClientStream = null;
                NamedPipeServerStream namedPipeServerStream = null;
                try
                {
                    namedPipeClientStream = new NamedPipeClientStream(".", outPipeName, PipeDirection.Out,
                        PipeOptions.None, TokenImpersonationLevel.Identification);
                    namedPipeClientStream.Connect(timeout);
                    namedPipeServerStream =
                        (NamedPipeServerStream)(InStream = CreateRecievingPipeServer(namedPipeClientStream));
                    OutStream = namedPipeClientStream;
                    outMessageStream = new MessageStream(namedPipeClientStream);
                    namedPipeServerStream.BeginWaitForConnection(Connected, this);
                }
                catch
                {
                    namedPipeClientStream?.Dispose();
                    namedPipeServerStream?.Dispose();
                    Close();
                    connectionState = 0;
                    throw;
                }
            }
        }

        private void Close()
        {
            outMessageStream = null;
            InStream?.Dispose();
            OutStream?.Dispose();
            InStream = null;
            OutStream = null;
        }

        private void EnsureConnected()
        {
            if (connectionState == 0 && !string.IsNullOrEmpty(outPipeName))
            {
                Connect();
                this.ConnectionReestablished?.Invoke(this, EventArgs.Empty);
            }
        }

        private void HandleMessage(string message)
        {
            try
            {
                this.MessageReceived?.Invoke(this, new MessageReceivedEvent
                {
                    Message = message
                });
            }
            catch (Exception ex)
            {
                if (ex is MessengerClosedException || ex is IOException)
                {
                    Log.Warning(ex, "Failed to handle message (pipe closed) : " + message + ".");
                }
                else
                {
                    Log.Error(ex, "Failed to handle message " + message + ".");
                }
            }
        }
    }
}