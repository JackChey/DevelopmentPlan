namespace InprovePlan.Domain.BaseEntities
{
    /// <summary>
    /// 具体项目实体基类,用于标识数据库实体类
    /// </summary>
    public abstract class BaseEntity<TId> : IEntity<TId>
    {
        public TId Id { get; set; } = default!;


    }
}
