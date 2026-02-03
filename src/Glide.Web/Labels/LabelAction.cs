using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Glide.Data.Boards;
using Glide.Data.Cards;
using Glide.Data.Labels;

using Microsoft.AspNetCore.Http;

namespace Glide.Web.Labels;

public class LabelAction(
    ILabelRepository labelRepository,
    IBoardRepository boardRepository,
    ICardRepository cardRepository)
{
    public async Task<Result<LabelView>> GetByIdAsync(string labelId, ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<LabelView>(Results.Unauthorized());
        }

        Label? label = await labelRepository.GetByIdAsync(labelId);
        if (label is null)
        {
            return new Result<LabelView>(Results.NotFound("Label not found"));
        }

        Board? board = await boardRepository.GetByIdAsync(label.BoardId);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return new Result<LabelView>(Results.NotFound("Label not found"));
        }

        return new Result<LabelView>(LabelView.FromLabel(label));
    }

    public async Task<Result<IEnumerable<LabelView>>> GetByBoardIdAsync(string boardId, ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<IEnumerable<LabelView>>(Results.Unauthorized());
        }

        Board? board = await boardRepository.GetByIdAsync(boardId);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return new Result<IEnumerable<LabelView>>(Results.NotFound("Board not found"));
        }

        IEnumerable<Label> labels = await labelRepository.GetByBoardIdAsync(boardId);
        return new Result<IEnumerable<LabelView>>(labels.Select(LabelView.FromLabel));
    }

    public async Task<Result<LabelView>> CreateAsync(
        string boardId,
        string name,
        ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<LabelView>(Results.Unauthorized());
        }

        Board? board = await boardRepository.GetByIdAsync(boardId);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return new Result<LabelView>(Results.NotFound("Board not found"));
        }

        // Check if user is the board owner
        if (board.BoardUsers.All(x => x.UserId != userId || !x.IsOwner))
        {
            return new Result<LabelView>(Results.Forbid());
        }

        // Check if a label with this name already exists (case-insensitive)
        Label? existingLabel = await labelRepository.GetByBoardIdAndNameAsync(boardId, name);
        if (existingLabel is not null)
        {
            return new Result<LabelView>(LabelView.FromLabel(existingLabel));
        }

        Label label = await labelRepository.CreateAsync(boardId, name);
        return new Result<LabelView>(LabelView.FromLabel(label));
    }

    public async Task<Result<LabelView>> UpdateAsync(
        string labelId,
        string name,
        ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<LabelView>(Results.Unauthorized());
        }

        Label? existing = await labelRepository.GetByIdAsync(labelId);
        if (existing is null)
        {
            return new Result<LabelView>(Results.NotFound("Label not found"));
        }

        Board? board = await boardRepository.GetByIdAsync(existing.BoardId);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return new Result<LabelView>(Results.NotFound("Label not found"));
        }

        // Check if user is the board owner
        if (board.BoardUsers.All(x => x.UserId != userId || !x.IsOwner))
        {
            return new Result<LabelView>(Results.Forbid());
        }

        await labelRepository.UpdateAsync(labelId, name);

        Label? updated = await labelRepository.GetByIdAsync(labelId);
        if (updated is null)
        {
            return new Result<LabelView>(Results.InternalServerError("Failed to retrieve updated label"));
        }

        return new Result<LabelView>(LabelView.FromLabel(updated));
    }

    public async Task<Result<string>> DeleteAsync(string labelId, ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<string>(Results.Unauthorized());
        }

        Label? existing = await labelRepository.GetByIdAsync(labelId);
        if (existing is null)
        {
            return new Result<string>(Results.NotFound("Label not found"));
        }

        Board? board = await boardRepository.GetByIdAsync(existing.BoardId);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return new Result<string>(Results.NotFound("Label not found"));
        }

        // Check if user is the board owner
        if (board.BoardUsers.All(x => x.UserId != userId || !x.IsOwner))
        {
            return new Result<string>(Results.Forbid());
        }

        await labelRepository.DeleteAsync(labelId);
        return new Result<string>("");
    }

    public async Task<Result<IEnumerable<LabelView>>> AddLabelToCardAsync(
        string cardId,
        string labelId,
        ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<IEnumerable<LabelView>>(Results.Unauthorized());
        }

        Card? card = await cardRepository.GetByIdAsync(cardId);
        if (card is null)
        {
            return new Result<IEnumerable<LabelView>>(Results.NotFound("Card not found"));
        }

        Board? board = await boardRepository.GetByIdAsync(card.BoardId);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return new Result<IEnumerable<LabelView>>(Results.NotFound("Card not found"));
        }

        Label? label = await labelRepository.GetByIdAsync(labelId);
        if (label is null)
        {
            return new Result<IEnumerable<LabelView>>(Results.NotFound("Label not found"));
        }

        // Verify label belongs to the same board
        if (label.BoardId != card.BoardId)
        {
            return new Result<IEnumerable<LabelView>>(Results.BadRequest("Label does not belong to the same board"));
        }

        await labelRepository.AddLabelToCardAsync(cardId, labelId);

        IEnumerable<Label> labels = await labelRepository.GetLabelsByCardIdAsync(cardId);
        return new Result<IEnumerable<LabelView>>(labels.Select(LabelView.FromLabel));
    }

    public async Task<Result<IEnumerable<LabelView>>> RemoveLabelFromCardAsync(
        string cardId,
        string labelId,
        ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<IEnumerable<LabelView>>(Results.Unauthorized());
        }

        Card? card = await cardRepository.GetByIdAsync(cardId);
        if (card is null)
        {
            return new Result<IEnumerable<LabelView>>(Results.NotFound("Card not found"));
        }

        Board? board = await boardRepository.GetByIdAsync(card.BoardId);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return new Result<IEnumerable<LabelView>>(Results.NotFound("Card not found"));
        }

        await labelRepository.RemoveLabelFromCardAsync(cardId, labelId);

        IEnumerable<Label> labels = await labelRepository.GetLabelsByCardIdAsync(cardId);
        return new Result<IEnumerable<LabelView>>(labels.Select(LabelView.FromLabel));
    }

    public record Result<T>(T? Object, IResult? StatusResult = null)
    {
        public Result(IResult statusResult) : this(default, statusResult) { }

        public bool IsError => StatusResult is not null;
    }
}
