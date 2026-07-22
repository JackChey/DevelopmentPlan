using Instructure.Paging;
using Instructure.Sorting;

namespace InprovePlan.IntegrationTests.Helpers;

/// <summary>
/// 测试查询工厂类，用于在测试环境中快速构建分页和排序查询对象。
/// 该类为静态内部类，提供便捷的静态方法来生成常用的查询参数实例。
/// </summary>
internal static class TestQueryFactory
{
    /// <summary>
    /// 创建一个分页查询对象（Pagination）。
    /// 该方法允许指定页码和每页大小，若未提供参数，则使用默认值（第1页，每页10条）。
    /// </summary>
    /// <param name="pageIndex">
    /// 页码，从1开始。默认为1。
    /// </param>
    /// <param name="pageSize">
    /// 每页包含的数据条数。默认为10。
    /// </param>
    /// <returns>
    /// 返回一个配置好页码和每页大小的 Pagination 实例。
    /// </returns>
    public static Pagination Page(int pageIndex = 1, int pageSize = 10)
    {
        // 初始化并返回 Pagination 对象
        // 将传入的页码和每页大小赋值给对应属性
        return new Pagination
        {
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 创建一个排序查询对象（SortQuery）。
    /// 该方法允许指定排序字段和排序方向，若未提供参数，则使用默认值（按 createdAt 降序排列）。
    /// </summary>
    /// <param name="sortBy">
    /// 排序依据的字段名称。默认为 "createdAt"。
    /// </param>
    /// <param name="sortDirection">
    /// 排序方向，通常为 "asc"（升序）或 "desc"（降序）。默认为 "desc"。
    /// </param>
    /// <returns>
    /// 返回一个配置好排序字段和方向的 SortQuery 实例。
    /// </returns>
    public static SortQuery Sort(string sortBy = "createdAt", string sortDirection = "desc")
    {
        // 初始化并返回 SortQuery 对象
        // 将传入的排序字段和排序方向赋值给对应属性
        return new SortQuery
        {
            SortBy = sortBy,
            SortDirection = sortDirection
        };
    }
}

