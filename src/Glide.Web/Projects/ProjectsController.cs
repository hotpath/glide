using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

using Glide.Data.Boards;
using Glide.Data.Projects;
using Glide.Data.Swimlanes;
using Glide.Web.Features;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Glide.Web.Projects;

[Route("/projects")]
[ApiController]
public class ProjectsController(
    ProjectRepository projectRepository,
    BoardRepository boardRepository,
    SwimlaneRepository swimlaneRepository) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<RazorComponentResult<ProjectList>> GetProjectsAsync()
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            throw new Exception("Shouldn't get here!");
        }

        IEnumerable<Project> projects = await projectRepository.GetByUserIdAsync(userId);
        return new RazorComponentResult<ProjectList>(new Dictionary<string, object?> { { "Projects", projects } });
    }

    [HttpPost]
    [Authorize]
    public async Task<RazorComponentResult<ProjectSingle>> CreateAsync([FromForm] string name)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(name))
        {
            return new RazorComponentResult<ProjectSingle>();
        }

        // first, create the project
        Project project = await projectRepository.CreateAsync(name, userId);

        // create a default board
        Board board = await boardRepository.CreateAsync("Main Board", project.Id);

        // create default swimlanes
        await swimlaneRepository.CreateDefaultSwimlanesAsync(board.Id);

        return new RazorComponentResult<ProjectSingle>(new Dictionary<string, object?> { { "Project", project } });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Project not found");
        }

        Project? existing = await projectRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound("Project not found");
        }

        await projectRepository.DeleteAsync(id);

        return Ok("");
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<RazorComponentResult<ProjectSingle>> UpdateAsync([FromRoute] string id, [FromForm] string name)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return new RazorComponentResult<ProjectSingle>();
        }

        Project? existing = await projectRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return new RazorComponentResult<ProjectSingle>();
        }

        await projectRepository.UpdateAsync(id, name);

        Project? updated = await projectRepository.GetByIdAsync(id);
        if (updated is not null)
        {
            return new RazorComponentResult<ProjectSingle>(
                new Dictionary<string, object?> { { "Project", updated } });
        }

        return new RazorComponentResult<ProjectSingle>();
    }
}