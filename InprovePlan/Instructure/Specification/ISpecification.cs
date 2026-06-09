namespace Instructure.Specification;

using System.Linq.Expressions;

/// <summary>
/// 查询规范接口。
/// 
/// 设计目标：
/// 1. 封装业务查询条件。
/// 2. 保证条件最终作用在 IQueryable 上，由数据库执行。
/// 3. 避免仓储层到处散落 Where / Include 逻辑。
/// 
/// 注意：
/// Specification 只负责“查什么数据”，
/// 不负责分页参数校验、排序白名单校验、HTTP 响应。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public interface ISpecification<TEntity>
    where TEntity : class
{
    /// <summary>
    /// 查询条件集合。
    /// 
    /// 多个 Criteria 默认按 AND 组合。
    /// 例如：
    /// x => x.Status == 1
    /// x => x.CreatedAt >= startTime
    /// x => x.CreatedAt < endTime
    /// 
    /// 最终等价于：
    /// WHERE Status = 1
    /// AND CreatedAt >= startTime
    /// AND CreatedAt < endTime
    /// </summary>
    IReadOnlyList<Expression<Func<TEntity, bool>>> Criteria { get; }

    /// <summary>
    /// 需要加载的关联数据。
    /// 
    /// 建议只允许后端代码显式指定 Include，
    /// 不要让前端传 Include 字段。
    /// </summary>
    IReadOnlyList<Expression<Func<TEntity, object>>> Includes { get; }

    /// <summary>
    /// 是否使用 AsNoTracking。
    /// 
    /// 查询列表页通常不需要 EF Core 跟踪实体状态，
    /// 使用 AsNoTracking 可以减少内存占用，提高查询性能。
    /// </summary>
    bool AsNoTracking { get; }
}