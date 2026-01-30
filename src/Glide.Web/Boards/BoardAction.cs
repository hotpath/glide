using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Glide.Data.Boards;
using Glide.Data.Swimlanes;
using Glide.Data.Cards;

using Microsoft.AspNetCore.Http;

using Card = Glide.Data.Cards.Card;

namespace Glide.Web.Boards;

public class BoardAction(
    BoardRepository boardRepository,
    SwimlaneRepository swimlaneRepository,
    CardRepository cardRepository)
{
    public enum DeleteResult { Success, Unauthenticated, NoOwnership }

    public async Task<Result<IEnumerable<BoardView>>> GetByUserAsync(ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return new Result<IEnumerable<BoardView>>(Results.Unauthorized());
        }

        IEnumerable<Board> boards = await boardRepository.GetByUserIdAsync(userId);

        return new Result<IEnumerable<BoardView>>(boards.Select(b => BoardView.FromBoard(b, userId)));
    }

    public async Task<Result<BoardView>> CreateAsync(string name, ClaimsPrincipal user)
    {
        string? userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<BoardView>(Results.Unauthorized());
        }

        Board board = await boardRepository.CreateAsync(name, userId);
        await swimlaneRepository.CreateDefaultSwimlanesAsync(board.Id);
        return new Result<BoardView>(BoardView.FromBoard(board, userId));
    }

    public async Task<Result<string>> DeleteAsync(string boardId, ClaimsPrincipal user)
    {
        string? userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return new Result<string>(Results.Unauthorized());
        }

        Board? existing = await boardRepository.GetByIdAsync(boardId);
        if (existing is null || !existing.BoardUsers.Any(x => x.UserId == userId && x.IsOwner))
        {
            return new Result<string>(Results.NotFound());
        }

        await boardRepository.DeleteAsync(boardId);
        return new Result<string>("");
    }

    public async Task<Result<BoardView>> UpdateAsync(string boardId, string name, ClaimsPrincipal user)
    {
        string? userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return new Result<BoardView>(Results.Unauthorized());
        }

        Board? existing = await boardRepository.GetByIdAsync(boardId);
        if (existing is null || !existing.BoardUsers.Any(x => x.UserId == userId && x.IsOwner))
        {
            return new Result<BoardView>(Results.NotFound("board not found"));
        }

        Board? updated = await boardRepository.UpdateAsync(boardId, name);
        return updated is null
            ? new Result<BoardView>(null, null)
            : new Result<BoardView>(BoardView.FromBoard(updated, userId));
    }

    public async Task<Result<BoardView>> GetByIdAsync(string boardId, ClaimsPrincipal user)
    {
        string? userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return new Result<BoardView>(Results.Unauthorized());
        }

        Board? board = await boardRepository.GetByIdAsync(boardId);
        if (board is null || board.BoardUsers.All(bu => bu.UserId != userId))
        {
            return new Result<BoardView>(Results.NotFound("board not found"));
        }

        return new Result<BoardView>(BoardView.FromBoard(board, userId));
    }

    public async Task<Result<IEnumerable<SwimlaneView>>> GetSwimlanesAsync(string boardId, ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<IEnumerable<SwimlaneView>>(Results.Unauthorized());
        }

        Board? board = await boardRepository.GetByIdAsync(boardId);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return new Result<IEnumerable<SwimlaneView>>(Results.NotFound("board not found"));
        }

        IEnumerable<Swimlane> swimlanes = await swimlaneRepository.GetAllByBoardIdAsync(boardId);
        return new Result<IEnumerable<SwimlaneView>>(swimlanes.Select(SwimlaneView.FromSwimlane));
    }

    public async Task<Result<IEnumerable<SwimlaneView>>> CreateSwimlaneAsync(string boardId, string name,
        ClaimsPrincipal user)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new Result<IEnumerable<SwimlaneView>>(Results.BadRequest("name is required"));
        }

        // verify the board exists and belongs to the user
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<IEnumerable<SwimlaneView>>(Results.Unauthorized());
        }

        Board? board = await boardRepository.GetByIdAsync(boardId);
        if (board is null || !board.BoardUsers.Any(x => x.UserId == userId && x.IsOwner))
        {
            return new Result<IEnumerable<SwimlaneView>>(Results.NotFound("board not found"));
        }

        int existingPosition = await swimlaneRepository.GetMaxPositionAsync(boardId);
        await swimlaneRepository.CreateAsync(name, boardId, existingPosition + 1);

        IEnumerable<Swimlane> allSwimlanes = await swimlaneRepository.GetAllByBoardIdAsync(boardId);
        return new Result<IEnumerable<SwimlaneView>>(allSwimlanes.Select(SwimlaneView.FromSwimlane));
    }

    public async Task<Result<CardView>> CreateCardAsync(
        string boardId,
        string swimlaneId,
        string title,
        ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<CardView>(Results.Unauthorized());
        }

        if (string.IsNullOrWhiteSpace(swimlaneId) || string.IsNullOrWhiteSpace(title))
        {
            return new Result<CardView>(Results.BadRequest("missing required parameters"));
        }

        Board? board = await boardRepository.GetByIdAsync(boardId);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return new Result<CardView>(Results.NotFound("board not found"));
        }

        Card card = await cardRepository.CreateAsync(title, boardId, swimlaneId);
        return new Result<CardView>(CardView.FromCard(card));
    }

    public record Result<T>(T? Object, IResult? StatusResult = null)
    {
        public Result(IResult statusResult) : this(default, statusResult) { }

        public bool IsError => StatusResult is not null;
    }
}