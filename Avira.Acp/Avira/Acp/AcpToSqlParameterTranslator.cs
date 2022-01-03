using System;
using System.Globalization;
using System.Linq;
using ServiceStack.Text;

namespace Avira.Acp
{
    public class AcpToSqlParameterTranslator
    {
        public enum SqlDataType
        {
            Number,
            Text,
            TimeStamp
        }

        public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

        public string AcpFieldName { get; set; }

        public string SqlFieldName { get; set; }

        public Func<string, string> Converter { get; set; }

        public AcpToSqlParameterTranslator(string acpFieldName, string sqlFieldName, SqlDataType dataType)
        {
            AcpFieldName = acpFieldName;
            SqlFieldName = sqlFieldName;
            Converter = GetConverter(dataType);
        }

        private static Func<string, string> GetConverter(SqlDataType dataType)
        {
            return delegate(string input)
            {
                Func<string, string> verifier = GetVerifier(dataType);
                return (!input.Contains(","))
                    ? verifier(input)
                    : (from i in input.Split(',')
                        select verifier(i)).Aggregate((string current, string next) => current + "," + next);
            };
        }

        private static Func<string, string> GetVerifier(SqlDataType dataType)
        {
            return dataType switch
            {
                SqlDataType.Number => GetVerifiedNumber,
                SqlDataType.TimeStamp => GetVerifiedTimeStamp,
                _ => GetVerifiedText,
            };
        }

        public static string GetVerifiedNumber(string numberString)
        {
            return long.Parse(numberString).ToString();
        }

        public static string GetVerifiedText(string text)
        {
            return "'" + Escape(text) + "'";
        }

        public static string GetVerifiedTimeStamp(string dateTimeText)
        {
            return DateTime.ParseExact(dateTimeText, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal).ToUnixTime().ToString();
        }

        private static string Escape(string data)
        {
            data = data.Replace("'", "''");
            data = data.Replace("\\", "\\\\");
            return data;
        }
    }
}