namespace Instructure.Repositories;

using InprovePlan.Domain.BaseEntities;
using Instructure.Paging;
using Instructure.Sorting;
using Instructure.Specification;
using System.Linq.Expressions;

/// <summary>
/// 只读仓储接口。
///
/// 设计目标：
/// 1. 只提供查询能力，不提供新增、修改、删除。
/// 2. 支持按主键查询、条件查询、分页查询。
/// 3. 查询逻辑通过 Specification 封装，避免仓储层堆满业务 Where。
/// 4. 分页、排序必须在数据库侧完成。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public interface IReadRepository<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// 根据主键查询实体。
    ///
    /// 适合简单主键查询。
    /// 如果需要 Include 或复杂条件，建议使用 Specification。
    /// </summary>
    Task<TEntity?> GetByIdAsync(
        object id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询是否存在符合条件的数据。
    ///
    /// 常用于业务校验，例如：
    /// - 用户是否存在
    /// - 是否已经关注
    /// - 编码是否重复
    /// </summary>
    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据 Specification 查询单条数据。
    ///
    /// 如果没有数据，返回 null。
    /// 如果可能存在多条数据，但业务只需要第一条，可以使用该方法。
    /// </summary>
    Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据 Specification 查询列表。
    ///
    /// 注意：
    /// 该方法不分页。
    /// 只适合数据量可控的查询。
    /// 大列表查询应优先使用 PageAsync。
    /// </summary>
    Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询符合 Specification 的总数。
    ///
    /// Count 查询不应该应用分页。
    /// 通常也不需要 Include。
    /// </summary>
    Task<long> LongCountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页查询。
    ///
    /// 生产约束：
    /// 1. Specification 负责条件查询。
    /// 2. SortWhitelist 负责排序字段白名单。
    /// 3. Pagination 负责分页参数。
    /// 4. Count、OrderBy、Skip、Take 都必须在数据库侧执行。
    /// </summary>
    Task<PagedResult<TEntity>> PageAsync(
        ISpecification<TEntity> specification,
        Pagination pagination,
        SortQuery sortQuery,
        SortWhitelist<TEntity> sortWhitelist,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 非跟踪查询
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<TEntity?> FirstOrDefaultAsNoTrackingAsync(
    Expression<Func<TEntity, bool>> predicate,
    CancellationToken cancellationToken = default);
}