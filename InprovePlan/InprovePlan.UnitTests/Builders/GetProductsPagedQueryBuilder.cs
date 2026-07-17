using InprovePlan.Domain.Entities;
using InprovePlan.UnitTests.TestData;
using InprovePlan.UserCase.Products.Queries;
using Instructure.Paging;
using Instructure.Sorting;

namespace InprovePlan.UnitTests.Builders;

/// <summary>
/// 分页查询产品请求 (GetProductsPagedQuery) 的测试数据构建器。
/// 用于在单元测试中快速构造包含默认分页、排序及筛选条件的查询对象，
/// 简化测试准备阶段的数据初始化工作。
/// </summary>
public sealed class GetProductsPagedQueryBuilder
{
    // 默认页码：初始化为第 1 页。
    private int _pageIndex = 1;

    // 默认每页大小：初始化为 10 条记录。
    private int _pageSize = 10;

    // 默认排序字段：初始化为 "createdAt"（创建时间）。
    private string _sortBy = "createdAt";

    // 默认排序方向：初始化为 "desc"（降序），即最新创建的产品排在前面。
    private string _sortDirection = "desc";

    // 默认搜索关键词：初始化为测试数据中定义的有效关键词。
    // 用于模拟模糊搜索场景，若需测试无关键词搜索，可后续扩展 With 方法或修改此处逻辑。
    private string? _keyword = ProductTestData.ValidKeyword;

    // 默认产品类型ID：初始化为测试数据中定义的有效类型ID。
    // 用于模拟按分类筛选产品的场景。
    private int? _productTypeId = (int)ProductTestData.ValidProductTypeId;

    // 默认产品状态：初始化为测试数据中定义的有效状态（如上架、下架等）。
    // 用于模拟按状态筛选产品的场景。
    private AppProductStatus? _productStatus = ProductTestData.ValidProductStatus;

    /// <summary>
    /// 构建并返回最终的 GetProductsPagedQuery 对象。
    /// </summary>
    /// <returns>包含当前配置参数的查询对象实例。</returns>
    /// <remarks>
    /// 此方法是构建过程的终点。它使用当前内部存储的分页、排序、筛选参数，
    /// 并固定设置 IncludeVoid 为 false（不查询已逻辑删除/无效的产品），
    /// 实例化查询对象。
    /// </remarks>
    public GetProductsPagedQuery Build()
    {
        // 使用当前配置好的参数创建查询对象
        return new GetProductsPagedQuery(
            new Pagination { PageIndex = _pageIndex, PageSize = _pageSize }, // 分页参数
            new SortQuery { SortBy = _sortBy, SortDirection = _sortDirection }, // 排序参数
            _keyword,           // 搜索关键词
            _productTypeId,     // 产品类型ID筛选
            _productStatus,     // 产品状态筛选
            IncludeVoid: false  // 明确指定不包含已作废/删除的产品
        );
    }
}

