using Inventory.Api.Exceptions;

namespace Inventory.Tests.TestHelpers;

/// <summary>
/// Single helper that locks the (ErrorCode, Message substring, non-null Hint) trio
/// required by TEST-03 / CONTEXT.md D-03. Call once per exception scenario; add
/// targeted Asserts on Details/Hint after for richer coverage.
/// </summary>
public static class AssertDomain
{
    /// <summary>
    /// Asserts that the exception carries the expected <paramref name="expectedErrorCode"/>,
    /// that its <see cref="Exception.Message"/> contains <paramref name="messageSubstring"/>,
    /// and that <see cref="DomainException.Hint"/> is non-null + non-empty.
    /// </summary>
    public static void Trio(DomainException ex, string expectedErrorCode, string messageSubstring)
    {
        Assert.Equal(expectedErrorCode, ex.ErrorCode);
        Assert.Contains(messageSubstring, ex.Message);
        Assert.False(
            string.IsNullOrWhiteSpace(ex.Hint),
            $"Expected non-empty Hint for {expectedErrorCode}, got: '{ex.Hint}'");
    }
}
