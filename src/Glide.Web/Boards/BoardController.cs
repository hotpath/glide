using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Glide.Data.Boards;
using Glide.Data.Swimlanes;
using Glide.Data.Tasks;
using Glide.Web.Features;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using Task = Glide.Data.Tasks.Task;

namespace Glide.Web.Boards;

[Route("/boards")]
[ApiController]
public class BoardController(
    BoardRepository boardRepository,
    SwimlaneRepository swimlaneRepository,
    TaskRepository taskRepository) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IResult> GetBoardsAsync()
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        IEnumerable<Board> boards = await boardRepository.GetByUserIdAsync(userId);
        return new RazorComponentResult<BoardList>(new Dictionary<string, object?> { { "Boards", boards } });
    }

    [HttpPost]
    [Authorize]
    public async Task<IResult> CreateAsync([FromForm] string name)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        Board board = await boardRepository.CreateAsync(name, userId);
        await swimlaneRepository.CreateDefaultSwimlanesAsync(board.Id);
        return new RazorComponentResult<BoardCard>(new { Board = board });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Board? existing = await boardRepository.GetByIdAsync(id);
        if (existing is null || !existing.BoardUsers.Any(x => x.UserId == userId && x.IsOwner))
        {
            return NotFound("Board not found");
        }


        await boardRepository.DeleteAsync(id);
        return Ok("");
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IResult> UpdateAsync([FromRoute] string id, [FromForm] string name)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        Board? existing = await boardRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return new RazorComponentResult<BoardCard>();
        }

        Board? updated = await boardRepository.UpdateAsync(id, name);
        if (updated is null)
        {
            return new RazorComponentResult<BoardCard>();
        }

        return new RazorComponentResult<BoardCard>(new { Board = updated });
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IResult> GetByIdAsync([FromRoute] string id)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        Board? board = await boardRepository.GetByIdAsync(id);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return Results.NotFound();
        }

        return new RazorComponentResult<BoardDetail>(new { Board = board });
    }

    [HttpGet("{id}/swimlanes")]
    [Authorize]
    public async Task<IResult> GetSwimlanesAsync([FromRoute] string id)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        Board? board = await boardRepository.GetByIdAsync(id);
        if (board is null || board.BoardUsers.All(x => x.UserId != userId))
        {
            return Results.NotFound();
        }

        IEnumerable<Swimlane> swimlanes = await swimlaneRepository.GetAllByBoardIdAsync(id);
        return new RazorComponentResult<SwimlaneLayout>(new { Swimlanes = swimlanes });
    }

    [HttpPost("{boardId}/tasks")]
    [Authorize]
    public async Task<IResult> CreateTaskAsync(
        [FromRoute] string boardId,
        [FromForm(Name = "swimlane_id")] string swimlaneId,
        [FromForm] string title)
    {
        if (string.IsNullOrWhiteSpace(swimlaneId) || string.IsNullOrWhiteSpace(title))
        {
            return Results.BadRequest("Missing required parameters");
        }

        Task task = await taskRepository.CreateAsync(title, boardId, swimlaneId);

        return new RazorComponentResult<TaskCard>(new { Task = task });
    }
}