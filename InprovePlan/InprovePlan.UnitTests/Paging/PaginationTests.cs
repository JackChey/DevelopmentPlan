using FluentAssertions;
using Instructure.Paging;
using Xunit;

namespace InprovePlan.UnitTests.Paging;

/// <summary>
/// 分页参数测试。
///
/// 覆盖生产分页标准：
/// - pageIndex 从 1 开始
/// - pageSize 必须大于 0
/// - pageSize 不得超过最大值
/// - Skip 计算正确
/// </summary>
public sealed class PaginationTests
{
    [Fact]
    public void Validate_ShouldPass_WhenPaginationIsValid()
    {
        var pagination = new Pagination
        {
            PageIndex = 1,
            PageSize = 20
        };

        var errors = pagination.Validate();

        errors.Should().BeEmpty();
        pagination.GetSkipCount().Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenPageIndexIsInvalid(int pageIndex)
    {
        var pagination = new Pagination
        {
            PageIndex = pageIndex,
            PageSize = 20
        };

        var errors = pagination.Validate();

        errors.Should().Contain(error => error.Field == nameof(Pagination.PageIndex));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(Pagination.MaxPageSize + 1)]
    public void Validate_ShouldFail_WhenPageSizeIsInvalid(int pageSize)
    {
        var pagination = new Pagination
        {
            PageIndex = 1,
            PageSize = pageSize
        };

        var errors = pagination.Validate();

        errors.Should().Contain(error => error.Field == nameof(Pagination.PageSize));
    }

    [Fact]
    public void GetSkipCount_ShouldReturnCorrectSkip()
    {
        var pagination = new Pagination
        {
            PageIndex = 3,
            PageSize = 20
        };

        pagination.GetSkipCount().Should().Be(40);
    }
}