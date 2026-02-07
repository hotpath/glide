using System.Threading.Tasks;

using Glide.Data.SiteSettings;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Glide.Web.App;

[ApiController]
public class AppController(SettingsAction settingsAction) : ControllerBase
{
    [HttpGet("/settings")]
    [Authorize]
    public IResult GetSettings()
    {
        return new RazorComponentResult<Settings>();
    }

    [HttpGet("/settings/content")]
    [Authorize]
    public async Task<IResult> GetSettingsContent()
    {
        SettingsAction.Result<SiteSetting> result = await settingsAction.GetSettingAsync("registration_open", User);
        bool isAdmin = User.FindFirst("is_admin")?.Value == "True";
        bool isRegistrationOpen = result.Object?.Value == "true";

        return new RazorComponentResult<SettingsContent>(new
        {
            IsAdmin = isAdmin,
            IsRegistrationOpen = isRegistrationOpen
        });
    }

    [HttpPost("/settings/toggle-registration")]
    [Authorize]
    public async Task<IResult> ToggleRegistration()
    {
        SettingsAction.Result<SiteSetting> currentResult =
            await settingsAction.GetSettingAsync("registration_open", User);
        if (currentResult.IsError)
        {
            return currentResult.StatusResult!;
        }

        string newValue = currentResult.Object!.Value == "true" ? "false" : "true";
        SettingsAction.Result<SiteSetting> updateResult =
            await settingsAction.UpdateSettingAsync("registration_open", newValue, User);

        if (updateResult.IsError)
        {
            return updateResult.StatusResult!;
        }

        bool isRegistrationOpen = updateResult.Object!.Value == "true";
        return new RazorComponentResult<SettingsContent>(
            new { IsAdmin = true, IsRegistrationOpen = isRegistrationOpen });
    }

    [HttpGet("/about")]
    [Authorize]
    public Task<IResult> GetAbout()
    {
        return Task.FromResult<IResult>(new RazorComponentResult<About>());
    }

    [HttpGet("/profile")]
    [Authorize]
    public Task<IResult> GetProfile()
    {
        return Task.FromResult<IResult>(new RazorComponentResult<Profile>());
    }
}