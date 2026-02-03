using System.Text.Json;
using System.Threading.Tasks;

using Glide.Web.Boards;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Glide.Web.Labels;

[Route("/labels")]
[ApiController]
public class LabelController(LabelAction labelAction, BoardAction boardAction) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<IResult> CreateAsync(
        [FromForm(Name = "board_id")] string boardId,
        [FromForm] string name)
    {
        LabelAction.Result<LabelView> result = await labelAction.CreateAsync(boardId, name, User);
        if (result.IsError)
        {
            return result.StatusResult!;
        }

        // Return JSON for inline creation
        return Results.Json(result.Object);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IResult> UpdateAsync(
        [FromRoute] string id,
        [FromForm] string name)
    {
        LabelAction.Result<LabelView> result = await labelAction.UpdateAsync(id, name, User);
        if (result.IsError)
        {
            return result.StatusResult!;
        }

        // Fetch board columns to refresh the board view
        BoardAction.Result<System.Collections.Generic.IEnumerable<ColumnView>> columnsResult =
            await boardAction.GetColumnsAsync(result.Object!.BoardId, User);

        // Fetch filter labels
        LabelAction.Result<System.Collections.Generic.IEnumerable<LabelView>> labelsResult =
            await labelAction.GetByBoardIdAsync(result.Object.BoardId, User);

        return new RazorComponentResult<LabelBoardRefreshResponse>(new
        {
            Label = result.Object,
            Columns = columnsResult.IsError ? null : columnsResult.Object,
            Labels = labelsResult.IsError ? null : labelsResult.Object
        });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IResult> DeleteAsync([FromRoute] string id)
    {
        // Get the label first to know which board to refresh
        LabelAction.Result<LabelView> labelResult = await labelAction.GetByIdAsync(id, User);
        if (labelResult.IsError)
        {
            return labelResult.StatusResult!;
        }

        string boardId = labelResult.Object!.BoardId;

        LabelAction.Result<string> result = await labelAction.DeleteAsync(id, User);
        if (result.IsError)
        {
            return result.StatusResult!;
        }

        // Fetch board columns to refresh the board view
        BoardAction.Result<System.Collections.Generic.IEnumerable<ColumnView>> columnsResult =
            await boardAction.GetColumnsAsync(boardId, User);

        // Fetch filter labels
        LabelAction.Result<System.Collections.Generic.IEnumerable<LabelView>> labelsResult =
            await labelAction.GetByBoardIdAsync(boardId, User);

        return new RazorComponentResult<LabelBoardRefreshResponse>(new
        {
            Label = (LabelView?)null,
            Columns = columnsResult.IsError ? null : columnsResult.Object,
            Labels = labelsResult.IsError ? null : labelsResult.Object
        });
    }
}