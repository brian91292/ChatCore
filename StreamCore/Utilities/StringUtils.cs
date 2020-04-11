using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore
{
    public static class StringUtils
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
    }
}
