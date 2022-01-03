using System;

namespace Avira.Acp.Messages
{
    public static class AcpMessageFormatter
    {
        public static class MaskedValuesNames
        {
            public const string Authorization = "\"Authorization\"";

            public const string AcpOauthKey = "\"X-Avira-Permanent-Connection-Url\"";

            public const string AccessToken = "\"access_token\"";

            public const string RefreshToken = "\"refresh_token\"";

            public const string LoginUrl = "\"login_url\"";
        }

        private const int MinimumValueLength = 30;

        private const int VisibleCharLength = 5;

        public static string RemoveTokenInformation(string message)
        {
            try
            {
                MaskHeader(ref message);
                MaskOauthPayload(ref message);
                return message;
            }
            catch
            {
                return message;
            }
        }

        private static void MaskHeader(ref string serializedMessage)
        {
            MaskJsonValue(ref serializedMessage, "\"Authorization\"", 0);
            MaskJsonValue(ref serializedMessage, "\"X-Avira-Permanent-Connection-Url\"", 0);
        }

        private static void MaskOauthPayload(ref string serializedMessage)
        {
            MaskJsonValue(ref serializedMessage, "\"access_token\"", 0);
            MaskJsonValue(ref serializedMessage, "\"refresh_token\"", 0);
            MaskJsonValue(ref serializedMessage, "\"login_url\"", 0);
        }

        private static void MaskJsonValue(ref string message, string jsonKey, int startIndex)
        {
            int num = message.IndexOf(jsonKey, startIndex, StringComparison.Ordinal);
            if (num < 0)
            {
                return;
            }

            int num2 = message.IndexOf("\"", num + jsonKey.Length, StringComparison.Ordinal);
            if (num2 < 0)
            {
                MaskJsonValue(ref message, jsonKey, num + 1);
                return;
            }

            num2++;
            if (!message.Substring(num, num2 - num).Contains(":"))
            {
                MaskJsonValue(ref message, jsonKey, num + 1);
                return;
            }

            int num3 = message.IndexOf("\"", num2, StringComparison.Ordinal);
            if (num3 < 0)
            {
                MaskJsonValue(ref message, jsonKey, num + 1);
                return;
            }

            int num4 = num3 - num2;
            if (num4 >= 30)
            {
                num4 -= 5;
            }

            message = message.Remove(num2, num4);
            message = message.Insert(num2, "...");
        }
    }
}