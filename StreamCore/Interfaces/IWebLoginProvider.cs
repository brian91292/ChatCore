using StreamCore.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IWebLoginProvider
    {
        void Start();
        void Stop();
    }
}
