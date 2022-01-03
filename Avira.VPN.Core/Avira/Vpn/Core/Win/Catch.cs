using System;
using Serilog;

namespace Avira.VPN.Core.Win
{
    public static class Catch
    {
        public static void All(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Action failed: " + action.ToString() + ".");
            }
        }

        public static T All<T>(Func<T> action, T defaultValue)
        {
            try
            {
                return action();
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Action failed: " + action.ToString() + ".");
                return defaultValue;
            }
        }
    }
}