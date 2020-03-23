using StreamCore.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IUserAuthManager
    {
        event Action<LoginCredentials> OnCredentialsUpdated;

        LoginCredentials Credentials { get; set; }
    }
}
