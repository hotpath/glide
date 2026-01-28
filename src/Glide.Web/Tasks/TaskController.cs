using System.Threading.Tasks;

using Glide.Data.Swimlanes;
using Glide.Data.Tasks;

using Markdig;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using Task = Glide.Data.Tasks.Task;

namespace Glide.Web.Tasks;

[Route("/tasks")]
[ApiController]
public class TaskController(TaskRepository taskRepository, SwimlaneRepository swimlaneRepository) : ControllerBase
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
        //TODO: Make sure the board belongs to the user for this task
        Task? task = await taskRepository.GetByIdAsync(id);
        if (task is null)
        {
            return Results.NotFound("Task not found");
        }

        return new RazorComponentResult<TaskModal>(new { Task = task });
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IResult> UpdateAsync(
        [FromRoute] string id,
        [FromForm] string title,
        [FromForm] string? description)
    {
        // TODO: Make sure the board belongs to the user for this task
        Task? existing = await taskRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return Results.NotFound("Task not found");
        }

        await taskRepository.UpdateAsync(id, title, description);

        Task? updated = await taskRepository.GetByIdAsync(id);
        if (updated is null)
        {
            return Results.InternalServerError("failed to retrieve updated task");
        }

        return new RazorComponentResult<TaskCard>(new { Task = updated });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IResult> DeleteAsync([FromRoute] string id)
    {
        // TODO: Make sure the board belongs to the user for this task
        Task? existing = await taskRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return Results.NotFound("task not found");
        }

        await taskRepository.DeleteAsync(id);

        return Results.Ok();
    }

    [HttpPut("{id}/move")]
    [Authorize]
    public async Task<IResult> MoveAsync([FromRoute] string id, [FromForm(Name = "swimlane_id")] string swimlaneId,
        [FromForm] int? position)
    {
        // TODO: Make sure the board belongs to the user for this task
        Task? existing = await taskRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return Results.NotFound("task not found");
        }

        Swimlane? swimlane = await swimlaneRepository.GetByIdAsync(swimlaneId);
        if (swimlane is null)
        {
            return Results.NotFound("swimlane not found");
        }

        await taskRepository.MoveToSwimlaneAsync(id, swimlaneId, position);

        Task? moved = await taskRepository.GetByIdAsync(id);
        if (moved is null)
        {
            return Results.InternalServerError("could not retrieve moved task");
        }

        return new RazorComponentResult<TaskCard>(new { Task = moved });
    }

    [HttpPost("markdown")]
    [Authorize]
    public IResult ConvertToHtml([FromForm] string markdown)
    {
        return Results.Text(Markdown.ToHtml(markdown, Pipeline), "text/html");
    }
}