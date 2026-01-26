using System.Threading.Tasks;

using Glide.Data.Tasks;
using Glide.Web.Features;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using Task = Glide.Data.Tasks.Task;

namespace Glide.Web.Tasks;

[Route("/tasks")]
[ApiController]
public class TaskController(TaskRepository taskRepository) : ControllerBase
{
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
}