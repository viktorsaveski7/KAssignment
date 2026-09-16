using Claims.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api.Common;

public static class ResultExtensions
{
    public static ActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.NoContent()
            : Failure(result, controller);

    public static ActionResult<T> ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.Ok(result.Value)
            : Failure(result, controller);

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
