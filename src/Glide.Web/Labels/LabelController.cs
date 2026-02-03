using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Glide.Web.Labels;

[Route("/labels")]
[ApiController]
public class LabelController(LabelAction labelAction) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<IResult> CreateAsync(
        [FromForm(Name = "board_id")] string boardId,
        [FromForm] string name,
        [FromForm] string? icon)
    {
        LabelAction.Result<LabelView> result = await labelAction.CreateAsync(boardId, name, icon, User);
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
        [FromForm] string name,
        [FromForm] string? icon)
    {
        LabelAction.Result<LabelView> result = await labelAction.UpdateAsync(id, name, icon, User);
        return result.IsError
            ? result.StatusResult!
            : new RazorComponentResult<LabelBadge>(new { Label = result.Object });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IResult> DeleteAsync([FromRoute] string id)
    {
        LabelAction.Result<string> result = await labelAction.DeleteAsync(id, User);
        return result.IsError ? result.StatusResult! : Results.Ok();
    }
}
