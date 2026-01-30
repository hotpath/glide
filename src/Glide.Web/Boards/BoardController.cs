using System.Collections.Generic;
using System.Threading.Tasks;

using Glide.Web.Cards;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Glide.Web.Boards;

[Route("/boards")]
[ApiController]
public class BoardController(BoardAction boardAction) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IResult> GetBoardsAsync()
    {
        BoardAction.Result<IEnumerable<BoardView>> result = await boardAction.GetByUserAsync(User);
        return result.IsError
            ? result.StatusResult!
            : new RazorComponentResult<BoardList>(new { Boards = result.Object });
    }

    [HttpPost]
    [Authorize]
    public async Task<IResult> CreateAsync([FromForm] string name)
    {
        BoardAction.Result<BoardView> result = await boardAction.CreateAsync(name, User);
        return result.IsError
            ? result.StatusResult!
            : new RazorComponentResult<BoardCard>(new { Board = result.Object });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IResult> DeleteAsync([FromRoute] string id)
    {
        BoardAction.Result<string> result = await boardAction.DeleteAsync(id, User);
        return result.IsError ? result.StatusResult! : Results.Ok(result.Object);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IResult> UpdateAsync([FromRoute] string id, [FromForm] string name)
    {
        BoardAction.Result<BoardView> result = await boardAction.UpdateAsync(id, name, User);
        return result.IsError
            ? result.StatusResult!
            : new RazorComponentResult<BoardCard>(new { Board = result.Object });
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IResult> GetByIdAsync([FromRoute] string id)
    {
        BoardAction.Result<BoardView> result = await boardAction.GetByIdAsync(id, User);
        return result.IsError
            ? result.StatusResult!
            : new RazorComponentResult<BoardDetail>(new { Board = result.Object });
    }

    [HttpGet("{id}/swimlanes")]
    [Authorize]
    public async Task<IResult> GetSwimlanesAsync([FromRoute] string id)
    {
        BoardAction.Result<IEnumerable<SwimlaneView>> result = await boardAction.GetSwimlanesAsync(id, User);
        return result.IsError
            ? result.StatusResult!
            : new RazorComponentResult<SwimlaneLayout>(new { Swimlanes = result.Object });
    }

    [HttpPost("{boardId}/swimlanes")]
    [Authorize]
    public async Task<IResult> CreateSwimlane([FromRoute] string boardId, [FromForm] string name)
    {
        BoardAction.Result<IEnumerable<SwimlaneView>> result =
            await boardAction.CreateSwimlaneAsync(boardId, name, User);
        return result.IsError
            ? result.StatusResult!
            : new RazorComponentResult<SwimlaneLayout>(new { Swimlanes = result.Object });
    }

    [HttpPost("{boardId}/cards")]
    [Authorize]
    public async Task<IResult> CreateCardAsync(
        [FromRoute] string boardId,
        [FromForm(Name = "swimlane_id")] string swimlaneId,
        [FromForm] string title)
    {
        BoardAction.Result<CardView> result = await boardAction.CreateCardAsync(boardId, swimlaneId, title, User);
        return result.IsError ? result.StatusResult! : new RazorComponentResult<CardCard>(new { Card = result.Object });
    }
}