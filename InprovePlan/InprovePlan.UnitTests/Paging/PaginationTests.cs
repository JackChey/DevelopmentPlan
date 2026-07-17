using FluentAssertions;
using Instructure.Paging;
using Xunit;

namespace InprovePlan.UnitTests.Paging;

/// <summary>
/// 分页参数 (Pagination) 的单元测试类。
/// 
/// 主要验证以下核心行为，以覆盖生产环境的分页标准：
/// 1. PageIndex 必须从 1 开始（即最小值为 1）。
/// 2. PageSize 必须大于 0。
/// 3. PageSize 不得超过定义的最大值 (MaxPageSize)。
/// 4. Skip 计数（用于数据库查询偏移量）计算逻辑正确。
/// </summary>
public sealed class PaginationTests
{
    /// <summary>
    /// 测试当分页参数完全合法时，验证通过且 Skip 计数计算正确。
    /// </summary>
    /// <remarks>
    /// 场景描述：
    /// - PageIndex = 1 (第一页)
    /// - PageSize = 20 (每页20条)
    /// 
    /// 预期结果：
    /// - Validate() 返回空错误列表。
    /// - GetSkipCount() 返回 0 (因为第一页不需要跳过任何记录: (1-1)*20 = 0)。
    /// </remarks>
    [Fact]
    public void Validate_ShouldPass_WhenPaginationIsValid()
    {
        // Arrange: 创建合法的分页对象
        var pagination = new Pagination
        {
            PageIndex = 1,
            PageSize = 20
        };

        // Act: 执行验证和 Skip 计算
        var errors = pagination.Validate();

        // Assert: 验证无错误，且 Skip 值为 0
        errors.Should().BeEmpty();
        pagination.GetSkipCount().Should().Be(0);
    }

    /// <summary>
    /// 测试当 PageIndex 无效（小于 1）时，验证失败并报告相应错误。
    /// </summary>
    /// <param name="pageIndex">无效的页码值（0 或负数）。</param>
    /// <remarks>
    /// 场景描述：
    /// - 传入 pageIndex 为 0 或 -1。
    /// 
    /// 预期结果：
    /// - Validate() 返回的错误列表中，包含一个针对 "PageIndex" 字段的错误。
    /// </remarks>
    [Theory]
    [InlineData(0)]    // 页码不能为 0
    [InlineData(-1)]   // 页码不能为负数
    public void Validate_ShouldFail_WhenPageIndexIsInvalid(int pageIndex)
    {
        // Arrange: 创建包含无效 PageIndex 的分页对象
        var pagination = new Pagination
        {
            PageIndex = pageIndex,
            PageSize = 20 // PageSize 保持合法，以隔离测试 PageIndex
        };

        // Act: 执行验证
        var errors = pagination.Validate();

        // Assert: 验证错误列表中确实包含关于 PageIndex 的错误
        errors.Should().Contain(error => error.Field == nameof(Pagination.PageIndex));
    }

    /// <summary>
    /// 测试当 PageSize 无效（小于等于 0 或超过最大值）时，验证失败并报告相应错误。
    /// </summary>
    /// <param name="pageSize">无效的每页大小值。</param>
    /// <remarks>
    /// 场景描述：
    /// - pageSize 为 0 或 -1（过小）。
    /// - pageSize 为 MaxPageSize + 1（过大）。
    /// 
    /// 预期结果：
    /// - Validate() 返回的错误列表中，包含一个针对 "PageSize" 字段的错误。
    /// </remarks>
    [Theory]
    [InlineData(0)]                       // 每页大小不能为 0
    [InlineData(-1)]                      // 每页大小不能为负数
    [InlineData(Pagination.MaxPageSize + 1)] // 每页大小不能超过允许的最大值
    public void Validate_ShouldFail_WhenPageSizeIsInvalid(int pageSize)
    {
        // Arrange: 创建包含无效 PageSize 的分页对象
        var pagination = new Pagination
        {
            PageIndex = 1, // PageIndex 保持合法，以隔离测试 PageSize
            PageSize = pageSize
        };

        // Act: 执行验证
        var errors = pagination.Validate();

        // Assert: 验证错误列表中确实包含关于 PageSize 的错误
        errors.Should().Contain(error => error.Field == nameof(Pagination.PageSize));
    }

    /// <summary>
    /// 测试 GetSkipCount 方法是否能根据 PageIndex 和 PageSize 正确计算数据库查询的偏移量。
    /// </summary>
    /// <remarks>
    /// 场景描述：
    /// - PageIndex = 3 (第三页)
    /// - PageSize = 20 (每页20条)
    /// 
    /// 计算公式：Skip = (PageIndex - 1) * PageSize
    /// 预期计算：(3 - 1) * 20 = 40
    /// 
    /// 预期结果：
    /// - GetSkipCount() 返回 40。
    /// </remarks>
    [Fact]
    public void GetSkipCount_ShouldReturnCorrectSkip()
    {
        // Arrange: 创建特定分页参数的对象
        var pagination = new Pagination
        {
            PageIndex = 3,
            PageSize = 20
        };

        // Act & Assert: 验证 Skip 计数是否符合公式 (PageIndex - 1) * PageSize
        pagination.GetSkipCount().Should().Be(40);
    }
}
