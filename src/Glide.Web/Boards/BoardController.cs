using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Glide.Web.Cards;
using Glide.Web.Columns;
using Glide.Web.Labels;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Glide.Web.Boards;

[Route("/boards")]
[ApiController]
public class BoardController(BoardAction boardAction, LabelAction labelAction) : ControllerBase
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

    [HttpGet("{id}/columns")]
    [Authorize]
    public async Task<IResult> GetColumnsAsync([FromRoute] string id)
    {
        BoardAction.Result<IEnumerable<ColumnView>> result = await boardAction.GetColumnsAsync(id, User);
        return result.IsError
            ? result.StatusResult!
            : new RazorComponentResult<ColumnLayout>(new { Columns = result.Object });
    }

    [HttpPost("{boardId}/columns")]
    [Authorize]
    public async Task<IResult> CreateColumn([FromRoute] string boardId, [FromForm] string name)
    {
        BoardAction.Result<IEnumerable<ColumnView>> result =
            await boardAction.CreateColumnAsync(boardId, name, User);
        return result.IsError
            ? result.StatusResult!
            : new RazorComponentResult<ColumnLayout>(new { Columns = result.Object });
    }

    [HttpPost("{boardId}/cards")]
    [Authorize]
    public async Task<IResult> CreateCardAsync(
        [FromRoute] string boardId,
        [FromForm(Name = "column_id")] string columnId,
        [FromForm] string title)
    {
        BoardAction.Result<CardView> result = await boardAction.CreateCardAsync(boardId, columnId, title, User);
        return result.IsError ? result.StatusResult! : new RazorComponentResult<CardCard>(new { Card = result.Object });
    }

    [HttpGet("{boardId}/labels")]
    [Authorize]
    public async Task<IResult> GetLabelsAsync([FromRoute] string boardId, [FromQuery] string? view = null)
    {
        LabelAction.Result<IEnumerable<LabelView>> result = await labelAction.GetByBoardIdAsync(boardId, User);
        if (result.IsError)
        {
            return result.StatusResult!;
        }

        // Return different views based on query parameter
        if (view == "filter")
        {
            return new RazorComponentResult<LabelFilterList>(new { Labels = result.Object });
        }

        return new RazorComponentResult<LabelManagementModal>(new { Labels = result.Object, BoardId = boardId });
    }

    [HttpGet("{boardId}/users")]
    [Authorize]
    public async Task<IResult> GetBoardUsersAsync([FromRoute] string boardId)
    {
        BoardAction.Result<IEnumerable<BoardMemberView>> result = await boardAction.GetBoardUsersAsync(boardId, User);
        return result.IsError
            ? result.StatusResult!
            : Results.Json(result.Object);
    }

    [HttpPost("{boardId}/users")]
    [Authorize]
    public async Task<IResult> AddUserToBoardAsync(
        [FromRoute] string boardId,
        [FromForm] string userId,
        [FromForm] bool isOwner)
    {
        BoardAction.Result<BoardUserView>
            result = await boardAction.AddUserToBoardAsync(boardId, userId, isOwner, User);
        return result.IsError
            ? result.StatusResult!
            : Results.Json(result.Object, statusCode: StatusCodes.Status201Created);
    }

    [HttpPut("{boardId}/users/{userId}")]
    [Authorize]
    public async Task<IResult> UpdateUserRoleAsync(
        [FromRoute] string boardId,
        [FromRoute] string userId,
        [FromForm] bool isOwner)
    {
        BoardAction.Result<string> result = await boardAction.UpdateUserRoleAsync(boardId, userId, isOwner, User);
        if (result.IsError)
        {
            return result.StatusResult!;
        }

        // Fetch the updated member to return component
        BoardAction.Result<IEnumerable<BoardMemberView>> members = await boardAction.GetBoardUsersAsync(boardId, User);
        if (members.IsError)
        {
            return Results.BadRequest();
        }

        BoardMemberView? member = members.Object?.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
        {
            return Results.BadRequest();
        }

        return new RazorComponentResult<MemberItem>(new { BoardId = boardId, Member = member });
    }

    [HttpDelete("{boardId}/users/{userId}")]
    [Authorize]
    public async Task<IResult> RemoveUserFromBoardAsync(
        [FromRoute] string boardId,
        [FromRoute] string userId)
    {
        BoardAction.Result<string> result = await boardAction.RemoveUserFromBoardAsync(boardId, userId, User);
        return result.IsError ? result.StatusResult! : Results.Ok();
    }

    [HttpGet("search-users")]
    [Authorize]
    public async Task<IResult> SearchUsersAsync([FromQuery] string q)
    {
        BoardAction.Result<IEnumerable<UserSearchResultView>> result = await boardAction.SearchUsersAsync(q);
        return result.IsError
            ? result.StatusResult!
            : Results.Json(result.Object);
    }
}