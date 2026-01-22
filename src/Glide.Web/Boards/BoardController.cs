using Glide.Data.Boards;
using Glide.Data.Projects;

using Microsoft.AspNetCore.Mvc;

namespace Glide.Web.Boards;

[Route("/projects/{projectId}/boards")]
[ApiController]
public class BoardController(ProjectRepository projectRepository, BoardRepository boardRepository) : ControllerBase
{
}