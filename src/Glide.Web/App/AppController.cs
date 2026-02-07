using System;
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
        SettingsAction.Result<SiteSetting> registrationResult =
            await settingsAction.GetSettingAsync("registration_open", User);
        SettingsAction.Result<SiteSetting> dateFormatResult =
            await settingsAction.GetSettingAsync("date_format", User);

        bool isAdmin = User.FindFirst("is_admin")?.Value == "True";
        bool isRegistrationOpen = registrationResult.Object?.Value == "true";
        string dateFormat = dateFormatResult.Object?.Value ?? "yyyy-MM-dd";

        return new RazorComponentResult<SettingsContent>(new
        {
            IsAdmin = isAdmin,
            IsRegistrationOpen = isRegistrationOpen,
            DateFormat = dateFormat
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

        // Get date format for the response
        SettingsAction.Result<SiteSetting> dateFormatResult =
            await settingsAction.GetSettingAsync("date_format", User);
        string dateFormat = dateFormatResult.Object?.Value ?? "yyyy-MM-dd";

        bool isRegistrationOpen = updateResult.Object!.Value == "true";
        return new RazorComponentResult<SettingsContent>(
            new { IsAdmin = true, IsRegistrationOpen = isRegistrationOpen, DateFormat = dateFormat });
    }

    [HttpPost("/settings/update-date-format")]
    [Authorize]
    public async Task<IResult> UpdateDateFormat([FromForm(Name = "date_format")] string dateFormat)
    {
        SettingsAction.Result<SiteSetting> updateResult =
            await settingsAction.UpdateSettingAsync("date_format", dateFormat, User);

        if (updateResult.IsError)
        {
            return updateResult.StatusResult!;
        }

        // Get registration status for the response
        SettingsAction.Result<SiteSetting> registrationResult =
            await settingsAction.GetSettingAsync("registration_open", User);
        bool isRegistrationOpen = registrationResult.Object?.Value == "true";

        return new RazorComponentResult<SettingsContent>(
            new { IsAdmin = true, IsRegistrationOpen = isRegistrationOpen, DateFormat = updateResult.Object!.Value });
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

    [HttpGet("/settings/preview-date-format")]
    [Authorize]
    public IResult PreviewDateFormat([FromQuery] string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return Results.Text("Invalid format");
        }

        if (!DateFormatService.IsValidFormat(format))
        {
            return Results.Text("Invalid format");
        }

        try
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            string formatted = today.ToString(format);
            return Results.Text(formatted);
        }
        catch
        {
            return Results.Text("Invalid format");
        }
    }
}