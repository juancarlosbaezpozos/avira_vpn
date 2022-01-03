using System;
using System.IO;
using System.IO.Pipes;
using Avira.Acp.Logging;

namespace Avira.Acp.Endpoints.NamedPipe
{
    public abstract class NamedPipeListener : INamedPipeListener, IDisposable
    {
        public const int MaxBufferSize = 5242880;

        private readonly INamedPipeAuthenticationService authenticationService;

        private readonly INamedPipeAuthTokenExtractor namedPipeAuthTokenExtractor;

        private readonly bool closeWhenConnectionFinished;

        private readonly object pipeServerStreamLockObject = new object();

        public bool IsAvailable { get; private set; }

        internal ILogger Logger { get; }

        protected NamedPipeServerStream PipeServerStream { get; private set; }

        protected bool Closing { get; private set; }

        public event EventHandler PipeConnected;

        public event EventHandler ThreadFinished;

        public event EventHandler Restarted;

        protected NamedPipeListener(NamedPipeServerStream pipeServerStream,
            INamedPipeAuthenticationService authenticationService,
            INamedPipeAuthTokenExtractor namedPipeAuthTokenExtractor)
            : this(pipeServerStream, authenticationService, namedPipeAuthTokenExtractor,
                closeWhenConnectionFinished: true)
        {
        }

        protected NamedPipeListener(NamedPipeServerStream pipeServerStream,
            INamedPipeAuthenticationService authenticationService,
            INamedPipeAuthTokenExtractor namedPipeAuthTokenExtractor, bool closeWhenConnectionFinished)
        {
            this.authenticationService = authenticationService;
            this.namedPipeAuthTokenExtractor = namedPipeAuthTokenExtractor;
            this.closeWhenConnectionFinished = closeWhenConnectionFinished;
            PipeServerStream = pipeServerStream;
            Logger = LoggerFacade.GetCurrentClassLogger();
        }

        public void Start()
        {
            if (!Closing)
            {
                lock (pipeServerStreamLockObject)
                {
                    IsAvailable = true;
                    PipeServerStream?.BeginWaitForConnection(OnWaitForConnectionFinished, null);
                }
            }
        }

        public virtual void Close()
        {
            Closing = true;
            Logger.Info("Pipe is closing");
            lock (pipeServerStreamLockObject)
            {
                PipeServerStream?.Close();
            }
        }

        protected virtual void Restart()
        {
            if (Closing)
            {
                return;
            }

            lock (pipeServerStreamLockObject)
            {
                try
                {
                    PipeServerStream.Disconnect();
                }
                catch (InvalidOperationException)
                {
                }
                catch (IOException)
                {
                }

                Start();
                this.Restarted?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnWaitForConnectionFinished(IAsyncResult result)
        {
            IsAvailable = false;
            if (Closing)
            {
                this.ThreadFinished?.Invoke(this, EventArgs.Empty);
                return;
            }

            try
            {
                PipeServerStream.EndWaitForConnection(result);
                this.PipeConnected?.Invoke(this, EventArgs.Empty);
                if (ProcessAuthentication() == NamedPipeAuthenticationResult.Succeeded)
                {
                    ProcessMessages();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("NamedPipeListener thread stopped due to exception: {0}", ex);
            }

            if (closeWhenConnectionFinished)
            {
                Logger.Info("Closing pipe after connection finished.");
                Close();
                this.ThreadFinished?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Restart();
            }
        }

        private NamedPipeAuthenticationResult ProcessAuthentication()
        {
            if (authenticationService == null)
            {
                return NamedPipeAuthenticationResult.Succeeded;
            }

            NamedPipeAuthenticationToken namedPipeAuthenticationToken =
                namedPipeAuthTokenExtractor.Extract(PipeServerStream);
            return authenticationService.Authenticate(namedPipeAuthenticationToken);
        }

        protected abstract void ProcessMessages();

        public void Dispose()
        {
            Dispose(disposing: true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
                PipeServerStream = null;
            }
        }
    }
}