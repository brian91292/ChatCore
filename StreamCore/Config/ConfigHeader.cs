using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Config
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class ConfigHeader : System.Attribute
    {
        public string Comment;
        public ConfigHeader(string comment)
        {
            Comment = comment;
        }
    }
}
