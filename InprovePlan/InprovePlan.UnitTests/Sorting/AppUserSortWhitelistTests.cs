using FluentAssertions;
using Instructure.Sorting;
using Instructure.Sorting.SortWhitelists;
using Xunit;

namespace InprovePlan.UnitTests.Sorting;

/// <summary>
/// 用户排序白名单测试。
///
/// 验证排序安全：
/// - 只允许白名单字段
/// - 禁止 passwordHash 等敏感字段
/// - 排序方向只允许 asc / desc
/// </summary>
public sealed class AppUserSortWhitelistTests
{
    [Fact]
    public void Validate_ShouldPass_WhenSortFieldIsAllowed()
    {
        var query = new SortQuery
        {
            SortBy = "createdAt",
            SortDirection = "desc"
        };

        var errors = AppUserSortWhitelist.Instance.Validate(query);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldFail_WhenSortFieldIsNotAllowed()
    {
        var query = new SortQuery
        {
            SortBy = "passwordHash",
            SortDirection = "desc"
        };

        var errors = AppUserSortWhitelist.Instance.Validate(query);

        errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_ShouldFail_WhenSortDirectionIsInvalid()
    {
        var query = new SortQuery
        {
            SortBy = "createdAt",
            SortDirection = "drop table"
        };

        var errors = AppUserSortWhitelist.Instance.Validate(query);

        errors.Should().NotBeEmpty();
    }
}