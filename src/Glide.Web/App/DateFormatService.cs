using System;
using System.Threading;
using System.Threading.Tasks;

using Glide.Data.SiteSettings;

using Microsoft.AspNetCore.Http;

namespace Glide.Web.App;

public class DateFormatService(ISiteSettingsRepository siteSettingsRepository, IHttpContextAccessor httpContextAccessor)
{
    private const string DefaultFormat = "yyyy-MM-dd";
    private const string CacheKey = "DateFormat";

    public async Task<string> GetDateFormatAsync()
    {
        SiteSetting? setting = await siteSettingsRepository.GetByKeyAsync("date_format");
        return setting?.Value ?? DefaultFormat;
    }

    public string GetDateFormat()
    {
        try
        {
            // Try to get from HTTP context items cache
            if (httpContextAccessor.HttpContext?.Items.ContainsKey(CacheKey) == true)
            {
                return httpContextAccessor.HttpContext.Items[CacheKey] as string ?? DefaultFormat;
            }

            // Fetch from database synchronously (not ideal but necessary for Razor rendering)
            string format = GetDateFormatAsync().GetAwaiter().GetResult();

            // Cache in HTTP context for this request
            if (httpContextAccessor.HttpContext != null)
            {
                httpContextAccessor.HttpContext.Items[CacheKey] = format;
            }

            return format;
        }
        catch (Exception)
        {
            // Return default format if anything goes wrong
            return DefaultFormat;
        }
    }

    public string FormatDate(DateOnly date, string format)
    {
        try
        {
            return date.ToString(format);
        }
        catch
        {
            return date.ToString(DefaultFormat);
        }
    }

    public string FormatDate(DateOnly date)
    {
        string format = GetDateFormat();
        return FormatDate(date, format);
    }

    public async Task<string> FormatDateAsync(DateOnly date)
    {
        string format = await GetDateFormatAsync();
        return FormatDate(date, format);
    }

    public static bool IsValidFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return false;
        }

        try
        {
            // Test the format with a sample date
            DateOnly testDate = new DateOnly(2024, 12, 31);
            _ = testDate.ToString(format);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
