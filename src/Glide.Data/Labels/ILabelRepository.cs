using System.Collections.Generic;
using System.Threading.Tasks;

namespace Glide.Data.Labels;

public interface ILabelRepository
{
    Task<Label> CreateAsync(string boardId, string name);
    Task<Label?> GetByIdAsync(string id);
    Task<Label?> GetByBoardIdAndNameAsync(string boardId, string name);
    Task<IEnumerable<Label>> GetByBoardIdAsync(string boardId);
    Task UpdateAsync(string id, string name);
    Task DeleteAsync(string id);
    Task AddLabelToCardAsync(string cardId, string labelId);
    Task RemoveLabelFromCardAsync(string cardId, string labelId);
    Task<IEnumerable<Label>> GetLabelsByCardIdAsync(string cardId);
}