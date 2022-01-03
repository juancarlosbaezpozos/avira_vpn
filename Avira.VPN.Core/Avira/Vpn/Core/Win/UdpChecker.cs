using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Serilog;

namespace Avira.VPN.Core.Win
{
    public sealed class UdpChecker : IDisposable
    {
        private const int UdpDefaultPort = 1194;

        private const int ReceiveTimeout = 2000;

        private readonly UdpClient udpClient;

        private readonly string udpEchoServerUrl;

        private readonly int updClientPort = GetFirstUpdPortAvailable();

        private readonly byte[] magicNumber = Encoding.ASCII.GetBytes("4acd275cd5e99aaacf14672d171f744993be8ac7");

        public UdpChecker(string udpEchoServerUrl)
        {
            this.udpEchoServerUrl = udpEchoServerUrl;
            udpClient = new UdpClient(updClientPort);
        }

        public bool IsUdpAvailable()
        {
            try
            {
                return QueryUdpEchoServer(1194);
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "Failed to check upd availability.");
                return false;
            }
        }

        private static int GetFirstUpdPortAvailable()
        {
            return Enumerable.Range(5000, 6000).First((int p) =>
                IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().All((IPEndPoint l) => l.Port != p));
        }

        private bool QueryUdpEchoServer(int port)
        {
            try
            {
                udpClient.Connect(udpEchoServerUrl, port);
                udpClient.Send(magicNumber, magicNumber.Length);
                return ReceiveMessageSync()?.SequenceEqual(magicNumber) ?? false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private byte[] ReceiveMessageSync()
        {
            IAsyncResult asyncResult = udpClient.BeginReceive(null, null);
            asyncResult.AsyncWaitHandle.WaitOne(2000);
            if (!asyncResult.IsCompleted)
            {
                return null;
            }

            try
            {
                IPEndPoint remoteEP = null;
                return udpClient.EndReceive(asyncResult, ref remoteEP);
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "Failed to receive udp echo package.");
            }

            return null;
        }

        public void Dispose()
        {
            if (udpClient != null)
            {
                try
                {
                    udpClient.Close();
                }
                catch (Exception)
                {
                }
            }
        }
    }
}