using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using Avira.Acp.Logging;

namespace Avira.Acp.Extensions
{
    public static class PipeStreamExtensions
    {
        private enum ReadOption
        {
            StreamOfMessages,
            StreamOfBytes
        }

        public const int MaxBufferSize = 1048576;

        private static readonly ILogger Logger = LoggerFacade.GetCurrentClassLogger();

        public static void WriteMessage(this PipeStream stream, string message)
        {
            stream.WriteMessage(message, 1048576);
        }

        public static void WriteMessage(this PipeStream stream, string message, int maxBufferSize)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            if (maxBufferSize != 0 && bytes.Length > maxBufferSize)
            {
                throw new IOException("Writing to stream exceeded buffer size limit of " + maxBufferSize + " bytes");
            }

            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }

        public static void WriteTextAsync(this PipeStream stream, string message)
        {
            stream.WriteTextAsync(message, 1048576);
        }

        public static void WriteTextAsync(this PipeStream stream, string message, int maxBufferSize)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            if (maxBufferSize != 0 && bytes.Length > maxBufferSize)
            {
                throw new IOException("Writing to stream exceeded buffer size limit of " + maxBufferSize + " bytes");
            }

            stream.BeginWrite(bytes, 0, bytes.Length, WriteCallback, stream);
        }

        private static void WriteCallback(IAsyncResult asyncResult)
        {
            Stream stream = asyncResult.AsyncState as Stream;
            try
            {
                stream.EndWrite(asyncResult);
                stream.Flush();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex2)
            {
                Logger.Warn("Unhandled exception in the write callback: {0}", ex2);
            }
        }

        public static string ReadText(this PipeStream stream)
        {
            return stream.ReadText(1048576);
        }

        public static string ReadText(this PipeStream stream, int maxBufferSizeInBytes)
        {
            return stream.Read(maxBufferSizeInBytes, ReadOption.StreamOfBytes);
        }

        public static string ReadMessage(this PipeStream stream)
        {
            return stream.Read(1048576, ReadOption.StreamOfMessages);
        }

        public static string ReadMessage(this PipeStream stream, int maxBufferSizeInBytes)
        {
            return stream.Read(maxBufferSizeInBytes, ReadOption.StreamOfMessages);
        }

        private static string Read(this PipeStream stream, int maxBufferSizeInBytes, ReadOption readOption)
        {
            int num = 512;
            byte[] array = new byte[num];
            int num2 = 0;
            bool flag;
            do
            {
                int num3 = stream.Read(array, num2, num - num2);
                num2 += num3;
                flag = ((readOption != 0) ? (num2 == num) : (num3 > 0 && !stream.IsMessageComplete));
                if (flag)
                {
                    int num4 = num * 2;
                    if (maxBufferSizeInBytes != 0 && num4 > maxBufferSizeInBytes)
                    {
                        throw new IOException("Reading from stream exceeded buffer size limit of " +
                                              maxBufferSizeInBytes + " bytes");
                    }

                    array = ResizeBuffer(array, num, num4);
                    num = num4;
                }
            } while (flag);

            return Encoding.UTF8.GetString(array, 0, num2);
        }

        private static byte[] ResizeBuffer(byte[] buffer, int oldBufferSize, int newBufferSize)
        {
            byte[] array = new byte[newBufferSize];
            Buffer.BlockCopy(buffer, 0, array, 0, oldBufferSize);
            return array;
        }
    }
}