using System;
using System.Collections.Generic;

namespace Avira.VpnService
{
    public class LogNotification : OpenVpnNotification
    {
        public enum LogType
        {
            Debug,
            Informational,
            Warning,
            Error,
            Fatal
        }

        private static readonly Dictionary<char, LogType> LogTypeMap = new Dictionary<char, LogType>
        {
            {
                'I',
                LogType.Informational
            },
            {
                'F',
                LogType.Fatal
            },
            {
                'N',
                LogType.Error
            },
            {
                'W',
                LogType.Warning
            },
            {
                'D',
                LogType.Debug
            }
        };

        public LogType Type { get; private set; }

        public LogNotification(string message)
        {
            try
            {
                Dictionary<string, string> dictionary = ParseParameters(message);
                Type = ((!(dictionary["type"] == string.Empty)) ? LogTypeMap[dictionary["type"][0]] : LogType.Debug);
                base.Reason = dictionary["message"];
                base.Timestamp = OpenVpnNotification.UnixTimeToDateTime(dictionary["timestamp"]);
            }
            catch (KeyNotFoundException)
            {
                throw new Exception("[error] Wrong StateType type in StateType real-time notification: " + message);
            }
        }

        private static Dictionary<string, string> ParseParameters(string message)
        {
            string[] array = message.Split(new char[1] { ',' }, 3);
            if (array.Length != 3)
            {
                throw new Exception("[error] Wrong parameters number in real-time notification : " + message);
            }

            if (string.IsNullOrEmpty(array[0]))
            {
                throw new Exception("[error] Wrong format of real-time notification : " + message);
            }

            return new Dictionary<string, string>
            {
                {
                    "timestamp",
                    array[0].Trim()
                },
                {
                    "type",
                    array[1].Trim() ?? string.Empty
                },
                {
                    "message",
                    array[2]?.Trim() ?? string.Empty
                }
            };
        }
    }
}