using FluentAssertions;
using Instructure.Paging;
using Xunit;

namespace InprovePlan.UnitTests.Paging;

/// <summary>
/// 分页结果 (PagedResult) 的单元测试类。
/// 
/// 主要验证以下核心行为：
/// 1. Total 属性正确反映符合查询条件的记录总数。
/// 2. Count 属性正确反映当前页实际返回的项目数量。
/// 3. Items 集合在创建时永不为 null，即使输入为空或 null 也应初始化为空集合。
/// 4. Metadata 元数据对象（包含页码、总页数、是否有上一页/下一页等）计算正确。
/// </summary>
public sealed class PagedResultTests
{
    /// <summary>
    /// 测试正常场景下 Create 方法是否正确构建分页结果。
    /// </summary>
    /// <remarks>
    /// 场景描述：
    /// - 请求第 2 页，每页 10 条数据。
    /// - 当前页实际返回 3 条数据 (11, 12, 13)。
    /// - 数据库中共有 23 条符合条件的数据。
    /// 
    /// 预期结果：
    /// - Total 为 23。
    /// - Count 为 3。
    /// - Items 内容与输入列表一致。
    /// - Metadata 中 PageIndex=2, PageSize=10。
    /// - TotalPages 应为 ceil(23/10) = 3。
    /// - HasPrevious 为 true (因为当前是第2页，前面有第1页)。
    /// - HasNext 为 true (因为当前是第2页，后面还有第3页)。
    /// </remarks>
    [Fact]
    public void Create_ShouldReturnCorrectPagedResult()
    {
        //  Arrange: 准备测试数据
        var pagination = new Pagination
        {
            PageIndex = 2, // 当前页码
            PageSize = 10  // 每页大小
        };

        // 模拟当前页查询到的数据项
        var items = new List<int> { 11, 12, 13 };

        //  Act: 执行创建操作，传入数据项、总记录数和分页参数
        var result = PagedResult<int>.Create(items, 23, pagination);

        //  Assert: 验证结果是否符合预期
        result.Total.Should().Be(23);          // 总记录数应等于传入的 total
        result.Count.Should().Be(3);           // 当前页项目数应等于 items 的数量
        result.Items.Should().BeEquivalentTo(items); // 项目内容应与输入一致

        // 验证元数据计算逻辑
        result.Metadata.PageIndex.Should().Be(2);      // 页码应保持输入值
        result.Metadata.PageSize.Should().Be(10);      // 每页大小应保持输入值
        result.Metadata.TotalPages.Should().Be(3);     // 总页数 = ceil(23 / 10) = 3
        result.Metadata.HasPrevious.Should().BeTrue(); // 第2页存在前一页 (第1页)
        result.Metadata.HasNext.Should().BeTrue();     // 第2页存在后一页 (第3页)
    }

    /// <summary>
    /// 测试当输入的数据项列表为 null 时，Create 方法是否能安全处理并返回空结果。
    /// </summary>
    /// <remarks>
    /// 场景描述：
    /// - 传入 null 作为 items 列表。
    /// - 传入 0 作为总记录数。
    /// 
    /// 预期结果：
    /// - Total 为 0。
    /// - Count 为 0。
    /// - Items 不为 null，而是一个空的集合（防止调用方出现 NullReferenceException）。
    /// </remarks>
    [Fact]
    public void Create_ShouldReturnEmptyItems_WhenItemsIsNull()
    {
        //  Arrange & Act: 传入 null 列表和默认分页参数
        var result = PagedResult<int>.Create(null, 0, new Pagination());

        //  Assert: 验证结果是否为安全的空状态
        result.Total.Should().Be(0);       // 总记录数为 0
        result.Count.Should().Be(0);       // 当前页数量为 0
        result.Items.Should().BeEmpty();   // Items 集合应为空，且隐含不为 null
    }
}
