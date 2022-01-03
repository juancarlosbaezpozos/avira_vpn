using System;

namespace Avira.VPN.Core.Win
{
    public class Ratings
    {
        private struct CalculatedValue
        {
            private DateTime lastCalculated;

            private int value;

            private Func<int> calculate;

            public CalculatedValue(Func<int> calculate)
            {
                lastCalculated = DateTime.MinValue;
                value = 0;
                this.calculate = calculate;
            }

            public int Get()
            {
                if (lastCalculated.Year < DateTime.Now.Year && lastCalculated.DayOfYear < DateTime.Now.DayOfYear)
                {
                    value = calculate();
                    lastCalculated = DateTime.Now;
                }

                return value;
            }
        }

        private CalculatedValue securityAfinityRating = new CalculatedValue(() => new SaRating().CalculateRating());

        private CalculatedValue downloadAfinityRating = new CalculatedValue(() => DarRating.CalculateRating());

        public int SecurityAfinityRating => securityAfinityRating.Get();

        public int DownloadAfinityRating => downloadAfinityRating.Get();
    }
}