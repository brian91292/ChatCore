using ChatCore.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Interfaces
{
    public interface IWebLoginProvider
    {
        void Start();
        void Stop();
    }
}
