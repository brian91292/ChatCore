using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Config
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class HTMLIgnore : System.Attribute
    {
        public HTMLIgnore()
        {
        }
    }
}
