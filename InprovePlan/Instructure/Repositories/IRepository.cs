using InprovePlan.Domain.BaseEntities;

namespace Instructure.Repositories;

/// <summary>
/// 可写仓储接口。
///
/// 可写仓储继承只读仓储，表示：
/// 既可以查询，也可以新增、修改、删除。
///
/// 注意：
/// 是否在仓储中提供 SaveChangesAsync，取决于你的架构。
/// 如果你有 UnitOfWork，可以把 SaveChangesAsync 放到 UnitOfWork。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public interface IRepository<TEntity> : IReadRepository<TEntity>
    where TEntity : class,IEntity
{
    /// <summary>
    /// 新增实体。
    ///
    /// 只负责把实体加入 DbContext。
    /// 是否立即提交，由 SaveChangesAsync 或 UnitOfWork 控制。
    /// </summary>
    Task<TEntity> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量新增实体。
    /// </summary>
    Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体。
    ///
    /// 对于 EF Core 已跟踪实体，通常不需要显式调用 Update。
    /// 对于离线 DTO 映射后的实体，可以调用 Update。
    /// </summary>
    void Update(TEntity entity);

    /// <summary>
    /// 删除实体。
    ///
    /// 如果项目采用软删除，这里可以在具体仓储或领域层中改成设置 IsDeleted。
    /// </summary>
    void Remove(TEntity entity);

    /// <summary>
    /// 批量删除实体。
    /// </summary>
    void RemoveRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// 提交更改。
    ///
    /// 如果项目已有 UnitOfWork，可以删除该方法，
    /// 改为统一由 IUnitOfWork.SaveChangesAsync 提交。
    /// </summary>
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
