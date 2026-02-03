using System.Collections.Generic;
using System.Linq;

using Glide.Data.Boards;
using Glide.Data.Columns;
using Glide.Data.Cards;
using Glide.Data.Labels;

namespace Glide.Web;

public record BoardView(string Id, string Name, BoardUserView? BoardUserView = null)
{
    public static BoardView FromBoard(Board board, string userId)
    {
        return new BoardView(board.Id, board.Name,
            BoardUserView.FromBoardUser(board.BoardUsers.FirstOrDefault(x => x.UserId == userId)));
    }
}

public record BoardUserView(string Username, bool IsOwner)
{
    public static BoardUserView? FromBoardUser(BoardUser? boardUser)
    {
        return boardUser is null ? null : new BoardUserView(boardUser.UserId, boardUser.IsOwner);
    }
}

public record ColumnView(string Id, string Name, string BoardId, int Position, IEnumerable<CardView> Cards)
{
    public static ColumnView FromColumn(Column column)
    {
        return new ColumnView(column.Id, column.Name, column.BoardId, column.Position,
            column.Cards.Select(c => CardView.FromCard(c)));
    }
}

public record CardView(
    string Id,
    string Title,
    string? Description,
    string BoardId,
    string? ColumnId,
    string? AssignedTo,
    int Position,
    IEnumerable<LabelView> Labels = null!)
{
    public static CardView FromCard(Card card, IEnumerable<Label>? labels = null)
    {
        return new CardView(
            card.Id,
            card.Title,
            card.Description,
            card.BoardId,
            card.ColumnId,
            card.AssignedTo,
            card.Position,
            labels?.Select(LabelView.FromLabel) ?? Enumerable.Empty<LabelView>());
    }
}

public record LabelView(string Id, string Name, string BoardId)
{
    public static LabelView FromLabel(Label label)
    {
        return new LabelView(label.Id, label.Name, label.BoardId);
    }
}