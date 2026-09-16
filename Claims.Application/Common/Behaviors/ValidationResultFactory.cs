using System.Collections.Concurrent;
using System.Reflection;
using Claims.Domain.Common;

namespace Claims.Application.Common.Behaviors;

/// <summary>
/// Builds a failed result when the response type is only known generically. Reflection is the cost of
/// keeping validation failures as values; the lookup is cached per type.
/// </summary>
internal static class ValidationResultFactory
{
    private static readonly ConcurrentDictionary<Type, MethodInfo?> InvalidFactories = new();

    public static TResponse Invalid<TResponse>(IEnumerable<string> errors)
        where TResponse : Result
    {
        var messages = errors.ToArray();
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Invalid(messages);
        }

        var factory = InvalidFactories.GetOrAdd(responseType, static type => type.GetMethod(
            nameof(Result.Invalid),
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
            binder: null,
            types: new[] { typeof(string[]) },
            modifiers: null));

        if (factory is null)
        {
            throw new InvalidOperationException(
                $"'{responseType}' does not declare a static Invalid(string[]) factory.");
        }

        return (TResponse)factory.Invoke(null, new object[] { messages })!;
    }
}
