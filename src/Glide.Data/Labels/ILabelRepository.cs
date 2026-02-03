using System.Collections.Generic;
using System.Threading.Tasks;

namespace Glide.Data.Labels;

public interface ILabelRepository
{
    Task<Label> CreateAsync(string boardId, string name, string? icon);
    Task<Label?> GetByIdAsync(string id);
    Task<IEnumerable<Label>> GetByBoardIdAsync(string boardId);
    Task UpdateAsync(string id, string name, string? icon);
    Task DeleteAsync(string id);
    Task AddLabelToCardAsync(string cardId, string labelId);
    Task RemoveLabelFromCardAsync(string cardId, string labelId);
    Task<IEnumerable<Label>> GetLabelsByCardIdAsync(string cardId);
    Task<IEnumerable<string>> GetCardIdsByLabelIdAsync(string labelId);
}
