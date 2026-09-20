using Claims.Api.Common;
using Claims.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Tests.Unit.Api;

public class ResultExtensionsTests
{
    private sealed class TestController : ControllerBase;

    private static TestController Controller() => new();

    [Fact]
    public void Successful_void_result_becomes_no_content()
    {
        var action = Result.Success().ToActionResult(Controller());

        Assert.IsType<NoContentResult>(action);
    }

    [Fact]
    public void Successful_result_becomes_ok_with_the_value()
    {
        var action = Result<string>.Success("payload").ToActionResult(Controller());

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Equal("payload", ok.Value);
    }

    [Fact]
    public void Not_found_becomes_a_404_problem_carrying_the_reason()
    {
        var action = Result<string>.NotFound("Cover 'x' does not exist.").ToActionResult(Controller());

        var problem = Assert.IsType<ObjectResult>(action.Result);
        var details = Assert.IsType<ProblemDetails>(problem.Value);
        Assert.Equal(StatusCodes.Status404NotFound, details.Status);
        Assert.Equal("Resource not found", details.Title);
        Assert.Equal("Cover 'x' does not exist.", details.Detail);
    }

    [Fact]
    public void Invalid_becomes_a_400_problem_carrying_every_reason()
    {
        var action = Result<string>.Invalid("first", "second").ToActionResult(Controller());

        var problem = Assert.IsType<ObjectResult>(action.Result);
        var details = Assert.IsType<ProblemDetails>(problem.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, details.Status);
        Assert.Equal("Request violates a business rule", details.Title);
        Assert.Contains("first", details.Detail);
        Assert.Contains("second", details.Detail);
    }

    [Fact]
    public void Invalid_void_result_becomes_a_400_problem()
    {
        var action = Result.Invalid("bad request").ToActionResult(Controller());

        var problem = Assert.IsType<ObjectResult>(action);
        var details = Assert.IsType<ProblemDetails>(problem.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, details.Status);
    }

    [Fact]
    public void Not_found_void_result_becomes_a_404_problem()
    {
        var action = Result.NotFound("gone").ToActionResult(Controller());

        var problem = Assert.IsType<ObjectResult>(action);
        var details = Assert.IsType<ProblemDetails>(problem.Value);
        Assert.Equal(StatusCodes.Status404NotFound, details.Status);
    }

    [Fact]
    public void Created_result_points_at_the_named_route()
    {
        var action = Result<string>.Success("payload")
            .ToCreatedResult(Controller(), "GetThing", value => new { id = value });

        var created = Assert.IsType<CreatedAtRouteResult>(action.Result);
        Assert.Equal("GetThing", created.RouteName);
        Assert.Equal("payload", created.Value);
    }

    [Fact]
    public void Created_result_falls_through_to_the_error_response_on_failure()
    {
        var action = Result<string>.NotFound("missing")
            .ToCreatedResult(Controller(), "GetThing", value => new { id = value });

        var problem = Assert.IsType<ObjectResult>(action.Result);
        var details = Assert.IsType<ProblemDetails>(problem.Value);
        Assert.Equal(StatusCodes.Status404NotFound, details.Status);
    }
}
