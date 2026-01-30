using System.Threading.Tasks;

namespace Glide.Data.Cards;

public interface ICardRepository
{
    Task<Card> CreateAsync(string title, string boardId, string columnId);
    Task<Card?> GetByIdAsync(string id);
    System.Threading.Tasks.Task UpdateAsync(string id, string title, string? description);
    System.Threading.Tasks.Task MoveToColumnAsync(string id, string columnId, int? position = null);
    System.Threading.Tasks.Task DeleteAsync(string id);
}