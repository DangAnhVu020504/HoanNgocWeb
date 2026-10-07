using TechPos.Sync.Core;
using Xunit;

namespace TechPos.UnitTests;

/// <summary>Hash phải ổn định bất kể thứ tự key — nền tảng của W2 Reconcile.</summary>
public class MirrorWriterHashTests
{
    [Fact]
    public void ComputeHash_IsStableRegardlessOfKeyOrder()
    {
        var a = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ID"] = "ABC", ["NAME"] = "Hàng A", ["TIENHANG"] = 123.45m
        };
        var b = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["TIENHANG"] = 123.45m, ["NAME"] = "Hàng A", ["ID"] = "ABC"
        };
        Assert.Equal(MirrorWriter.ComputeHash(a), MirrorWriter.ComputeHash(b));
    }

    [Fact]
    public void ComputeHash_DiffersForDifferentValues()
    {
        var a = new Dictionary<string, object?> { ["ID"] = "1", ["NAME"] = "A" };
        var b = new Dictionary<string, object?> { ["ID"] = "1", ["NAME"] = "B" };
        Assert.NotEqual(MirrorWriter.ComputeHash(a), MirrorWriter.ComputeHash(b));
    }

    [Fact]
    public void ComputeHash_NullVsEmpty_Differ()
    {
        var a = new Dictionary<string, object?> { ["ID"] = "1", ["NOTE"] = null };
        var b = new Dictionary<string, object?> { ["ID"] = "1", ["NOTE"] = "" };
        Assert.NotEqual(MirrorWriter.ComputeHash(a), MirrorWriter.ComputeHash(b));
    }
}
