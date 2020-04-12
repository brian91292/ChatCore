using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Config
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class ConfigMeta : System.Attribute
    {
        public string Comment;
        public ConfigMeta()
        {
            Comment = null;
        }
    }
}
