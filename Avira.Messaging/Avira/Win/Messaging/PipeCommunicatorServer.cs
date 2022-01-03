using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using Serilog;

namespace Avira.Win.Messaging
{
    public sealed class PipeCommunicatorServer : IChannelConnectNotifier, IDisposable
    {
        private readonly string pipeName;

        private readonly PipeSecurity pipeSecurity;

        private readonly ConcurrentDictionary<NamedPipeServerStream, bool> pipeServers =
            new ConcurrentDictionary<NamedPipeServerStream, bool>();

        public IAuthorizationChecker AuthorizationChecker { get; set; }

        public event EventHandler<PipeConnectionArgs> PipeConnected;

        public event EventHandler<PipeConnectionArgs> PipeDisconnected;

        public PipeCommunicatorServer(string pipeName)
            : this(pipeName, null)
        {
        }

        public PipeCommunicatorServer(string pipeName, PipeSecurity pipeSecurity)
        {
            this.pipeSecurity = pipeSecurity;
            this.pipeName = pipeName;
            PipeConnected += delegate { };
        }

        private static NamedPipeClientStream CreateOutputStream(NamedPipeServerStream srv)
        {
            string text = new MessageStream(srv).ReadMessage();
            NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", text, PipeDirection.Out,
                PipeOptions.None, TokenImpersonationLevel.Identification);
            namedPipeClientStream.Connect(2000);
            return namedPipeClientStream;
        }

        public void Start()
        {
            CreateListener();
        }

        private void OnConnect(IAsyncResult asyncResult)
        {
            NamedPipeServerStream namedPipeServerStream = (NamedPipeServerStream)asyncResult.AsyncState;

            try
            {
                namedPipeServerStream.EndWaitForConnection(asyncResult);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            CreateListener();
            if (!CheckPipeAuthorization(namedPipeServerStream))
            {
                Log.Warning("Disconnecting unauthorized client.");
                namedPipeServerStream.Close();
                namedPipeServerStream.Dispose();
                return;
            }

            PipeMessenger pipeMessenger = null;

            try
            {
                NamedPipeClientStream outStream = CreateOutputStream(namedPipeServerStream);
                pipeMessenger = new PipeMessenger(namedPipeServerStream, outStream);
                this.PipeConnected?.Invoke(this, new PipeConnectionArgs(pipeMessenger));
                pipeMessenger.RunReadMessageLoop();
                this.PipeDisconnected?.Invoke(this, new PipeConnectionArgs(pipeMessenger));
                Log.Information("Named pipe client disconnected.");
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Failed to connect to pipe.");
            }

            pipeMessenger?.Dispose();
            pipeServers.TryRemove(namedPipeServerStream, out var _);
        }

        private bool CheckPipeAuthorization(NamedPipeServerStream srv)
        {
            if (AuthorizationChecker != null)
            {
                return AuthorizationChecker.Check(srv);
            }

            return true;
        }

        private void CreateListener()
        {
            NamedPipeServerStream namedPipeServerStream = ((pipeSecurity != null)
                ? new NamedPipeServerStream(pipeName, PipeDirection.In, -1, PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous, 0, 0, pipeSecurity)
                : new NamedPipeServerStream(pipeName, PipeDirection.In, -1, PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous));

            pipeServers[namedPipeServerStream] = true;

            try
            {
                Log.Information("Named pipe server " + pipeName + ": start listening ...");
                namedPipeServerStream.BeginWaitForConnection(OnConnect, namedPipeServerStream);
            }
            catch (IOException exception)
            {
                Log.Error(exception, "BeginWaitForConnection failed.");
            }
        }

        public void Dispose()
        {
            foreach (NamedPipeServerStream key in pipeServers.Keys)
            {
                key.Dispose();
            }
        }
    }
}