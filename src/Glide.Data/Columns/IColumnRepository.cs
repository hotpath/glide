using System.Collections.Generic;
using System.Threading.Tasks;

namespace Glide.Data.Columns;

public interface IColumnRepository
{
    Task<Column> CreateAsync(string name, string boardId, int position);
    Task CreateDefaultColumnsAsync(string boardId);
    Task<int> GetMaxPositionAsync(string boardId);
    Task<Column?> GetByIdAsync(string id);
    Task<IEnumerable<Column>> GetAllByBoardIdAsync(string boardId);
    Task DeleteAsync(string id);
}