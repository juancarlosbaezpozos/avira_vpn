using System;
using System.Collections.Generic;

namespace Avira.VPN.Core.Win
{
    public class SaRating
    {
        internal class SaData
        {
            public int Factor { get; set; }

            public Func<bool> Calculator { get; set; }
        }

        internal const string EncS =
            "Ti427A9gTujfgUl8YycZ3+jCIhn8tU6Kd/n7mRnz+J9ekrEvdBR2ZNvUwOM27VMsSWQoHKRDT/0ONuizn5zGl1VWoNZj+b2yAXhUZtNT0tu89oSI3NjPNRYRw0KpZFNGAtvn3urTTOsRSRTjp4H3SmTuHGrfVTlFE3oPcTkJ5Ma7E8x0aq1jvmZS0kv5l3R+5nsJkLEsVzGgMb7Qlkq6Z+XIv4XisAItVvvADVslJdikBZwse/raLTu3CC3JpHX2PE72icOlwqpJ/xzTT5+ZdHHbgliN4SWXOsVCzOf+J7zP87VHZzEJEcRkjgSQ7O12XKvzLQw4vewsqM4Ox6Wf6tN/lf4Td5u40wV5RRJ+Jbf6e9FguUE8/NL/nz9TO/lIr5BYbhMNzFyMYzepGsbS8ztX7VwR0gXPBSZYUQOgijmMm+YzF9cnb542oMdtqn0Z11TnwRz7VbH12WLvfI4WozIgOUCV8tQvoeclrRneAWszs7UNYy0XPYV1vo6kgHIYcvFqu/qhVCUrjtxGZ1HDt5bKL3mj+M611KfTmhn2JeQb5eGqLXn46ILH+d3gzml2IxBcUH/pqf0u05czi7GgnSY/2mV40EZb5yH1lTV5q9VQZVBe99dzm/B2M5tLYEopQTIw6eAHLKxBp0rAc/NpI8VqTlGLIBy+E3LAKqNLBWsx5bGgd9UA4X0zAKxXaabJHh/DmL+woYApwjqNX9tpQZa44+55oDinsfeorZoSValTeOCVC5xGyLWV+j8FoO/ptlfxyE2oEWmiU5dLOjV5p83b35y6uN67Bqvan9oagIOTxhmviVTcRRf2dAG7YEtg";

        internal const string SecOpinScan =
            "Ti427A9gTujfgUl8YycZ3+jCIhn8tU6Kd/n7mRnz+J9ekrEvdBR2ZNvUwOM27VMsSWQoHKRDT/0ONuizn5zGl1VWoNZj+b2yAXhUZtNT0tu89oSI3NjPNRYRw0KpZFNGAtvn3urTTOsRSRTjp4H3SmTuHGrfVTlFE3oPcTkJ5Ma7E8x0aq1jvmZS0kv5l3R+5nsJkLEsVzGgMb7Qlkq6Z+XIv4XisAItVvvADVslJdikBZwse/raLTu3CC3JpHX2PE72icOlwqpJ/xzTT5+ZdHHbgliN4SWXOsVCzOf+J7zP87VHZzEJEcRkjgSQ7O12XKvzLQw4vewsqM4Ox6Wf6tN/lf4Td5u40wV5RRJ+Jbf6e9FguUE8/NL/nz9TO/lIr5BYbhMNzFyMYzepGsbS8ztX7VwR0gXPBSZYUQOgijlT1ELtM4AquC3BiHjjjXT7ih5ElOhumJhpe4iJn81H4uRKfdwtGj3CrkFTCQAtwTpw6GgkJroOhLbYjhGQFQS1qWhewX7/kuakF+Of/Y34Nrq2jbtfqweWF5+/atLKizU=";

