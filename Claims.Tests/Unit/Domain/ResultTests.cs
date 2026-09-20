using Claims.Domain.Common;

namespace Claims.Tests.Unit.Domain;

public class ResultTests
{
    [Fact]
    public void Success_is_successful_and_carries_no_errors()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void NotFound_carries_the_reason()
    {
        var result = Result.NotFound("missing");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal(new[] { "missing" }, result.Errors);
    }

    [Fact]
    public void Invalid_carries_every_reason()
    {
        var result = Result.Invalid("first", "second");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(new[] { "first", "second" }, result.Errors);
    }

    [Fact]
    public void Generic_success_exposes_its_value()
    {
        var result = Result<string>.Success("payload");

        Assert.True(result.IsSuccess);
        Assert.Equal("payload", result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Generic_not_found_carries_the_reason_and_no_value()
    {
        var result = Result<string>.NotFound("missing");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal(new[] { "missing" }, result.Errors);
    }

    [Fact]
    public void Generic_invalid_carries_every_reason()
    {
        var result = Result<int>.Invalid("bad", "worse");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(new[] { "bad", "worse" }, result.Errors);
    }

    [Fact]
    public void Reading_the_value_of_a_failed_result_throws()
    {
        var result = Result<string>.NotFound("missing");

        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Contains("NotFound", exception.Message);
    }
}
