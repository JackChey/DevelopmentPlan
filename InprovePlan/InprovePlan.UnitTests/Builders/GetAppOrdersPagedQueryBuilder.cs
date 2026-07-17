using InprovePlan.UnitTests.TestData;
using InprovePlan.UserCase.AppOrders.Queries;
using Instructure.Paging;
using Instructure.Sorting;

namespace InprovePlan.UnitTests.Builders;

/// <summary>
/// 分页查询订单请求 (GetAppOrdersPagedQuery) 的测试数据构建器。
/// 用于在单元测试中快速构造包含默认分页、排序及筛选条件的查询对象，
/// 简化测试准备阶段的数据初始化工作。
/// </summary>
public sealed class GetAppOrdersPagedQueryBuilder
{
    // 默认开始时间：初始化为当前时间的前一天。
    // 用于模拟一个合理的近期查询时间范围下限。
    private DateTimeOffset? _startTime = DateTimeOffset.UtcNow.AddDays(-1);

    // 默认结束时间：初始化为当前时间的后一天。
    // 用于模拟一个合理的近期查询时间范围上限。
    private DateTimeOffset? _endTime = DateTimeOffset.UtcNow.AddDays(1);

    /// <summary>
    /// 设置查询的时间范围。
    /// </summary>
    /// <param name="startTime">查询起始时间（包含）。</param>
    /// <param name="endTime">查询结束时间（包含）。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法允许测试人员自定义时间窗口，例如测试跨天查询、空时间范围或特定历史时间段的数据检索。
    /// 若传入 null，则对应字段在最终查询中可能表示“无限制”。
    /// </remarks>
    public GetAppOrdersPagedQueryBuilder WithTimeRange(DateTimeOffset? startTime, DateTimeOffset? endTime)
    {
        // 更新内部时间范围字段
        _startTime = startTime;
        _endTime = endTime;

        // 返回 this 以支持链式调用
        return this;
    }

    /// <summary>
    /// 构建并返回最终的 GetAppOrdersPagedQuery 对象。
    /// </summary>
    /// <returns>包含默认分页、排序及筛选参数的查询对象实例。</returns>
    /// <remarks>
    /// 此方法是构建过程的终点。它使用硬编码的默认分页（第1页，每页10条）、
    /// 默认排序（按创建时间降序）以及来自 AppOrderTestData 的有效筛选条件，
    /// 结合当前配置的时间范围，实例化查询对象。
    /// </remarks>
    public GetAppOrdersPagedQuery Build()
    {
        // 使用默认的分页、排序、筛选条件以及配置好的时间范围创建查询对象
        return new GetAppOrdersPagedQuery(
            new Pagination { PageIndex = 1, PageSize = 10 },       // 默认分页参数
            new SortQuery { SortBy = "createdAt", SortDirection = "desc" }, // 默认排序规则
            AppOrderTestData.ValidOrderKeyword,                     // 默认搜索关键词
            AppOrderTestData.ValidUserId,                           // 默认用户ID筛选
            AppOrderTestData.ValidProductId,                        // 默认产品ID筛选
            AppOrderTestData.ValidOrderStatus,                      // 默认订单状态筛选
            _startTime,                                             // 配置的起始时间
            _endTime);                                              // 配置的结束时间
    }
}

