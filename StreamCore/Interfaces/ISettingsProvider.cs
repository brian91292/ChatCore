using StreamCore.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface ISettingsProvider
    {
        bool RunWebApp { get; set; }
        int WebAppPort { get; set; }

        void Save();
    }
}
