using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Glide.Web.App;

[ApiController]
public class AppController : ControllerBase
{
    [HttpGet("/settings")]
    [Authorize]
    public Task<IResult> GetSettings()
    {
        return Task.FromResult<IResult>(new RazorComponentResult<Settings>());
    }

    [HttpGet("/about")]
    [Authorize]
    public Task<IResult> GetAbout()
    {
        return Task.FromResult<IResult>(new RazorComponentResult<About>());
    }

    [HttpGet("/profile")]
    [Authorize]
    public Task<IResult> GetProfile()
    {
        return Task.FromResult<IResult>(new RazorComponentResult<Profile>());
    }
}