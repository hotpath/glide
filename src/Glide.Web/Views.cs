using System.Collections.Generic;
using System.Linq;

using Glide.Data.Boards;
using Glide.Data.Swimlanes;
using Glide.Data.Tasks;

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

public record SwimlaneView(string Id, string Name, string BoardId, int Position, IEnumerable<TaskView> Tasks)
{
    public static SwimlaneView FromSwimlane(Swimlane swimlane)
    {
        return new SwimlaneView(swimlane.Id, swimlane.Name, swimlane.BoardId, swimlane.Position,
            swimlane.Tasks.Select(TaskView.FromTask));
    }
}

public record TaskView(
    string Id,
    string Title,
    string? Description,
    string BoardId,
    string? SwimlaneId,
    string? AssignedTo,
    int Position)
{
    public static TaskView FromTask(Task task)
    {
        return new TaskView(task.Id, task.Title, task.Description, task.BoardId, task.SwimlaneId, task.AssignedTo,
            task.Position);
    }
}