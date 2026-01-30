using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Glide.Web.Columns;

[Route("/columns")]
[ApiController]
public class ColumnController(ColumnAction columnAction) : ControllerBase
{
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IResult> DeleteColumnAsync([FromRoute] string id)
    {
        ColumnAction.Result<IEnumerable<ColumnView>> result = await columnAction.DeleteColumnAsync(id, User);
        return result.IsError
            ? result.StatusResult!
            : new RazorComponentResult<ColumnLayout>(new { Columns = result.Object });
    }
}