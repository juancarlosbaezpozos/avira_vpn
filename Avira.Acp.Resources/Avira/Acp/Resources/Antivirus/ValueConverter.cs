using System;
using System.Globalization;

namespace Avira.Acp.Resources.Antivirus
{
    public static class ValueConverter
    {
        private const string AvDateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

        public static DateTime? DateTimeFromAvDate(string avDate)
        {
            if (avDate != null && DateTime.TryParseExact(avDate, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var result))
            {
                return result;
            }

            return null;
        }

        public static string AvDateFromDateTime(DateTime? dateTime)
        {
            return dateTime?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        }
    }
}