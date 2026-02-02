using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Glide.Web.Labels;

using Markdig;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Glide.Web.Cards;

[Route("/cards")]
[ApiController]
public class CardController(CardAction cardAction, LabelAction labelAction) : ControllerBase
{
    public static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .UseSoftlineBreakAsHardlineBreak()
            .Build();

    [HttpGet("{id}/edit")]
    [Authorize]
    public async Task<IResult> EditAsync([FromRoute] string id)
    {
        CardAction.Result<CardView> result = await cardAction.GetForEditAsync(id, User);
        if (result.IsError)
        {
            return result.StatusResult!;
        }

        // Get all labels for the board
        LabelAction.Result<IEnumerable<LabelView>> labelsResult =
            await labelAction.GetByBoardIdAsync(result.Object!.BoardId, User);

        var allLabels = labelsResult.IsError ? Enumerable.Empty<LabelView>() : labelsResult.Object!;

        return new RazorComponentResult<CardModal>(new
        {
            Card = result.Object,
            AllBoardLabels = allLabels
        });
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IResult> UpdateAsync(
        [FromRoute] string id,
        [FromForm] string title,
        [FromForm] string? description)
    {
        CardAction.Result<CardView> result = await cardAction.UpdateAsync(id, title, description, User);
        return result.IsError
            ? result.StatusResult!
            : new RazorComponentResult<CardCard>(new { Card = result.Object });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IResult> DeleteAsync([FromRoute] string id)
    {
        CardAction.Result<string> result = await cardAction.DeleteAsync(id, User);
        return result.IsError ? result.StatusResult! : Results.Ok();
    }

    [HttpPut("{id}/move")]
    [Authorize]
    public async Task<IResult> MoveAsync(
        [FromRoute] string id,
        [FromForm(Name = "column_id")] string columnId,
        [FromForm] int? position)
    {
        CardAction.Result<CardView> result = await cardAction.MoveAsync(id, columnId, position, User);
        return result.IsError
            ? result.StatusResult!
            : new RazorComponentResult<CardCard>(new { Card = result.Object });
    }

    [HttpPost("markdown")]
    [Authorize]
    public IResult ConvertToHtml([FromForm] string markdown)
    {
        return Results.Text(Markdown.ToHtml(markdown, Pipeline), "text/html");
    }

    [HttpPost("{cardId}/labels")]
    [Authorize]
    public async Task<IResult> AddLabelAsync(
        [FromRoute] string cardId,
        [FromForm(Name = "label_id")] string labelId)
    {
        LabelAction.Result<IEnumerable<LabelView>> result = await labelAction.AddLabelToCardAsync(cardId, labelId, User);
        return result.IsError
            ? result.StatusResult!
            : new RazorComponentResult<LabelList>(new { Labels = result.Object });
    }

    [HttpDelete("{cardId}/labels/{labelId}")]
    [Authorize]
    public async Task<IResult> RemoveLabelAsync([FromRoute] string cardId, [FromRoute] string labelId)
    {
        LabelAction.Result<IEnumerable<LabelView>> result = await labelAction.RemoveLabelFromCardAsync(cardId, labelId, User);
        return result.IsError
            ? result.StatusResult!
            : new RazorComponentResult<LabelList>(new { Labels = result.Object });
    }
}
