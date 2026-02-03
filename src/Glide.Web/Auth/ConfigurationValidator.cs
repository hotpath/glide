namespace Glide.Web.Auth;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Validates required configuration on application startup.
/// Ensures all required environment variables are set before the app runs.
/// </summary>
public static class ConfigurationValidator
{
    public static void ValidateRequired(IConfiguration configuration)
    {
        var requiredVariables = new[]
        {
            "GLIDE_DATABASE_PATH",
            "OAUTH_CLIENT_ID",
            "OAUTH_CLIENT_SECRET",
            "OAUTH_AUTHORIZE_URL",
            "OAUTH_TOKEN_URL",
            "OAUTH_USER_INFO_URL",
            "OAUTH_REDIRECT_URI"
        };

        var missingVariables = new List<string>();

        foreach (var variable in requiredVariables)
        {
            var value = configuration[variable];
            if (string.IsNullOrWhiteSpace(value))
            {
                missingVariables.Add(variable);
            }
        }

        if (missingVariables.Any())
        {
            throw new InvalidOperationException(
                $"Missing required configuration variables:\n  - {string.Join("\n  - ", missingVariables)}\n\n" +
                $"Create a .env file with these variables. See .env.example for template.");
        }
    }
}
