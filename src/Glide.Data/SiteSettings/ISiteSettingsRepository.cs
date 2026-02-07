using System.Threading.Tasks;

namespace Glide.Data.SiteSettings;

public interface ISiteSettingsRepository
{
    Task<SiteSetting?> GetByKeyAsync(string key);
    Task UpdateAsync(string key, string value);
}