using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Config
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class ConfigHeader : System.Attribute
    {
        public string[] Comment;
        public ConfigHeader(params string[] comment)
        {
            Comment = comment;
        }
    }
}
