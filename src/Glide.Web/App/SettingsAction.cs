using System.Security.Claims;
using System.Threading.Tasks;

using Glide.Data.SiteSettings;

using Microsoft.AspNetCore.Http;

namespace Glide.Web.App;

public class SettingsAction(ISiteSettingsRepository siteSettingsRepository)
{
    public async Task<Result<SiteSetting>> GetSettingAsync(string key, ClaimsPrincipal user)
    {
        if (!IsAdmin(user))
        {
            return new Result<SiteSetting>(Results.StatusCode(403));
        }

        SiteSetting? setting = await siteSettingsRepository.GetByKeyAsync(key);
        if (setting is null)
        {
            return new Result<SiteSetting>(Results.NotFound());
        }

        return new Result<SiteSetting>(setting);
    }

    public async Task<Result<SiteSetting>> UpdateSettingAsync(string key, string value, ClaimsPrincipal user)
    {
        if (!IsAdmin(user))
        {
            return new Result<SiteSetting>(Results.StatusCode(403));
        }

        // Validate date_format setting
        if (key == "date_format" && !DateFormatService.IsValidFormat(value))
        {
            return new Result<SiteSetting>(Results.BadRequest("Invalid date format"));
        }

        await siteSettingsRepository.UpdateAsync(key, value);
        SiteSetting? updated = await siteSettingsRepository.GetByKeyAsync(key);

        return updated is null
            ? new Result<SiteSetting>(Results.NotFound())
            : new Result<SiteSetting>(updated);
    }

    private static bool IsAdmin(ClaimsPrincipal user)
    {
        string? isAdminClaim = user.FindFirst("is_admin")?.Value;
        return isAdminClaim == "True";
    }

    public record Result<T>(T? Object, IResult? StatusResult = null)
    {
        public Result(IResult statusResult) : this(default, statusResult) { }

        public bool IsError => StatusResult is not null;
    }
}