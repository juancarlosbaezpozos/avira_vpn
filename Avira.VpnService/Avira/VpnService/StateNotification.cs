using System;
using System.Collections.Generic;

namespace Avira.VpnService
{
    public class StateNotification : OpenVpnNotification
    {
        public enum Type
        {
            Connected,
            Reconnecting,
            Exiting,
            Connecting,
            Wait,
            Auth,
            GetConfig,
            AssignIp,
            AddRoutes,
            Resolve,
            TcpConnect
        }

        private static readonly Dictionary<string, Type> StatesMap = new Dictionary<string, Type>
        {
            {
                "CONNECTING",
                Type.Connecting
            },
            {
                "WAIT",
                Type.Wait
            },
            {
                "AUTH",
                Type.Auth
            },
            {
                "GET_CONFIG",
                Type.GetConfig
            },
            {
                "ASSIGN_IP",
                Type.AssignIp
            },
            {
                "ADD_ROUTES",
                Type.AssignIp
            },
            {
                "CONNECTED",
                Type.Connected
            },
            {
                "RECONNECTING",
                Type.Reconnecting
            },
            {
                "EXITING",
                Type.Exiting
            },
            {
                "RESOLVE",
                Type.Resolve
            },
            {
                "TCP_CONNECT",
                Type.TcpConnect
            }
        };

        public string LocalAddress { get; protected set; }

        public string RemoteAddress { get; protected set; }

        public Type StateType { get; protected set; }

        public StateNotification(string message)
        {
            try
            {
                Dictionary<string, string> dictionary = ParseParameters(message);
                base.Timestamp = OpenVpnNotification.UnixTimeToDateTime(dictionary["timestamp"]);
                StateType = StatesMap[dictionary["state"]];
                base.Reason = dictionary["reason"];
                LocalAddress = dictionary["local"];
                RemoteAddress = dictionary["remote"];
            }
            catch (KeyNotFoundException)
            {
                throw new Exception("Wrong StateType type in StateType real-time notification: " + message);
            }
        }

        private static Dictionary<string, string> ParseParameters(string message)
        {
            string[] array = message.Split(',');
            if (array.Length < 5)
            {
                throw new Exception("Wrong parameters number in StateType real-time notification : " + message);
            }

            if (string.IsNullOrEmpty(array[0]) || string.IsNullOrEmpty(array[1]))
            {
                throw new Exception("Wrong format of real-time notification : " + message);
            }

            return new Dictionary<string, string>
            {
                {
                    "timestamp",
                    array[0].Trim()
                },
                {
                    "state",
                    array[1].Trim()
                },
                {
                    "reason",
                    array[2] ?? string.Empty
                },
                {
                    "local",
                    array[3] ?? string.Empty
                },
                {
                    "remote",
                    array[4] ?? string.Empty
                }
            };
        }

        public override string ToString()
        {
            return
                $"[{base.Timestamp.ToLongDateString()}] {GetType()} ({StateType}, {LocalAddress} => {RemoteAddress}) : {base.Reason}";
        }
    }
}