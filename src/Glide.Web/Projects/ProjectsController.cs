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

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new RazorComponentResult<ProjectSingle>();
        }

        if (string.IsNullOrWhiteSpace(name))
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
}