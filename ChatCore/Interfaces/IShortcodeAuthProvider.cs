using ChatCore.Models.OAuth;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatCore.Interfaces
{
    public interface IShortcodeAuthProvider
    {
        Task<OAuthShortcodeRequest> RequestShortcode();
        Task<OAuthCredentials> AwaitUserApproval(CancellationToken cancellationToken, OAuthShortcodeRequest request = null, bool launchBrowserProcess = false);
    }
}
