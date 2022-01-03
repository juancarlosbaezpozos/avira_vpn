using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Avira.Acp.Extensions;
using Avira.Acp.Messages;

namespace Avira.Acp.Endpoints.NamedPipe
{
    public class DuplexNamedPipeChannel : NamedPipeListener, IDuplexNamedPipeChannel, IChannel
    {
        internal static class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool WaitNamedPipe(string name, int timeout);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool GetNamedPipeClientProcessId(IntPtr pipeHandle, out uint clientProcessId);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool GetNamedPipeServerProcessId(IntPtr pipeHandle, out uint clientProcessId);
        }

        private const int MaxThreads = 254;

        private const int ConnectionTimeout = 5000;

        private INamedPipeAdapter namedPipeAdapter;

        private NamedPipeClientStream namedPipeClient;

        public INamedPipeAdapter NamedPipeAdapter
        {
            set { namedPipeAdapter = value; }
        }

        public string ReceiverPipeName { get; }

        public DuplexNamedPipeChannel(string pipeName, PipeSecurity pipeSecurity,
            INamedPipeAuthenticationService authenticationService,
            INamedPipeAuthTokenExtractor namedPipeAuthTokenExtractor)
            : base(
                new NamedPipeServerStream(pipeName, PipeDirection.InOut, 254, PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous, 0, 0, pipeSecurity), authenticationService, namedPipeAuthTokenExtractor,
                closeWhenConnectionFinished: false)
        {
            ReceiverPipeName = pipeName;
        }

        public DuplexNamedPipeChannel(string pipeName, PipeSecurity pipeSecurity,
            INamedPipeAuthenticationService authenticationService,
            INamedPipeAuthTokenExtractor namedPipeAuthTokenExtractor, bool closeWhenConnectionFinished)
            : base(
                new NamedPipeServerStream(pipeName, PipeDirection.InOut, 254, PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous, 0, 0, pipeSecurity), authenticationService, namedPipeAuthTokenExtractor,
                closeWhenConnectionFinished)
        {
            ReceiverPipeName = pipeName;
        }

        public virtual void Connect(string pipeName)
        {
            if (!DoesNamedPipeExist(pipeName))
            {
                throw new Exception($"Named pipe '{pipeName}' does not exist.");
            }

            base.Logger.Info($"Connecting to {pipeName}");
            namedPipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            namedPipeClient.Connect(5000);
            VerifyRemoteProcess();
            base.Logger.Info("Connected.");
        }

        public virtual void Send(Request request)
        {
            Send(request.ToString());
        }

        public virtual void Send(Response response)
        {
            Send(response.ToString());
        }

        public virtual void Send(Notification notification)
        {
            Send(notification.ToString());
        }

        private void Send(string message)
        {
            string arg = AcpMessageFormatter.RemoveTokenInformation(message);
            if (namedPipeClient == null)
            {
                base.Logger.Warn($"Sending message is not possible, because stream is closed. Message: {arg}");
                return;
            }

            base.Logger.Debug($"Send async: {arg}");
            namedPipeClient.WriteTextAsync(message, 5242880);
        }

        public virtual string Receive()
        {
            string text = base.PipeServerStream.ReadMessage(5242880);
            base.Logger.Debug($"Receive: {AcpMessageFormatter.RemoveTokenInformation(text)}");
            return text;
        }

        public bool IsOpen()
        {
            if (!base.Closing)
            {
                return base.PipeServerStream.IsConnected;
            }

            return false;
        }

        protected override void ProcessMessages()
        {
            VerifyRemoteProcess();
            namedPipeAdapter.ProcessMessages();
        }

        private bool DoesNamedPipeExist(string pipeName)
        {
            return NativeMethods.WaitNamedPipe($"\\\\.\\pipe\\{pipeName}", 0);
        }

        private void VerifyRemoteProcess()
        {
            if (namedPipeClient != null)
            {
                int serverProcessIdFromPipe = GetServerProcessIdFromPipe(namedPipeClient);
                int clientProcessIdFromPipe = GetClientProcessIdFromPipe(base.PipeServerStream);
                if (serverProcessIdFromPipe != 0 && clientProcessIdFromPipe != 0 &&
                    serverProcessIdFromPipe != clientProcessIdFromPipe)
                {
                    throw new Exception(
                        $"Process verification failed. Remote server has processId {serverProcessIdFromPipe}, remote client has process id {clientProcessIdFromPipe}");
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This call site is trusted.")]
        private int GetClientProcessIdFromPipe(PipeStream stream)
        {
            try
            {
                if (!NativeMethods.GetNamedPipeClientProcessId(stream.SafePipeHandle.DangerousGetHandle(),
                        out var clientProcessId))
                {
                    return 0;
                }

                return (int)clientProcessId;
            }
            catch
            {
                return 0;
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This call site is trusted.")]
        private int GetServerProcessIdFromPipe(PipeStream stream)
        {
            try
            {
                if (!NativeMethods.GetNamedPipeServerProcessId(stream.SafePipeHandle.DangerousGetHandle(),
                        out var clientProcessId))
                {
                    return 0;
                }

                return (int)clientProcessId;
            }
            catch
            {
                return 0;
            }
        }

        public override void Close()
        {
            base.Close();
            CloseClientStream();
        }

        protected override void Restart()
        {
            CloseClientStream();
            base.Restart();
        }

        private void CloseClientStream()
        {
            if (namedPipeClient != null)
            {
                namedPipeClient.Close();
                namedPipeClient = null;
            }
        }
    }
}