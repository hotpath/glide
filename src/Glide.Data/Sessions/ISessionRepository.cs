using System.Threading.Tasks;

namespace Glide.Data.Sessions;

public interface ISessionRepository
{
    Task<Session> CreateAsync(string userId, long durationSeconds);
    Task<SessionUser?> GetAsync(string sessionId);
    Task DeleteAsync(string sessionId);
}