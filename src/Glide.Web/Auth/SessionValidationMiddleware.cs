using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

using Glide.Data.Sessions;

using Microsoft.AspNetCore.Http;

namespace Glide.Web.Auth;

public class SessionValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, SessionRepository sessionRepository)
    {
        string? sessionId = context.Request.Cookies["glide_session"];

        if (!string.IsNullOrEmpty(sessionId))
        {
            SessionUser? session = await sessionRepository.GetAsync(sessionId);

            if (session is not null)
            {
                // Set user identity
                List<Claim> claims =
                [
                    new(ClaimTypes.NameIdentifier, session.UserId),
                    new(ClaimTypes.Email, session.Email),
                    new("SessionId", session.Id)
                ];

                ClaimsIdentity identity = new(claims, "DatabaseSession");
                context.User = new ClaimsPrincipal(identity);
            }
        }

        await next(context);
    }
}