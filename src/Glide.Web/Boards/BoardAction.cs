using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Glide.Data.Boards;
using Glide.Data.Cards;
using Glide.Data.Columns;
using Glide.Data.Labels;
using Glide.Data.Users;

using Microsoft.AspNetCore.Http;

using Card = Glide.Data.Cards.Card;

namespace Glide.Web.Boards;

public class BoardAction(
    IBoardRepository boardRepository,
    IColumnRepository columnRepository,
    ICardRepository cardRepository,
    ILabelRepository labelRepository,
    IUserRepository userRepository)
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
        await columnRepository.CreateDefaultColumnsAsync(board.Id);
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

    public async Task<Result<IEnumerable<ColumnView>>> GetColumnsAsync(string boardId, ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<IEnumerable<ColumnView>>(Results.Unauthorized());
        }

        Board? board = await boardRepository.GetByIdAsync(boardId);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return new Result<IEnumerable<ColumnView>>(Results.NotFound("board not found"));
        }

        IEnumerable<Column> columns = await columnRepository.GetAllByBoardIdAsync(boardId);

        // Load labels for all cards in all columns
        var cardIds = columns.SelectMany(c => c.Cards).Select(card => card.Id).ToList();
        var cardLabelsDict = new Dictionary<string, IEnumerable<Label>>();

        foreach (var cardId in cardIds)
        {
            var labels = await labelRepository.GetLabelsByCardIdAsync(cardId);
            cardLabelsDict[cardId] = labels;
        }

        // Create ColumnViews with labels
        var columnViews = columns.Select(col => new ColumnView(
            col.Id,
            col.Name,
            col.BoardId,
            col.Position,
            col.Cards.Select(card => CardView.FromCard(
                card,
                cardLabelsDict.ContainsKey(card.Id) ? cardLabelsDict[card.Id] : null
            ))
        ));

        return new Result<IEnumerable<ColumnView>>(columnViews);
    }

    public async Task<Result<IEnumerable<ColumnView>>> CreateColumnAsync(string boardId, string name,
        ClaimsPrincipal user)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new Result<IEnumerable<ColumnView>>(Results.BadRequest("name is required"));
        }

        // verify the board exists and belongs to the user
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<IEnumerable<ColumnView>>(Results.Unauthorized());
        }

        Board? board = await boardRepository.GetByIdAsync(boardId);
        if (board is null || !board.BoardUsers.Any(x => x.UserId == userId && x.IsOwner))
        {
            return new Result<IEnumerable<ColumnView>>(Results.NotFound("board not found"));
        }

        int existingPosition = await columnRepository.GetMaxPositionAsync(boardId);
        await columnRepository.CreateAsync(name, boardId, existingPosition + 1);

        IEnumerable<Column> allColumns = await columnRepository.GetAllByBoardIdAsync(boardId);

        // Load labels for all cards
        var cardIds = allColumns.SelectMany(c => c.Cards).Select(card => card.Id).ToList();
        var cardLabelsDict = new Dictionary<string, IEnumerable<Label>>();

        foreach (var cardId in cardIds)
        {
            var labels = await labelRepository.GetLabelsByCardIdAsync(cardId);
            cardLabelsDict[cardId] = labels;
        }

        var columnViews = allColumns.Select(col => new ColumnView(
            col.Id,
            col.Name,
            col.BoardId,
            col.Position,
            col.Cards.Select(card => CardView.FromCard(
                card,
                cardLabelsDict.ContainsKey(card.Id) ? cardLabelsDict[card.Id] : null
            ))
        ));

        return new Result<IEnumerable<ColumnView>>(columnViews);
    }

    public async Task<Result<CardView>> CreateCardAsync(
        string boardId,
        string columnId,
        string title,
        ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new Result<CardView>(Results.Unauthorized());
        }

        if (string.IsNullOrWhiteSpace(columnId) || string.IsNullOrWhiteSpace(title))
        {
            return new Result<CardView>(Results.BadRequest("missing required parameters"));
        }

        Board? board = await boardRepository.GetByIdAsync(boardId);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return new Result<CardView>(Results.NotFound("board not found"));
        }

        Card card = await cardRepository.CreateAsync(title, boardId, columnId);
        var labels = await labelRepository.GetLabelsByCardIdAsync(card.Id);
        return new Result<CardView>(CardView.FromCard(card, labels));
    }

    public async Task<Result<IEnumerable<BoardMemberView>>> GetBoardUsersAsync(string boardId, ClaimsPrincipal user)
    {
        string? userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return new Result<IEnumerable<BoardMemberView>>(Results.Unauthorized());
        }

        Board? board = await boardRepository.GetByIdAsync(boardId);
        if (board is null || board.BoardUsers.All(bu => bu.UserId != userId))
        {
            return new Result<IEnumerable<BoardMemberView>>(Results.NotFound("board not found"));
        }

        IEnumerable<BoardMemberDetails> members = await boardRepository.GetBoardMembersWithEmailAsync(boardId);
        return new Result<IEnumerable<BoardMemberView>>(
            members.Select(m => new BoardMemberView(m.UserId, m.Email, m.IsOwner)));
    }

    public async Task<Result<BoardUserView>> AddUserToBoardAsync(
        string boardId,
        string userId,
        bool isOwner,
        ClaimsPrincipal user)
    {
        string? requestingUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requestingUserId is null)
        {
            return new Result<BoardUserView>(Results.Unauthorized());
        }

        Board? board = await boardRepository.GetByIdAsync(boardId);
        if (board is null || !board.BoardUsers.Any(x => x.UserId == requestingUserId && x.IsOwner))
        {
            return new Result<BoardUserView>(Results.NotFound("board not found or insufficient permissions"));
        }

        // Check if user is already on the board
        if (board.BoardUsers.Any(x => x.UserId == userId))
        {
            return new Result<BoardUserView>(Results.BadRequest("user is already a member of this board"));
        }

        try
        {
            await boardRepository.AddUserToBoardAsync(boardId, userId, isOwner);
            return new Result<BoardUserView>(new BoardUserView(userId, isOwner));
        }
        catch (Exception ex)
        {
            return new Result<BoardUserView>(Results.BadRequest($"failed to add user: {ex.Message}"));
        }
    }

    public async Task<Result<string>> UpdateUserRoleAsync(
        string boardId,
        string userId,
        bool isOwner,
        ClaimsPrincipal user)
    {
        string? requestingUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requestingUserId is null)
        {
            return new Result<string>(Results.Unauthorized());
        }

        Board? board = await boardRepository.GetByIdAsync(boardId);
        if (board is null || !board.BoardUsers.Any(x => x.UserId == requestingUserId && x.IsOwner))
        {
            return new Result<string>(Results.NotFound("board not found or insufficient permissions"));
        }

        BoardUser? targetUser = board.BoardUsers.FirstOrDefault(x => x.UserId == userId);
        if (targetUser is null)
        {
            return new Result<string>(Results.NotFound("user is not a member of this board"));
        }

        // Prevent removing the last owner
        if (!isOwner && board.BoardUsers.Count(x => x.IsOwner) == 1)
        {
            return new Result<string>(Results.BadRequest("cannot demote the last owner"));
        }

        await boardRepository.UpdateUserRoleAsync(boardId, userId, isOwner);
        return new Result<string>("");
    }

    public async Task<Result<string>> RemoveUserFromBoardAsync(
        string boardId,
        string userId,
        ClaimsPrincipal user)
    {
        string? requestingUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requestingUserId is null)
        {
            return new Result<string>(Results.Unauthorized());
        }

        Board? board = await boardRepository.GetByIdAsync(boardId);
        if (board is null || !board.BoardUsers.Any(x => x.UserId == requestingUserId && x.IsOwner))
        {
            return new Result<string>(Results.NotFound("board not found or insufficient permissions"));
        }

        BoardUser? targetUser = board.BoardUsers.FirstOrDefault(x => x.UserId == userId);
        if (targetUser is null)
        {
            return new Result<string>(Results.NotFound("user is not a member of this board"));
        }

        // Prevent removing the last owner
        if (targetUser.IsOwner && board.BoardUsers.Count(x => x.IsOwner) == 1)
        {
            return new Result<string>(Results.BadRequest("cannot remove the last owner"));
        }

        await boardRepository.RemoveUserFromBoardAsync(boardId, userId);
        return new Result<string>("");
    }

    public async Task<Result<IEnumerable<UserSearchResultView>>> SearchUsersAsync(string emailQuery)
    {
        if (string.IsNullOrWhiteSpace(emailQuery))
        {
            return new Result<IEnumerable<UserSearchResultView>>(Results.BadRequest("email query is required"));
        }

        IEnumerable<User> users = await userRepository.SearchByEmailAsync(emailQuery);
        return new Result<IEnumerable<UserSearchResultView>>(users.Select(u => UserSearchResultView.FromUser(u)));
    }

    public record Result<T>(T? Object, IResult? StatusResult = null)
    {
        public Result(IResult statusResult) : this(default, statusResult) { }

        public bool IsError => StatusResult is not null;
    }
}