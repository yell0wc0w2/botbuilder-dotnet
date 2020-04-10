using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Microsoft.Bot.Connector.Authentication;

namespace Microsoft.Bot.Builder
{
    public class AppCredentialManager
    {
        // Cache for appCredentials to speed up token acquisition (a token is not requested unless is expired)
        // AppCredentials are cached using appId + skillId (this last parameter is only used if the app credentials are used to call a skill)
        private readonly ConcurrentDictionary<string, AppCredentials> _appCredentialMap = new ConcurrentDictionary<string, AppCredentials>();

        public ConcurrentDictionary<string, AppCredentials> AppCredentialMap { get => _appCredentialMap; }

    }
}
