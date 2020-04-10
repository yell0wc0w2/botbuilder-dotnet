using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Rest.TransientFaultHandling;

namespace Microsoft.Bot.Builder
{
    public class ClientManager
    {
        private static readonly HttpClient DefaultHttpClient = new HttpClient();

        // There is a significant boost in throughput if we reuse a connectorClient
        // _connectorClients is a cache using [serviceUrl + appId].
        private readonly ConcurrentDictionary<string, ConnectorClient> _connectorClients = new ConcurrentDictionary<string, ConnectorClient>();

        // Cache for OAuthClient to speed up OAuth operations
        // _oAuthClients is a cache using [appId + oAuthCredentialAppId]
        private readonly ConcurrentDictionary<string, OAuthClient> _oAuthClients = new ConcurrentDictionary<string, OAuthClient>();
        private readonly RetryPolicy _connectorClientRetryPolicy;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Gets the credential provider for this ClientManager.
        /// </summary>
        /// <value>
        /// The credential provider for this ClientManager.
        /// </value>
        public ICredentialProvider CredentialProvider { get; private set; }

        /// <summary>
        /// Gets the channel provider for this ClientManager.
        /// </summary>
        /// <value>
        /// The channel provider for this ClientManager.
        /// </value>
        public IChannelProvider ChannelProvider { get; private set; }

        public HttpClient HttpClient { get => _httpClient; }


        public ClientManager(
            ICredentialProvider credentialProvider,
            IChannelProvider channelProvider = null,
            RetryPolicy connectorClientRetryPolicy = null,
            HttpClient customHttpClient = null)
        {
            CredentialProvider = credentialProvider;
            ChannelProvider = channelProvider;
            _connectorClientRetryPolicy = connectorClientRetryPolicy;
            _httpClient = customHttpClient ?? DefaultHttpClient;

            // DefaultRequestHeaders are not thread safe so set them up here because this adapter should be a singleton.
            ConnectorClient.AddDefaultRequestHeaders(_httpClient);
        }

        /// <summary>
        /// This method returns the correct Bot Framework OAuthScope for AppCredentials.
        /// </summary>
        public string GetBotFrameworkOAuthScope()
        {
            return ChannelProvider != null && ChannelProvider.IsGovernment() ?
                GovernmentAuthenticationConstants.ToChannelFromBotOAuthScope :
                AuthenticationConstants.ToChannelFromBotOAuthScope;
        }

        /// <summary>
        /// Creates an OAuth client for the bot with the credentials.
        /// </summary>
        /// <param name="turnContext">The context object for the current turn.</param>
        /// <param name="oAuthAppCredentials">AppCredentials for OAuth.</param>
        /// <returns>An OAuth client for the bot.</returns>
        public async Task<OAuthClient> CreateOAuthApiClientAsync(ITurnContext turnContext, AppCredentials oAuthAppCredentials)
        {
            if (!OAuthClientConfig.EmulateOAuthCards &&
                string.Equals(turnContext.Activity.ChannelId, Channels.Emulator, StringComparison.InvariantCultureIgnoreCase) &&
                (await CredentialProvider.IsAuthenticationDisabledAsync().ConfigureAwait(false)))
            {
                OAuthClientConfig.EmulateOAuthCards = true;
            }

            var appId = GetBotAppId(turnContext);

            var clientKey = $"{appId}:{oAuthAppCredentials?.MicrosoftAppId}";
            var oAuthScope = GetBotFrameworkOAuthScope();

            var appCredentials = oAuthAppCredentials ?? await GetAppCredentialsAsync(appId, oAuthScope).ConfigureAwait(false);

            if (!OAuthClientConfig.EmulateOAuthCards &&
                string.Equals(turnContext.Activity.ChannelId, Channels.Emulator, StringComparison.InvariantCultureIgnoreCase) &&
                (await CredentialProvider.IsAuthenticationDisabledAsync().ConfigureAwait(false)))
            {
                OAuthClientConfig.EmulateOAuthCards = true;
            }

            var oAuthClient = _oAuthClients.GetOrAdd(clientKey, (key) =>
            {
                OAuthClient oAuthClientInner;
                if (OAuthClientConfig.EmulateOAuthCards)
                {
                    // do not await task - we want this to run in the background
                    oAuthClientInner = new OAuthClient(new Uri(turnContext.Activity.ServiceUrl), appCredentials);
                    var task = Task.Run(() => OAuthClientConfig.SendEmulateOAuthCardsAsync(oAuthClientInner, OAuthClientConfig.EmulateOAuthCards));
                }
                else
                {
                    oAuthClientInner = new OAuthClient(new Uri(OAuthClientConfig.OAuthEndpoint), appCredentials);
                }

                return oAuthClientInner;
            });

            // adding the oAuthClient into the TurnState
            // TokenResolver.cs will use it get the correct credentials to poll for token for streaming scenario
            if (turnContext.TurnState.Get<OAuthClient>() == null)
            {
                turnContext.TurnState.Add(oAuthClient);
            }

            return oAuthClient;
        }

