using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace StreamCore.Utilities
{
    public static class ObjectUtils
    {
        public static object GetField(this object obj, string fieldName)
        {
            return obj.GetType().GetField(fieldName).GetValue(obj);
        }
    }
}
