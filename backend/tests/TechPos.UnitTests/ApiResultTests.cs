using FluentAssertions;
using TechPos.Shared;
using TechPos.Sync.Core;
using Xunit;

namespace TechPos.UnitTests;

public class ApiResultTests
{
    [Fact]
    public void Ok_should_build_success_envelope()
    {
        var r = ApiResult<string>.Ok("data", "t1");
        r.Success.Should().BeTrue();
        r.Data.Should().Be("data");
        r.TraceId.Should().Be("t1");
    }

    [Fact]
    public void Fail_should_carry_errors()
    {
        var r = ApiResult<string>.Fail("lỗi", "t2", [new ApiError("qty", "GT_0", "SL > 0")]);
        r.Success.Should().BeFalse();
        r.Errors.Should().HaveCount(1);
        r.Errors[0].Message.Should().Be("SL > 0");
    }
}

public class SyncRunResultTests
{
    [Fact]
    public void Run_result_defaults_to_zero()
    {
        var r = new SyncRunResult("TDONHANG", 0, 0, 0, 0, TimeSpan.Zero);
        r.TableName.Should().Be("TDONHANG");
        r.Errors.Should().Be(0);
    }

    [Fact]
    public void Table_policy_group_constants_follow_doc()
    {
        // mục 8: nghiệp vụ 30s, danh mục 120s
        SyncTables.TransactionIntervalSeconds.Should().Be(30);
        SyncTables.DictionaryIntervalSeconds.Should().Be(120);
        SyncTables.Transaction.Should().Contain("TDONHANG");
        SyncTables.Dictionary.Should().Contain("DMATHANG");
    }
}