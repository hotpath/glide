using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Glide.Data.Boards;
using Glide.Data.Cards;
using Glide.Data.Columns;

using Microsoft.AspNetCore.Http;

using Card = Glide.Data.Cards.Card;

namespace Glide.Web.Cards;

public class CardAction(
    CardRepository cardRepository,
    BoardRepository boardRepository,
    ColumnRepository columnRepository)
{
    public async Task<Result<CardView>> GetForEditAsync(string cardId, ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<CardView>(Results.Unauthorized());
        }

        Card? card = await cardRepository.GetByIdAsync(cardId);
        if (card is null)
        {
            return new Result<CardView>(Results.NotFound("Card not found"));
        }

        // Verify the board belongs to the user
        Board? board = await boardRepository.GetByIdAsync(card.BoardId);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return new Result<CardView>(Results.NotFound("Card not found"));
        }

        return new Result<CardView>(CardView.FromCard(card));
    }

    public async Task<Result<CardView>> UpdateAsync(
        string cardId,
        string title,
        string? description,
        ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<CardView>(Results.Unauthorized());
        }

        Card? existing = await cardRepository.GetByIdAsync(cardId);
        if (existing is null)
        {
            return new Result<CardView>(Results.NotFound("Card not found"));
        }

        // Verify the board belongs to the user
        Board? board = await boardRepository.GetByIdAsync(existing.BoardId);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return new Result<CardView>(Results.NotFound("Card not found"));
        }

        await cardRepository.UpdateAsync(cardId, title, description);

        Card? updated = await cardRepository.GetByIdAsync(cardId);
        if (updated is null)
        {
            return new Result<CardView>(Results.InternalServerError("Failed to retrieve updated card"));
        }

        return new Result<CardView>(CardView.FromCard(updated));
    }

    public async Task<Result<string>> DeleteAsync(string cardId, ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<string>(Results.Unauthorized());
        }

        Card? existing = await cardRepository.GetByIdAsync(cardId);
        if (existing is null)
        {
            return new Result<string>(Results.NotFound("Card not found"));
        }

        // Verify the board belongs to the user
        Board? board = await boardRepository.GetByIdAsync(existing.BoardId);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return new Result<string>(Results.NotFound("Card not found"));
        }

        await cardRepository.DeleteAsync(cardId);
        return new Result<string>("");
    }

    public async Task<Result<CardView>> MoveAsync(
        string cardId,
        string columnId,
        int? position,
        ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<CardView>(Results.Unauthorized());
        }

        Card? existing = await cardRepository.GetByIdAsync(cardId);
        if (existing is null)
        {
            return new Result<CardView>(Results.NotFound("Card not found"));
        }

        // Verify the board belongs to the user
        Board? board = await boardRepository.GetByIdAsync(existing.BoardId);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return new Result<CardView>(Results.NotFound("Card not found"));
        }

        Column? column = await columnRepository.GetByIdAsync(columnId);
        if (column is null)
        {
            return new Result<CardView>(Results.NotFound("Column not found"));
        }

        // Verify column belongs to the same board
        if (column.BoardId != existing.BoardId)
        {
            return new Result<CardView>(Results.BadRequest("Column does not belong to the same board"));
        }

        await cardRepository.MoveToColumnAsync(cardId, columnId, position);

        Card? moved = await cardRepository.GetByIdAsync(cardId);
        if (moved is null)
        {
            return new Result<CardView>(Results.InternalServerError("Could not retrieve moved card"));
        }

        return new Result<CardView>(CardView.FromCard(moved));
    }

    public record Result<T>(T? Object, IResult? StatusResult = null)
    {
        public Result(IResult statusResult) : this(default, statusResult) { }

        public bool IsError => StatusResult is not null;
    }
}
