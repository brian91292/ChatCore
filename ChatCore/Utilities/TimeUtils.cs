using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore
{
    public static class TimeUtils
    {
        public static string ToShortString(this TimeSpan span)
        {
            return string.Format("{0:00}:{1:00}:{2:00}", Math.Floor(span.TotalHours), span.Minutes, span.Seconds);
        }
    }
}
