namespace Avira.VPN.Core.Win
{
    public static class DarRating
    {
        internal const string DarData =
            "Ti427A9gTujfgUl8YycZ3+jCIhn8tU6Kd/n7mRnz+J9ekrEvdBR2ZNvUwOM27VMsSWQoHKRDT/0ONuizn5zGl1VWoNZj+b2yAXhUZtNT0tu89oSI3NjPNRYRw0KpZFNGAtvn3urTTOsRSRTjp4H3SmTuHGrfVTlFE3oPcTkJ5Ma7E8x0aq1jvmZS0kv5l3R+5nsJkLEsVzGgMb7Qlkq6Z+XIv4XisAItVvvADVslJdikBZwse/raLTu3CC3JpHX2PE72icOlwqpJ/xzTT5+ZdHHbgliN4SWXOsVCzOf+J7zP87VHZzEJEcRkjgSQ7O12XKvzLQw4vewsqM4Ox6Wf6tN/lf4Td5u40wV5RRJ+Jbf6e9FguUE8/NL/nz9TO/lIr5BYbhMNzFyMYzepGsbS8ztX7VwR0gXPBSZYUQOgijkKL5W2WSBTGz1+TbvpusTyAeDdARB+jI1iPXp3Ym4s9sa963fpUlS2fn1ao9NQxnrolaQa93e53Dqph1lqkSgAyP0Lrg2gxqDUkVN1JhJgE3zv9825wkhgKd1eXshmIqXFO6ZD1ymESXN4l1MBanlU5hlgsz2WTesZAgD3xYWPS06Do1ZB6NkLDkJf3Xmo0hXxpK4ipr4dAg3xyA/EIbxnoGBM0pJAuZ/Z46a5K1y5TYGg6aV/CQObgAINLEbLg0zl1EdD3oqQIyPX6xKRP9YPrrZ4YIhMJTFMKW0l9q6CPQ+CkxItWqCp1l+EFqMu/AjsbUAk6NUXlkP0y2qrcDeOklYPPhD3djoCpaOBNRMDhsBdPg5wN1vvMex/fCSfLRZoqNuD6hSw03HHi22or2c2WDaOnNpbSbfAeioQLLxV2Faz1BpwYj9Jo05VI5IXBrE1Pcsf2xdidwn5Dz2r4TOsUaqAyYE68qzschR4T2NdAgfkgIGOQyQcPCpO+PyezqIPl3ddwmpyz7v3Oi60InflObyzTcmLAUzmFRgeeDNYBJwH6xD+XAYJflzEgr7ccdn0qh+gweU1Sf7VjpA/jvKv4RrNrrCgxdziivw4s9HedNm2LIiCaThQ3TwgfzvPXyRvCI7eqDjEpqGRnvHdcmq+tFi/MRKbMJFPJB6kW8k4IUMZW9XWkdOKWsb7FHOtUsJQitBtpvxRBeZBI9eVUIJiIsAw9iNAXif69uIQXjV5LMCBVzDY3c5bujsLHOmo3s/u0cIlfriplH2TYa6qnFVisxRPLhSeQJgPP0PsEqkM56ThzGofcqBC78gxg95Dn8H8JUsZ7FnhgxiEsSypFbrcl0fGySnq23o87GjewMryM8OSNPNK3Ka9Wtk9PDrN+yQ=";

        public static int CalculateRating()
        {
            if (!SoftwareChecker.IsInstalled(SensitiveDataEncryptor.DecryptAppsList(
                    "Ti427A9gTujfgUl8YycZ3+jCIhn8tU6Kd/n7mRnz+J9ekrEvdBR2ZNvUwOM27VMsSWQoHKRDT/0ONuizn5zGl1VWoNZj+b2yAXhUZtNT0tu89oSI3NjPNRYRw0KpZFNGAtvn3urTTOsRSRTjp4H3SmTuHGrfVTlFE3oPcTkJ5Ma7E8x0aq1jvmZS0kv5l3R+5nsJkLEsVzGgMb7Qlkq6Z+XIv4XisAItVvvADVslJdikBZwse/raLTu3CC3JpHX2PE72icOlwqpJ/xzTT5+ZdHHbgliN4SWXOsVCzOf+J7zP87VHZzEJEcRkjgSQ7O12XKvzLQw4vewsqM4Ox6Wf6tN/lf4Td5u40wV5RRJ+Jbf6e9FguUE8/NL/nz9TO/lIr5BYbhMNzFyMYzepGsbS8ztX7VwR0gXPBSZYUQOgijkKL5W2WSBTGz1+TbvpusTyAeDdARB+jI1iPXp3Ym4s9sa963fpUlS2fn1ao9NQxnrolaQa93e53Dqph1lqkSgAyP0Lrg2gxqDUkVN1JhJgE3zv9825wkhgKd1eXshmIqXFO6ZD1ymESXN4l1MBanlU5hlgsz2WTesZAgD3xYWPS06Do1ZB6NkLDkJf3Xmo0hXxpK4ipr4dAg3xyA/EIbxnoGBM0pJAuZ/Z46a5K1y5TYGg6aV/CQObgAINLEbLg0zl1EdD3oqQIyPX6xKRP9YPrrZ4YIhMJTFMKW0l9q6CPQ+CkxItWqCp1l+EFqMu/AjsbUAk6NUXlkP0y2qrcDeOklYPPhD3djoCpaOBNRMDhsBdPg5wN1vvMex/fCSfLRZoqNuD6hSw03HHi22or2c2WDaOnNpbSbfAeioQLLxV2Faz1BpwYj9Jo05VI5IXBrE1Pcsf2xdidwn5Dz2r4TOsUaqAyYE68qzschR4T2NdAgfkgIGOQyQcPCpO+PyezqIPl3ddwmpyz7v3Oi60InflObyzTcmLAUzmFRgeeDNYBJwH6xD+XAYJflzEgr7ccdn0qh+gweU1Sf7VjpA/jvKv4RrNrrCgxdziivw4s9HedNm2LIiCaThQ3TwgfzvPXyRvCI7eqDjEpqGRnvHdcmq+tFi/MRKbMJFPJB6kW8k4IUMZW9XWkdOKWsb7FHOtUsJQitBtpvxRBeZBI9eVUIJiIsAw9iNAXif69uIQXjV5LMCBVzDY3c5bujsLHOmo3s/u0cIlfriplH2TYa6qnFVisxRPLhSeQJgPP0PsEqkM56ThzGofcqBC78gxg95Dn8H8JUsZ7FnhgxiEsSypFbrcl0fGySnq23o87GjewMryM8OSNPNK3Ka9Wtk9PDrN+yQ=")))
            {
                return 0;
            }

            return 100;
        }
    }
}