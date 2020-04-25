using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Config
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class ConfigSection : System.Attribute
    {
        public string Name;
        public ConfigSection(string name)
        {
            Name = name;
        }
    }
}
