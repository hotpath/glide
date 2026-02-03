using System.Collections.Generic;
using System.Threading.Tasks;

namespace Glide.Data.Boards;

public interface IBoardRepository
{
    Task<Board> CreateAsync(string name, string userId);
    Task<IEnumerable<Board>> GetByUserIdAsync(string userId);
    Task<Board?> GetByIdAsync(string boardId);
    Task<Board?> UpdateAsync(string id, string name);
    Task DeleteAsync(string boardId);
    Task<IEnumerable<BoardUser>> GetBoardUsersAsync(string boardId);
    Task<IEnumerable<BoardMemberDetails>> GetBoardMembersWithEmailAsync(string boardId);
    Task AddUserToBoardAsync(string boardId, string userId, bool isOwner);
    Task UpdateUserRoleAsync(string boardId, string userId, bool isOwner);
    Task RemoveUserFromBoardAsync(string boardId, string userId);
}