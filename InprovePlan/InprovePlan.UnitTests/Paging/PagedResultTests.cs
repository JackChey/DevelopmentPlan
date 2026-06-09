using FluentAssertions;
using Instructure.Paging;
using Xunit;

namespace InprovePlan.UnitTests.Paging;

/// <summary>
/// 分页结果测试。
///
/// 验证：
/// - Total 表示符合条件的总数
/// - Count 表示当前页数量
/// - Items 永不为 null
/// - Metadata 正确生成
/// </summary>
public sealed class PagedResultTests
{
    [Fact]
    public void Create_ShouldReturnCorrectPagedResult()
    {
        var pagination = new Pagination
        {
            PageIndex = 2,
            PageSize = 10
        };

        var items = new List<int> { 11, 12, 13 };

        var result = PagedResult<int>.Create(items, 23, pagination);

        result.Total.Should().Be(23);
        result.Count.Should().Be(3);
        result.Items.Should().BeEquivalentTo(items);
        result.Metadata.PageIndex.Should().Be(2);
        result.Metadata.PageSize.Should().Be(10);
        result.Metadata.TotalPages.Should().Be(3);
        result.Metadata.HasPrevious.Should().BeTrue();
        result.Metadata.HasNext.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldReturnEmptyItems_WhenItemsIsNull()
    {
        var result = PagedResult<int>.Create(null, 0, new Pagination());

        result.Total.Should().Be(0);
        result.Count.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}