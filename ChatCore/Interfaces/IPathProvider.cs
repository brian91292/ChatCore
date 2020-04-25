using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Interfaces
{
    public interface IPathProvider
    {
        string GetDataPath();
        string GetResourcePath();
    }
}
