using Claims.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api.Common;

/// <summary>Translates domain results into HTTP responses. The one place a failure becomes a status code.</summary>
public static class ResultExtensions
{
    /// <summary>Maps a valueless result onto 204 No Content, or the matching error response.</summary>
    public static ActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.NoContent()
            : Failure(result, controller);

    /// <summary>Maps a result onto 200 OK carrying its value, or the matching error response.</summary>
    public static ActionResult<T> ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.Ok(result.Value)
            : Failure(result, controller);

    /// <summary>Maps a successful result onto 201 Created pointing at a named route.</summary>
    /// <remarks>
    /// Addressed by route name, not action name: MVC trims the Async suffix from action names, so
    /// CreatedAtAction(nameof(GetClaimAsync)) silently fails to resolve and returns a 500.
    /// </remarks>
    public static ActionResult<T> ToCreatedResult<T>(
        this Result<T> result,
        ControllerBase controller,
        string routeName,
        Func<T, object> routeValues) =>
        result.IsSuccess
            ? controller.CreatedAtRoute(routeName, routeValues(result.Value), result.Value)
            : Failure(result, controller);

    private static ActionResult Failure(Result result, ControllerBase controller)
    {
        var detail = string.Join(" ", result.Errors);

        return result.Status switch
        {
            ResultStatus.NotFound => controller.Problem(
                detail: detail,
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found"),

            ResultStatus.Invalid => controller.Problem(
                detail: detail,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Request violates a business rule"),

            _ => throw new InvalidOperationException(
                $"Result status '{result.Status}' has no HTTP mapping.")
        };
    }
}
