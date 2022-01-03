using System;
using System.Collections.Generic;
using System.Linq;

namespace Avira.VPN.Core.Win
{
    public class PathWhiteList : IWhiteList
    {
        private static readonly List<string> WhiteList = new List<string>
        {
            Environment.SystemDirectory,
            AppDomain.CurrentDomain.BaseDirectory
        };

        public bool IsWhiteListed(string path)
        {
            return WhiteList.Any(path.StartsWith);
        }
    }
}