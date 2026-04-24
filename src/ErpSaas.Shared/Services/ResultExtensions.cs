using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Shared.Services;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new OkResult();

        return result.StatusCode switch
        {
            HttpStatusCode.NotFound => new NotFoundObjectResult(result.Errors),
            HttpStatusCode.Conflict => new ConflictObjectResult(result.Errors),
            HttpStatusCode.Forbidden => new ForbidResult(),
            HttpStatusCode.UnprocessableEntity => new UnprocessableEntityObjectResult(result.Errors),
            _ => new ObjectResult(result.Errors) { StatusCode = (int)result.StatusCode }
        };
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return result.StatusCode switch
        {
            HttpStatusCode.NotFound => new NotFoundObjectResult(result.Errors),
            HttpStatusCode.Conflict => new ConflictObjectResult(result.Errors),
            HttpStatusCode.Forbidden => new ForbidResult(),
            HttpStatusCode.UnprocessableEntity => new UnprocessableEntityObjectResult(result.Errors),
            _ => new ObjectResult(result.Errors) { StatusCode = (int)result.StatusCode }
        };
    }
}
