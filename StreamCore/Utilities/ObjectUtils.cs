using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace StreamCore
{
    public static class ObjectUtils
    {
        public static object GetFieldValue(this object obj, string fieldName)
        {
            return obj.GetType().GetField(fieldName).GetValue(obj);
        }
    }
}
