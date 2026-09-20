using Claims.Api.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Claims.Tests.Unit.Api;

public class GlobalExceptionHandlerTests
{
    private static (GlobalExceptionHandler Handler, IProblemDetailsService Service) Create(bool written = true)
    {
        var service = Substitute.For<IProblemDetailsService>();
        service.TryWriteAsync(Arg.Any<ProblemDetailsContext>()).Returns(ValueTask.FromResult(written));
        return (new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, service), service);
    }

    private static DefaultHttpContext HttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/Covers";
        return context;
    }

    [Fact]
    public async Task Sets_a_500_status_code()
    {
        var (handler, _) = Create();
        var context = HttpContext();

        await handler.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task Writes_an_opaque_problem_response()
    {
        var (handler, service) = Create();
        var context = HttpContext();
        var exception = new InvalidOperationException("connection string leaked here");

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        await service.Received(1).TryWriteAsync(Arg.Is<ProblemDetailsContext>(problem =>
            problem.ProblemDetails.Status == StatusCodes.Status500InternalServerError
            && problem.ProblemDetails.Title == "An unexpected error occurred"
            && !problem.ProblemDetails.Detail!.Contains("connection string leaked here")));
    }

    [Fact]
    public async Task Passes_the_exception_through_for_diagnostics()
    {
        var (handler, service) = Create();
        var exception = new InvalidOperationException("boom");

        await handler.TryHandleAsync(HttpContext(), exception, CancellationToken.None);

        await service.Received(1)
            .TryWriteAsync(Arg.Is<ProblemDetailsContext>(problem => problem.Exception == exception));
    }

    [Fact]
    public async Task Reports_unhandled_when_the_problem_service_declines()
    {
        var (handler, _) = Create(written: false);

        var handled = await handler.TryHandleAsync(
            HttpContext(),
            new InvalidOperationException("boom"),
            CancellationToken.None);

        Assert.False(handled);
    }
}
