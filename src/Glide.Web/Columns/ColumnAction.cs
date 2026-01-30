using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Glide.Data.Boards;
using Glide.Data.Columns;

using Microsoft.AspNetCore.Http;

namespace Glide.Web.Columns;

public class ColumnAction(ColumnRepository columnRepository, IBoardRepository boardRepository)
{
    public async Task<Result<IEnumerable<ColumnView>>> DeleteColumnAsync(string columnId, ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<IEnumerable<ColumnView>>(Results.Unauthorized());
        }

        Column? existing = await columnRepository.GetByIdAsync(columnId);
        if (existing is null)
        {
            return new Result<IEnumerable<ColumnView>>(Results.NotFound("column not found"));
        }

        Board? board = await boardRepository.GetByIdAsync(existing.BoardId);

        if (board is null || !board.BoardUsers.Any(x => x.UserId == userId && x.IsOwner))
        {
            return new Result<IEnumerable<ColumnView>>(Results.NotFound("board not found"));
        }

        await columnRepository.DeleteAsync(columnId);

        IEnumerable<Column> columns = await columnRepository.GetAllByBoardIdAsync(board.Id);

        return new Result<IEnumerable<ColumnView>>(columns.Select(ColumnView.FromColumn));
    }

    public record Result<T>(T? Object, IResult? StatusResult = null)
    {
        public Result(IResult statusResult) : this(default, statusResult) { }

        public bool IsError => StatusResult is not null;
    }
}