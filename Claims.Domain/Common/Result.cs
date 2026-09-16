namespace Claims.Domain.Common;

/// <summary>
/// Outcome of an operation that can fail for a reason the caller is expected to handle. Expected
/// failures are values rather than exceptions; genuine defects are still thrown.
/// </summary>
public class Result
{
    private static readonly IReadOnlyList<string> NoErrors = Array.Empty<string>();

    protected Result(ResultStatus status, IReadOnlyList<string> errors)
    {
        Status = status;
        Errors = errors;
    }

    public ResultStatus Status { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool IsSuccess => Status == ResultStatus.Success;

    public static Result Success() => new(ResultStatus.Success, NoErrors);

    public static Result NotFound(string error) => new(ResultStatus.NotFound, new[] { error });

    public static Result Invalid(params string[] errors) => new(ResultStatus.Invalid, errors);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(ResultStatus status, T? value, IReadOnlyList<string> errors)
        : base(status, errors)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"A failed result ({Status}) carries no value.");

    public static Result<T> Success(T value) => new(ResultStatus.Success, value, Array.Empty<string>());

    public static new Result<T> NotFound(string error) => new(ResultStatus.NotFound, default, new[] { error });

    public static new Result<T> Invalid(params string[] errors) => new(ResultStatus.Invalid, default, errors);
}
