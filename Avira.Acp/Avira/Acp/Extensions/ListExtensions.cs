using System;
using System.Collections.Generic;

namespace Avira.Acp.Extensions
{
    public static class ListExtensions
    {
        public static List<T> MoveAll<T>(this List<T> list, Predicate<T> predicate)
        {
            List<T> list2 = new List<T>();
            for (int num = list.Count - 1; num >= 0; num--)
            {
                T val = list[num];
                if (predicate(val))
                {
                    list2.Add(val);
                    list.RemoveAt(num);
                }
            }

            return list2;
        }
    }
}