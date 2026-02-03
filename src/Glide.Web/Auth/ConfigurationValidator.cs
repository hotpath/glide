using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Configuration;

namespace Glide.Web.Auth;

/// <summary>
///     Validates required configuration on application startup.
///     Ensures all required environment variables are set before the app runs.
/// </summary>
public static class ConfigurationValidator
{
    public static void ValidateRequired(IConfiguration configuration)
    {
        string[] requiredVariables =
        [
            "GLIDE_DATABASE_PATH", "GITHUB_CLIENT_ID", "GITHUB_CLIENT_SECRET", "GITHUB_BASE_URI",
            "GITHUB_REDIRECT_URI"
        ];

        List<string> missingVariables = new();

        foreach (string variable in requiredVariables)
        {
            string? value = configuration[variable];
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