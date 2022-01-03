using System;
using System.IO;
using System.Text;

namespace Avira.Win.Messaging
{
    public class MessageStream
    {
        public const string EndOfStream = "E5661997-FE27-4181-93D5-8EE30FED087E";

        private Stream stream;

        public MessageStream(Stream stream)
        {
            this.stream = stream;
        }

        public void WriteMessage(string message)
        {
            int byteCount = Encoding.UTF8.GetByteCount(message);
            byte[] array = new byte[byteCount + 2];
            Encoding.UTF8.GetBytes(message, 0, message.Length, array, 2);
            int num = byteCount;
            if (num > 65535)
            {
                throw new ArgumentOutOfRangeException();
            }

            array[0] = (byte)(num / 256);
            array[1] = (byte)((uint)num & 0xFFu);
            stream.Write(array, 0, array.Length);
            stream.Flush();
        }

        public string ReadMessage()
        {
            int num = stream.ReadByte();
            if (num == -1)
            {
                return "E5661997-FE27-4181-93D5-8EE30FED087E";
            }

            num *= 256;
            num += stream.ReadByte();
            byte[] array = new byte[num];
            stream.Read(array, 0, num);
            return Encoding.UTF8.GetString(array);
        }
    }
}