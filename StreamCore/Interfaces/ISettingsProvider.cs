using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface ISettingsProvider
    {
        bool RunWebApp { get; set; }

        void Save();
    }
}