        private readonly List<SaData> saData = new List<SaData>
        {
            new SaData
            {
                Factor = 10,
                Calculator = () => new BrowserExtensions().ArePasswordManagersInstalled()
            },
            new SaData
            {
                Factor = 10,
                Calculator = () => SoftwareChecker.IsInstalled(SensitiveDataEncryptor.DecryptAppsList(
                    "Ti427A9gTujfgUl8YycZ3+jCIhn8tU6Kd/n7mRnz+J9ekrEvdBR2ZNvUwOM27VMsSWQoHKRDT/0ONuizn5zGl1VWoNZj+b2yAXhUZtNT0tu89oSI3NjPNRYRw0KpZFNGAtvn3urTTOsRSRTjp4H3SmTuHGrfVTlFE3oPcTkJ5Ma7E8x0aq1jvmZS0kv5l3R+5nsJkLEsVzGgMb7Qlkq6Z+XIv4XisAItVvvADVslJdikBZwse/raLTu3CC3JpHX2PE72icOlwqpJ/xzTT5+ZdHHbgliN4SWXOsVCzOf+J7zP87VHZzEJEcRkjgSQ7O12XKvzLQw4vewsqM4Ox6Wf6tN/lf4Td5u40wV5RRJ+Jbf6e9FguUE8/NL/nz9TO/lIr5BYbhMNzFyMYzepGsbS8ztX7VwR0gXPBSZYUQOgijmMm+YzF9cnb542oMdtqn0Z11TnwRz7VbH12WLvfI4WozIgOUCV8tQvoeclrRneAWszs7UNYy0XPYV1vo6kgHIYcvFqu/qhVCUrjtxGZ1HDt5bKL3mj+M611KfTmhn2JeQb5eGqLXn46ILH+d3gzml2IxBcUH/pqf0u05czi7GgnSY/2mV40EZb5yH1lTV5q9VQZVBe99dzm/B2M5tLYEopQTIw6eAHLKxBp0rAc/NpI8VqTlGLIBy+E3LAKqNLBWsx5bGgd9UA4X0zAKxXaabJHh/DmL+woYApwjqNX9tpQZa44+55oDinsfeorZoSValTeOCVC5xGyLWV+j8FoO/ptlfxyE2oEWmiU5dLOjV5p83b35y6uN67Bqvan9oagIOTxhmviVTcRRf2dAG7YEtg"))
            },
            new SaData
            {
                Factor = 20,
                Calculator = () => SoftwareChecker.IsInstalled(SensitiveDataEncryptor.DecryptAppsList(
                    "Ti427A9gTujfgUl8YycZ3+jCIhn8tU6Kd/n7mRnz+J9ekrEvdBR2ZNvUwOM27VMsSWQoHKRDT/0ONuizn5zGl1VWoNZj+b2yAXhUZtNT0tu89oSI3NjPNRYRw0KpZFNGAtvn3urTTOsRSRTjp4H3SmTuHGrfVTlFE3oPcTkJ5Ma7E8x0aq1jvmZS0kv5l3R+5nsJkLEsVzGgMb7Qlkq6Z+XIv4XisAItVvvADVslJdikBZwse/raLTu3CC3JpHX2PE72icOlwqpJ/xzTT5+ZdHHbgliN4SWXOsVCzOf+J7zP87VHZzEJEcRkjgSQ7O12XKvzLQw4vewsqM4Ox6Wf6tN/lf4Td5u40wV5RRJ+Jbf6e9FguUE8/NL/nz9TO/lIr5BYbhMNzFyMYzepGsbS8ztX7VwR0gXPBSZYUQOgijlT1ELtM4AquC3BiHjjjXT7ih5ElOhumJhpe4iJn81H4uRKfdwtGj3CrkFTCQAtwTpw6GgkJroOhLbYjhGQFQS1qWhewX7/kuakF+Of/Y34Nrq2jbtfqweWF5+/atLKizU="))
            },
            new SaData
            {
                Factor = 20,
                Calculator = () => LoggedOnUser.CurrentUserIsNotAdmin()
            },
            new SaData
            {
                Factor = 20,
                Calculator = () => BitLockerInfo.IsActive()
            },
            new SaData
            {
                Factor = 10,
                Calculator = () => SoftwareChecker.IsAntivirusInstalled()
            }
        };

        public int CalculateRating()
        {
            return CalculateRating(saData);
        }

        internal int CalculateRating(List<SaData> saData)
        {
            int num = 0;
            int num2 = 0;
            foreach (SaData data in saData)
            {
                bool flag = Catch.All(() => data.Calculator(), defaultValue: false);
                num2 += data.Factor;
                num += (flag ? data.Factor : 0);
            }

            return num * 100 / num2;
        }
    }
}