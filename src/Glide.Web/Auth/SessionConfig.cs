using System;
using Microsoft.Extensions.Configuration;

namespace Glide.Web.Auth;

public record SessionConfig(long DurationSeconds)
{
    public static SessionConfig FromConfiguration(IConfiguration configuration)
    {
        long durationHours = configuration.GetValue<long>("SESSION_DURATION_HOURS");
        if (durationHours <= 0)
        {
            durationHours = 720; // Default to 30 days
        }

        long durationSeconds = (long)TimeSpan.FromHours(durationHours).TotalSeconds;
        return new SessionConfig(durationSeconds);
    }
}
