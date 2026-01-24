using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

using Glide.Data.Boards;
using Glide.Web.Features;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Glide.Web.Boards;

[Route("/boards")]
[ApiController]
public class BoardController(BoardRepository boardRepository) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<RazorComponentResult<BoardList>> GetBoardsAsync()
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new Exception("Shouldn't be here!");
        }

        IEnumerable<Board> boards = await boardRepository.GetByUserIdAsync(userId);
        return new RazorComponentResult<BoardList>(new Dictionary<string, object?> { { "Boards", boards } });
    }

    [HttpPost]
    [Authorize]
    public async Task<RazorComponentResult<BoardCard>> CreateAsync([FromForm] string name)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(name))
        {
            return new RazorComponentResult<BoardCard>();
        }

        Board board = await boardRepository.CreateAsync(name, userId);
        return new RazorComponentResult<BoardCard>(new { Board = board });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id)
    {
        Board? existing = await boardRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound("Board not found");
        }

        await boardRepository.DeleteAsync(id);
        return Ok("");
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<RazorComponentResult<BoardCard>> UpdateAsync([FromRoute] string id, [FromForm] string name)
    {
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
    public async Task<RazorComponentResult<BoardDetail>> GetByIdAsync([FromRoute] string id)
    {
        Board? board = await boardRepository.GetByIdAsync(id);
        return new RazorComponentResult<BoardDetail>(new { Board = board });
    }
}