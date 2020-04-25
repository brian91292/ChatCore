using ChatCore.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Interfaces
{
    public interface IUserAuthProvider
    {
        event Action<LoginCredentials> OnCredentialsUpdated;

        LoginCredentials Credentials { get; }

        void Save();
    }
}
