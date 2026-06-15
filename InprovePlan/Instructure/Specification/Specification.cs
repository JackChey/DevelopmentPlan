namespace Instructure.Specification;

using InprovePlan.Domain.BaseEntities;
using System.Linq.Expressions;

/// <summary>
/// 查询规范基类。
/// 
/// 业务查询规范可以继承该类，
/// 然后在构造函数中根据查询参数添加条件。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public abstract class Specification<TEntity> : ISpecification<TEntity>  where TEntity : class,IEntity
{
    private readonly List<Expression<Func<TEntity, bool>>> _criteria = [];
    private readonly List<Expression<Func<TEntity, object>>> _includes = [];

    /// <summary>
    /// 查询条件集合。
    /// </summary>
    public IReadOnlyList<Expression<Func<TEntity, bool>>> Criteria => _criteria;

    /// <summary>
    /// Include 集合。
    /// </summary>
    public IReadOnlyList<Expression<Func<TEntity, object>>> Includes => _includes;

    /// <summary>
    /// 默认使用 AsNoTracking。
    /// 
    /// 对于列表查询、分页查询、只读查询，这是更合理的默认值。
    /// 如果确实需要修改实体，可以在具体规范中关闭。
    /// </summary>
    public bool AsNoTracking { get;  set; } = true;

    /// <summary>
    /// 添加查询条件。
    /// 
    /// 注意：
    /// 这里接收 Expression，而不是 Func。
    /// Expression 可以被 EF Core 翻译成 SQL。
    /// Func 会导致内存执行，不符合生产分页查询标准。
    /// </summary>
    protected void AddCriteria(Expression<Func<TEntity, bool>> criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        _criteria.Add(criteria);
    }

    /// <summary>
    /// 添加 Include。
    /// 
    /// 建议使用强类型表达式 Include，
    /// 避免字符串 Include 带来的重构风险。
    /// </summary>
    protected void AddInclude(Expression<Func<TEntity, object>> include)
    {
        ArgumentNullException.ThrowIfNull(include);
        _includes.Add(include);
    }

    /// <summary>
    /// 关闭 AsNoTracking。
    /// 
    /// 仅当后续需要修改查询出的实体时使用。
    /// </summary>
    protected void EnableTracking()
    {
        AsNoTracking = false;
    }
}
