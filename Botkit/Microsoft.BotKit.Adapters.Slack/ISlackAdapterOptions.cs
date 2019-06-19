// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.BotKit.Adapters.Slack
{
    /// <summary>
    /// Interface for defining implementation of the SlackAdapter Options.
    /// </summary>
    public interface ISlackAdapterOptions
    {
        /// <summary>
        /// Gets or Sets the token for validating the origin of incoming webhooks.
        /// </summary>
        string VerificationToken { get; set; }

        /// <summary>
        /// Gets or Sets a token used to validate that incoming webhooks originated with Slack.
        /// </summary>
        string ClientSigningSecret { get; set; }

        /// <summary>
        /// Gets or Sets a token (provided by Slack) for a bot to work on a single workspace.
        /// </summary>
        string BotToken { get; set; }

        /// <summary>
        /// Gets or Sets the oauth client id provided by Slack for multi-team apps.
        /// </summary>
        string ClientId { get; set; }

        /// <summary>
        /// Gets or Sets the oauth client secret provided by Slack for multi-team apps.
        /// </summary>
        string ClientSecret { get; set; }

        /// <summary>
        /// Gets or Sets an array of scope names that are being requested during the oauth process. Must match the scopes defined at api.slack.com.
        /// </summary>
        string[] Scopes { get; set; }

        /// <summary>
        /// Gets or Sets the URL users will be redirected to after an oauth flow. In most cases, should be `https://<mydomain.com>/install/auth`.
        /// </summary>
        string RedirectUri { get; set; }

        /// <summary>
        /// A method that receives a Slack team id and returns the bot token associated with that team. Required for multi-team apps.
        /// </summary>
        /// <param name="TeamId">Team ID.</param>
        /// <returns>The bot token associated with the team.</returns>
        Task<string> GetTokenForTeam(string TeamId);

        /// <summary>
        /// A method that receives a Slack team id and returns the bot user id associated with that team. Required for multi-team apps.
        /// </summary>
        /// <param name="TeamId">Team ID.</param>
        /// <returns>The bot user id associated with that team.</returns>
        Task<string> GetBotUserByTeam(string TeamId);
    }
}