        /// <summary>
        /// Creates the connector client asynchronous.
        /// </summary>
        /// <param name="serviceUrl">The service URL.</param>
        /// <param name="claimsIdentity">The claims claimsIdentity.</param>
        /// <param name="audience">The audience of the token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>ConnectorClient instance.</returns>
        /// <exception cref="NotSupportedException">ClaimsIdentity cannot be null. Pass Anonymous ClaimsIdentity if authentication is turned off.</exception>
        private async Task<IConnectorClient> CreateConnectorClientAsync(string serviceUrl, ClaimsIdentity claimsIdentity, string audience, CancellationToken cancellationToken = default)
        {
            if (claimsIdentity == null)
            {
                throw new NotSupportedException("ClaimsIdentity cannot be null. Pass Anonymous ClaimsIdentity if authentication is turned off.");
            }

            // For requests from channel App Id is in Audience claim of JWT token. For emulator it is in AppId claim. For
            // unauthenticated requests we have anonymous claimsIdentity provided auth is disabled.
            // For Activities coming from Emulator AppId claim contains the Bot's AAD AppId.
            var botAppIdClaim = claimsIdentity.Claims?.SingleOrDefault(claim => claim.Type == AuthenticationConstants.AudienceClaim);
            if (botAppIdClaim == null)
            {
                botAppIdClaim = claimsIdentity.Claims?.SingleOrDefault(claim => claim.Type == AuthenticationConstants.AppIdClaim);
            }

            // For anonymous requests (requests with no header) appId is not set in claims.
            AppCredentials appCredentials = null;
            if (botAppIdClaim != null)
            {
                var botId = botAppIdClaim.Value;
                var scope = audience;

                if (string.IsNullOrWhiteSpace(audience))
                {
                    // The skill connector has the target skill in the OAuthScope.
                    scope = SkillValidation.IsSkillClaim(claimsIdentity.Claims) ?
                        JwtTokenValidation.GetAppIdFromClaims(claimsIdentity.Claims) :
                        GetBotFrameworkOAuthScope();
                }

                appCredentials = await GetAppCredentialsAsync(botId, scope, cancellationToken).ConfigureAwait(false);
            }

            return CreateConnectorClient(serviceUrl, appCredentials);
        }

        /// <summary>
        /// Creates the connector client.
        /// </summary>
        /// <param name="serviceUrl">The service URL.</param>
        /// <param name="appCredentials">The application credentials for the bot.</param>
        /// <returns>Connector client instance.</returns>
        private IConnectorClient CreateConnectorClient(string serviceUrl, AppCredentials appCredentials = null)
        {
            // As multiple bots can listen on a single serviceUrl, the clientKey also includes the OAuthScope.
            var clientKey = $"{serviceUrl}{appCredentials?.MicrosoftAppId}:{appCredentials?.OAuthScope}";

            return _connectorClients.GetOrAdd(clientKey, (key) =>
            {
                ConnectorClient connectorClient;
                if (appCredentials != null)
                {
                    connectorClient = new ConnectorClient(new Uri(serviceUrl), appCredentials, customHttpClient: _httpClient);
                }
                else
                {
                    var emptyCredentials = (ChannelProvider != null && ChannelProvider.IsGovernment()) ?
                        MicrosoftGovernmentAppCredentials.Empty :
                        MicrosoftAppCredentials.Empty;
                    connectorClient = new ConnectorClient(new Uri(serviceUrl), emptyCredentials, customHttpClient: _httpClient);
                }

                if (_connectorClientRetryPolicy != null)
                {
                    connectorClient.SetRetryPolicy(_connectorClientRetryPolicy);
                }

                return connectorClient;
            });
        }
    }
}
